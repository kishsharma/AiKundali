using HoraLife.Store.Application.Inventory.Models;

namespace HoraLife.Store.Application.Interfaces;

public interface IStoreInventoryReservationService
{
    Task<InventoryReservationOperationResult> ReserveAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<InventoryLifecycleOperationResult> ConfirmAsync(
        Guid orderId,
        Guid? actingUserId,
        bool bypassOwnership,
        CancellationToken cancellationToken = default);

    Task<InventoryLifecycleOperationResult> ReleaseAsync(
        Guid orderId,
        Guid? actingUserId,
        bool bypassOwnership,
        string releaseReason,
        CancellationToken cancellationToken = default);

    Task<int> ReleaseExpiredAsync(
        TimeSpan maxAge,
        CancellationToken cancellationToken = default);
}
