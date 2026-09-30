using HoraLife.Store.Application.Inventory.Dtos;
using HoraLife.Store.Application.Inventory.Models;

namespace HoraLife.Store.Application.Interfaces;

public interface IInventoryAdminRepository
{
    Task<IReadOnlyList<InventoryListItemDto>> ListAsync(
        bool lowStockOnly,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryTransactionListItemDto>> ListTransactionsAsync(
        Guid inventoryId,
        CancellationToken cancellationToken = default);

    Task<InventoryAdminOperationResult<InventoryListItemDto>> AdjustAsync(
        Guid inventoryId,
        int quantityChange,
        string? reason,
        string? transactionType,
        CancellationToken cancellationToken = default);

    Task<InventoryAdminOperationResult<InventoryListItemDto>> SetReorderLevelAsync(
        Guid inventoryId,
        int reorderLevel,
        CancellationToken cancellationToken = default);
}

public interface IStoreInventoryAdminService
{
    Task<IReadOnlyList<InventoryListItemDto>> ListAsync(
        bool lowStockOnly,
        CancellationToken cancellationToken = default);

    Task<InventoryAdminOperationResult<IReadOnlyList<InventoryTransactionListItemDto>>> ListTransactionsAsync(
        Guid inventoryId,
        CancellationToken cancellationToken = default);

    Task<InventoryAdminOperationResult<InventoryListItemDto>> AdjustAsync(
        Guid inventoryId,
        InventoryAdjustRequestDto request,
        CancellationToken cancellationToken = default);

    Task<InventoryAdminOperationResult<InventoryListItemDto>> SetReorderLevelAsync(
        Guid inventoryId,
        InventoryReorderLevelRequestDto request,
        CancellationToken cancellationToken = default);
}
