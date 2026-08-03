using System.Data;
using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Application.Inventory.Dtos;
using HoraLife.Store.Application.Inventory.Models;
using HoraLife.Store.Domain.Entities;
using HoraLife.Store.Domain.Enums;
using HoraLife.Store.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HoraLife.Store.Infrastructure.Repositories;

public sealed class InventoryReservationRepository :  IInventoryReservationRepository
{
    private const string OrderReferenceType = "order";

    private readonly StoreDbContext _dbContext;

    public InventoryReservationRepository( StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InventoryReservationOperationResult>  ReserveForOrderAsync( Guid userId, Guid orderId,  CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        try
        {
            // Lock the order row first. This serializes concurrent
            // reservation attempts for the same order.
            var order = await _dbContext.Orders
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM store.orders
                    WHERE id = {orderId}
                      AND user_id = {userId}
                    FOR UPDATE
                    """)
                .SingleOrDefaultAsync(cancellationToken);

            if (order is null)
            {
                await transaction.RollbackAsync(cancellationToken);

                return InventoryReservationOperationResult.Failure(
                    InventoryReservationErrorCode.OrderNotFound,
                    "The requested order was not found.");
            }

            if (order.OrderStatus != OrderStatus.Pending)
            {
                await transaction.RollbackAsync(cancellationToken);

                return InventoryReservationOperationResult.Failure(
                    InventoryReservationErrorCode.OrderNotPending,
                    "Inventory can be reserved only while the order is pending.");
            }

            // This check happens after the order lock, so a second
            // concurrent request sees the first committed reservation.
            var alreadyReserved =
                await _dbContext.InventoryTransactions
                    .AsNoTracking()
                    .AnyAsync(
                        inventoryTransaction =>
                            inventoryTransaction.ReferenceType ==
                                OrderReferenceType &&
                            inventoryTransaction.ReferenceId ==
                                order.Id &&
                            inventoryTransaction.TransactionType ==
                                InventoryTransactionType.OrderReserved,
                        cancellationToken);

            if (alreadyReserved)
            {
                await transaction.RollbackAsync(cancellationToken);

                return InventoryReservationOperationResult.Failure(
                    InventoryReservationErrorCode.InventoryAlreadyReserved,
                    $"Inventory is already reserved for order {order.OrderNumber}.");
            }

            var orderItems = await _dbContext.OrderItems
                .AsNoTracking()
                .Where(item => item.OrderId == order.Id)
                .OrderBy(item => item.Id)
                .ToListAsync(cancellationToken);

            if (orderItems.Count == 0)
            {
                await transaction.RollbackAsync(cancellationToken);

                return InventoryReservationOperationResult.Failure(
                    InventoryReservationErrorCode.OrderItemsMissing,
                    "The order does not contain any order items.");
            }

            var now = DateTimeOffset.UtcNow;

            var itemResults =
                new List<InventoryReservationItemDto>();

            foreach (var orderItem in orderItems)
            {
                if (!orderItem.ProductId.HasValue)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    return InventoryReservationOperationResult.Failure(
                        InventoryReservationErrorCode.OrderItemProductMissing,
                        $"Order item {orderItem.Id} does not contain a ProductId.");
                }

                var inventoryRows =
                    await LockMatchingInventoryRowsAsync(
                        orderItem.ProductId.Value,
                        orderItem.VariantId,
                        cancellationToken);

                if (inventoryRows.Count == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    return InventoryReservationOperationResult.Failure(
                        InventoryReservationErrorCode.InventoryRecordMissing,
                        $"No inventory record exists for {Describe(orderItem)}.");
                }

                var availableBefore = inventoryRows.Sum(
                    row => Math.Max(
                        0,
                        row.StockQuantity - row.ReservedQuantity));

                if (availableBefore < orderItem.Quantity)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    return InventoryReservationOperationResult.Failure(
                        InventoryReservationErrorCode.InsufficientStock,
                        $"{Describe(orderItem)} requires {orderItem.Quantity} item(s), but only {availableBefore} are available.");
                }

                var remainingToReserve = orderItem.Quantity;

                var allocations =
                    new List<InventoryReservationAllocationDto>();

                foreach (var inventory in inventoryRows)
                {
                    if (remainingToReserve == 0)
                    {
                        break;
                    }

                    var availableInRow = Math.Max(
                        0,
                        inventory.StockQuantity -
                        inventory.ReservedQuantity);

                    if (availableInRow == 0)
                    {
                        continue;
                    }

                    var reserveNow = Math.Min(
                        availableInRow,
                        remainingToReserve);

                    var reservedBefore =
                        inventory.ReservedQuantity;

                    inventory.ReservedQuantity += reserveNow;
                    inventory.UpdatedAt = now;

                    var inventoryTransaction =
                        new InventoryTransaction
                        {
                            Id = Guid.NewGuid(),
                            InventoryId = inventory.Id,
                            TransactionType =
                                InventoryTransactionType.OrderReserved,

                            // Reservation reduces availability, while
                            // physical stock remains unchanged.
                            QuantityChange = -reserveNow,
                            ReferenceType = OrderReferenceType,
                            ReferenceId = order.Id,
                            Notes =
                                $"Reserved for order {order.OrderNumber}; order item {orderItem.Id}.",
                            CreatedAt = now
                        };

                    _dbContext.InventoryTransactions.Add(
                        inventoryTransaction);

                    allocations.Add(
                        new InventoryReservationAllocationDto(
                            inventory.Id,
                            inventoryTransaction.Id,
                            inventory.StockQuantity,
                            reservedBefore,
                            reserveNow,
                            inventory.ReservedQuantity,
                            inventory.StockQuantity -
                                inventory.ReservedQuantity));

                    remainingToReserve -= reserveNow;
                }

                // Defensive check. The locked-row availability check
                // above should guarantee that this never occurs.
                if (remainingToReserve != 0)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    return InventoryReservationOperationResult.Failure(
                        InventoryReservationErrorCode.InsufficientStock,
                        $"Inventory allocation could not reserve the full quantity for {Describe(orderItem)}.");
                }

                itemResults.Add(
                    new InventoryReservationItemDto(
                        orderItem.Id,
                        orderItem.ProductId.Value,
                        orderItem.VariantId,
                        orderItem.ProductName,
                        orderItem.VariantName,
                        orderItem.Sku,
                        orderItem.Quantity,
                        availableBefore,
                        orderItem.Quantity,
                        availableBefore - orderItem.Quantity,
                        allocations));
            }

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            var response = new InventoryReservationDto(
                order.Id,
                order.OrderNumber,
                userId,
                ToApiValue(order.OrderStatus),
                ToApiValue(order.PaymentStatus),
                InventoryReserved: true,

                // Payment is pending, so Step 6 reserves stock.
                // Permanent deduction happens after payment.
                StockQuantityDeducted: false,
                itemResults.Sum(item =>
                    item.ReservedQuantity),
                itemResults,
                now);

            return InventoryReservationOperationResult.Success(
                response);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
    }

    private async Task<List<Inventory>>
        LockMatchingInventoryRowsAsync(
            Guid productId,
            Guid? variantId,
            CancellationToken cancellationToken)
    {
        if (variantId.HasValue)
        {
            return await _dbContext.Inventory
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM store.inventory
                    WHERE product_id = {productId}
                      AND variant_id = {variantId.Value}
                    ORDER BY id
                    FOR UPDATE
                    """)
                .ToListAsync(cancellationToken);
        }

        return await _dbContext.Inventory
            .FromSqlInterpolated($"""
                SELECT *
                FROM store.inventory
                WHERE product_id = {productId}
                  AND variant_id IS NULL
                ORDER BY id
                FOR UPDATE
                """)
            .ToListAsync(cancellationToken);
    }

    private static string Describe(
        OrderItem item)
    {
        return string.IsNullOrWhiteSpace(
            item.VariantName)
            ? item.ProductName
            : $"{item.ProductName} – {item.VariantName}";
    }

    private static string ToApiValue<TEnum>(
        TEnum value)
        where TEnum : struct, Enum
    {
        return string.Concat(
            value.ToString()
                .Select((character, index) =>
                    index > 0 &&
                    char.IsUpper(character)
                        ? $"_{char.ToLowerInvariant(character)}"
                        : char.ToLowerInvariant(character)
                            .ToString()));
    }
}
