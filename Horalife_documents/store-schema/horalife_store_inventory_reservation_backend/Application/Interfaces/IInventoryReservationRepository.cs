using HoraLife.Store.Application.Inventory.Models;

namespace HoraLife.Store.Application.Interfaces;

public interface IInventoryReservationRepository
{
    Task<InventoryReservationOperationResult> ReserveForOrderAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken = default);
}
