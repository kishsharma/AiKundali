using HoraLife.Store.Application.Inventory.Models;

namespace HoraLife.Store.Application.Interfaces;

public interface IStoreInventoryReservationService
{
    Task<InventoryReservationOperationResult> ReserveAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken = default);
}
