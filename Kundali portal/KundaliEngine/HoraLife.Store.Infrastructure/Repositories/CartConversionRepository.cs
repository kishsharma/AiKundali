using System.Data;
using HoraLife.Store.Application.Carts.Conversion.Dtos;
using HoraLife.Store.Application.Carts.Conversion.Models;
using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Domain.Enums;
using HoraLife.Store.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HoraLife.Store.Infrastructure.Repositories;

public sealed class CartConversionRepository :
    ICartConversionRepository
{
    private const string OrderReferenceType = "order";

    private readonly StoreDbContext _dbContext;

    public CartConversionRepository(
        StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CartConversionOperationResult>
        ConvertForOrderAsync(
            Guid userId,
            Guid orderId,
            CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        try
        {
            // Lock the order so two conversion requests for the same
            // order cannot race through the checks.
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
                await transaction.RollbackAsync(
                    cancellationToken);

                return CartConversionOperationResult.Failure(
                    CartConversionErrorCode.OrderNotFound,
                    "The requested order was not found.");
            }

            if (order.OrderStatus != OrderStatus.Pending)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return CartConversionOperationResult.Failure(
                    CartConversionErrorCode.OrderNotPending,
                    "The source cart can be converted only while the order is pending.");
            }

            if (!order.CartId.HasValue)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return CartConversionOperationResult.Failure(
                    CartConversionErrorCode.OrderCartMissing,
                    "The order is not linked to a source cart.");
            }

            var orderItemsCount =
                await _dbContext.OrderItems
                    .AsNoTracking()
                    .CountAsync(
                        item => item.OrderId == order.Id,
                        cancellationToken);

            if (orderItemsCount == 0)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return CartConversionOperationResult.Failure(
                    CartConversionErrorCode.OrderItemsMissing,
                    "Create order items before converting the cart.");
            }

            var inventoryReserved =
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

            if (!inventoryReserved)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return CartConversionOperationResult.Failure(
                    CartConversionErrorCode.InventoryNotReserved,
                    "Reserve inventory before converting the cart.");
            }

            // Lock the source cart before checking and changing status.
            var cart = await _dbContext.Carts
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM store.carts
                    WHERE id = {order.CartId.Value}
                    FOR UPDATE
                    """)
                .SingleOrDefaultAsync(cancellationToken);

            if (cart is null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return CartConversionOperationResult.Failure(
                    CartConversionErrorCode.CartNotFound,
                    "The source cart linked to the order was not found.");
            }

            if (cart.UserId != userId)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return CartConversionOperationResult.Failure(
                    CartConversionErrorCode.CartOwnershipMismatch,
                    "The source cart does not belong to the authenticated user.");
            }

            if (cart.Status == CartStatus.Converted)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return CartConversionOperationResult.Failure(
                    CartConversionErrorCode.CartAlreadyConverted,
                    $"Cart {cart.Id} is already converted.");
            }

            if (cart.Status != CartStatus.Active)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                return CartConversionOperationResult.Failure(
                    CartConversionErrorCode.CartNotActive,
                    $"Only an active cart can be converted. Current status is {cart.Status}.");
            }

            var previousStatus = cart.Status;
            var now = DateTimeOffset.UtcNow;

            cart.Status = CartStatus.Converted;
            cart.UpdatedAt = now;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return CartConversionOperationResult.Success(
                new ConvertedCartDto(
                    cart.Id,
                    order.Id,
                    order.OrderNumber,
                    userId,
                    ToApiValue(previousStatus),
                    ToApiValue(cart.Status),
                    ToApiValue(order.OrderStatus),
                    ToApiValue(order.PaymentStatus),
                    orderItemsCount,
                    inventoryReserved,
                    CartConverted: true,

                    // A new cart is intentionally not created now.
                    // StoreCartService creates one when the user next
                    // adds a product.
                    NewActiveCartCreated: false,
                    now));
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            throw;
        }
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
