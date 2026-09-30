using HoraLife.Store.Application.Inventory.Models;

namespace HoraLife.Store.Application.Interfaces;

public interface IInventoryReservationRepository
{
    Task<InventoryReservationOperationResult> ReserveForOrderAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task<InventoryLifecycleOperationResult> ConfirmForOrderAsync(
        Guid orderId,
        Guid? actingUserId,
        bool bypassOwnership,
        CancellationToken cancellationToken = default);

    Task<InventoryLifecycleOperationResult> ReleaseForOrderAsync(
        Guid orderId,
        Guid? actingUserId,
        bool bypassOwnership,
        string releaseReason,
        CancellationToken cancellationToken = default);

    Task<int> ReleaseExpiredReservationsAsync(
        TimeSpan maxAge,
        CancellationToken cancellationToken = default);
}
