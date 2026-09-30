using System.Data;
using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Application.Inventory.Dtos;
using HoraLife.Store.Application.Inventory.Models;
using HoraLife.Store.Domain.Entities;
using HoraLife.Store.Domain.Enums;
using HoraLife.Store.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HoraLife.Store.Infrastructure.Repositories;

public sealed class InventoryAdminRepository : IInventoryAdminRepository
{
    private readonly StoreDbContext _dbContext;

    public InventoryAdminRepository(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<InventoryListItemDto>> ListAsync(
        bool lowStockOnly,
        CancellationToken cancellationToken = default)
    {
        var rows = await (
            from inventory in _dbContext.Inventory.AsNoTracking()
            join product in _dbContext.Products.AsNoTracking()
                on inventory.ProductId equals product.Id
            join variant in _dbContext.ProductVariants.AsNoTracking()
                on inventory.VariantId equals variant.Id into variantGroup
            from variant in variantGroup.DefaultIfEmpty()
            select new
            {
                inventory.Id,
                inventory.ProductId,
                inventory.VariantId,
                ProductName = product.Name,
                ProductSku = product.Sku,
                VariantName = variant != null ? variant.Name : null,
                VariantSku = variant != null ? variant.Sku : null,
                inventory.StockQuantity,
                inventory.ReservedQuantity,
                inventory.ReorderLevel,
                inventory.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var mapped = rows
            .OrderBy(row => row.ProductName)
            .ThenBy(row => row.VariantName ?? string.Empty)
            .Select(row =>
            {
                var available = Math.Max(0, row.StockQuantity - row.ReservedQuantity);
                return new InventoryListItemDto(
                    row.Id,
                    row.ProductId,
                    row.VariantId,
                    row.ProductName,
                    row.ProductSku,
                    row.VariantName,
                    row.VariantSku,
                    row.StockQuantity,
                    row.ReservedQuantity,
                    available,
                    row.ReorderLevel,
                    available <= row.ReorderLevel,
                    row.UpdatedAt);
            })
            .ToList();

        if (lowStockOnly)
        {
            return mapped.Where(item => item.IsLowStock).ToList();
        }

        return mapped;
    }

    public async Task<IReadOnlyList<InventoryTransactionListItemDto>> ListTransactionsAsync(
        Guid inventoryId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.InventoryTransactions
            .AsNoTracking()
            .Where(txn => txn.InventoryId == inventoryId)
            .OrderByDescending(txn => txn.CreatedAt)
            .ThenByDescending(txn => txn.Id)
            .ToListAsync(cancellationToken);

        return rows
            .Select(txn => new InventoryTransactionListItemDto(
                txn.Id,
                txn.InventoryId,
                ToApiValue(txn.TransactionType),
                txn.QuantityChange,
                txn.ReferenceType,
                txn.ReferenceId,
                txn.Notes,
                txn.CreatedAt))
            .ToList();
    }

    public async Task<InventoryAdminOperationResult<InventoryListItemDto>> AdjustAsync(
        Guid inventoryId,
        int quantityChange,
        string? reason,
        string? transactionType,
        CancellationToken cancellationToken = default)
    {
        if (quantityChange == 0)
        {
            return InventoryAdminOperationResult<InventoryListItemDto>.Failure(
                InventoryAdminErrorCode.InvalidQuantityChange,
                "Quantity change must be non-zero.");
        }

        if (!TryParseAdjustType(transactionType, quantityChange, out var type, out var typeError))
        {
            return InventoryAdminOperationResult<InventoryListItemDto>.Failure(
                InventoryAdminErrorCode.InvalidTransactionType,
                typeError!);
        }

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

        try
        {
            var inventory = await _dbContext.Inventory
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM store.inventory
                    WHERE id = {inventoryId}
                    FOR UPDATE
                    """)
                .SingleOrDefaultAsync(cancellationToken);

            if (inventory is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return InventoryAdminOperationResult<InventoryListItemDto>.Failure(
                    InventoryAdminErrorCode.InventoryNotFound,
                    "Inventory record was not found.");
            }

            var nextStock = inventory.StockQuantity + quantityChange;
            if (nextStock < inventory.ReservedQuantity)
            {
                await transaction.RollbackAsync(cancellationToken);
                return InventoryAdminOperationResult<InventoryListItemDto>.Failure(
                    InventoryAdminErrorCode.StockWouldFallBelowReserved,
                    $"Stock cannot fall below reserved quantity ({inventory.ReservedQuantity}).");
            }

            if (nextStock < 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return InventoryAdminOperationResult<InventoryListItemDto>.Failure(
                    InventoryAdminErrorCode.InvalidQuantityChange,
                    "Stock quantity cannot become negative.");
            }

            var now = DateTimeOffset.UtcNow;
            inventory.StockQuantity = nextStock;
            inventory.UpdatedAt = now;

            _dbContext.InventoryTransactions.Add(new InventoryTransaction
            {
                Id = Guid.NewGuid(),
                InventoryId = inventory.Id,
                TransactionType = type,
                QuantityChange = quantityChange,
                ReferenceType = "admin",
                ReferenceId = null,
                Notes = string.IsNullOrWhiteSpace(reason)
                    ? $"Admin {ToApiValue(type)}"
                    : reason.Trim(),
                CreatedAt = now
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var dto = await GetListItemAsync(inventory.Id, cancellationToken);
            return InventoryAdminOperationResult<InventoryListItemDto>.Success(dto!);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<InventoryAdminOperationResult<InventoryListItemDto>> SetReorderLevelAsync(
        Guid inventoryId,
        int reorderLevel,
        CancellationToken cancellationToken = default)
    {
        if (reorderLevel < 0)
        {
            return InventoryAdminOperationResult<InventoryListItemDto>.Failure(
                InventoryAdminErrorCode.InvalidReorderLevel,
                "Reorder level cannot be negative.");
        }

        var inventory = await _dbContext.Inventory
            .SingleOrDefaultAsync(item => item.Id == inventoryId, cancellationToken);

        if (inventory is null)
        {
            return InventoryAdminOperationResult<InventoryListItemDto>.Failure(
                InventoryAdminErrorCode.InventoryNotFound,
                "Inventory record was not found.");
        }

        inventory.ReorderLevel = reorderLevel;
        inventory.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = await GetListItemAsync(inventory.Id, cancellationToken);
        return InventoryAdminOperationResult<InventoryListItemDto>.Success(dto!);
    }

    private async Task<InventoryListItemDto?> GetListItemAsync(
        Guid inventoryId,
        CancellationToken cancellationToken)
    {
        var list = await ListAsync(lowStockOnly: false, cancellationToken);
        return list.FirstOrDefault(item => item.Id == inventoryId);
    }

    private static bool TryParseAdjustType(
        string? transactionType,
        int quantityChange,
        out InventoryTransactionType type,
        out string? error)
    {
        error = null;
        type = quantityChange > 0
            ? InventoryTransactionType.StockAdded
            : InventoryTransactionType.ManualAdjustment;

        if (string.IsNullOrWhiteSpace(transactionType))
        {
            return true;
        }

        var normalized = transactionType.Trim().ToLowerInvariant().Replace('-', '_');
        if (normalized is "stock_added" or "stockadded")
        {
            if (quantityChange <= 0)
            {
                error = "stock_added requires a positive quantity change.";
                return false;
            }

            type = InventoryTransactionType.StockAdded;
            return true;
        }

        if (normalized is "manual_adjustment" or "manualadjustment")
        {
            type = InventoryTransactionType.ManualAdjustment;
            return true;
        }

        error = "Transaction type must be stock_added or manual_adjustment.";
        return false;
    }

    private static string ToApiValue<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        return string.Concat(
            value.ToString()
                .Select((character, index) =>
                    index > 0 && char.IsUpper(character)
                        ? $"_{char.ToLowerInvariant(character)}"
                        : char.ToLowerInvariant(character).ToString()));
    }
}
