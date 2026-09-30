using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Application.Inventory.Dtos;
using HoraLife.Store.Application.Inventory.Models;

namespace HoraLife.Store.Application.Services;

public sealed class StoreInventoryAdminService : IStoreInventoryAdminService
{
    private readonly IInventoryAdminRepository _repository;

    public StoreInventoryAdminService(IInventoryAdminRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<InventoryListItemDto>> ListAsync(
        bool lowStockOnly,
        CancellationToken cancellationToken = default)
    {
        return _repository.ListAsync(lowStockOnly, cancellationToken);
    }

    public async Task<InventoryAdminOperationResult<IReadOnlyList<InventoryTransactionListItemDto>>> ListTransactionsAsync(
        Guid inventoryId,
        CancellationToken cancellationToken = default)
    {
        var list = await _repository.ListAsync(lowStockOnly: false, cancellationToken);
        if (list.All(item => item.Id != inventoryId))
        {
            return InventoryAdminOperationResult<IReadOnlyList<InventoryTransactionListItemDto>>.Failure(
                InventoryAdminErrorCode.InventoryNotFound,
                "Inventory record was not found.");
        }

        var transactions = await _repository.ListTransactionsAsync(inventoryId, cancellationToken);
        return InventoryAdminOperationResult<IReadOnlyList<InventoryTransactionListItemDto>>.Success(transactions);
    }

    public Task<InventoryAdminOperationResult<InventoryListItemDto>> AdjustAsync(
        Guid inventoryId,
        InventoryAdjustRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return _repository.AdjustAsync(
            inventoryId,
            request.QuantityChange,
            request.Reason,
            request.TransactionType,
            cancellationToken);
    }

    public Task<InventoryAdminOperationResult<InventoryListItemDto>> SetReorderLevelAsync(
        Guid inventoryId,
        InventoryReorderLevelRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return _repository.SetReorderLevelAsync(
            inventoryId,
            request.ReorderLevel,
            cancellationToken);
    }
}
