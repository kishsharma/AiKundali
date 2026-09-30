using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Application.Inventory.Models;

namespace HoraLife.Store.Application.Services;

public sealed class StoreInventoryReservationService :
    IStoreInventoryReservationService
{
    private readonly IInventoryReservationRepository _reservationRepository;

    public StoreInventoryReservationService(
        IInventoryReservationRepository reservationRepository)
    {
        _reservationRepository = reservationRepository;
    }

    public Task<InventoryReservationOperationResult> ReserveAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return _reservationRepository.ReserveForOrderAsync(
            userId,
            orderId,
            cancellationToken);
    }

    public Task<InventoryLifecycleOperationResult> ConfirmAsync(
        Guid orderId,
        Guid? actingUserId,
        bool bypassOwnership,
        CancellationToken cancellationToken = default)
    {
        return _reservationRepository.ConfirmForOrderAsync(
            orderId,
            actingUserId,
            bypassOwnership,
            cancellationToken);
    }

    public Task<InventoryLifecycleOperationResult> ReleaseAsync(
        Guid orderId,
        Guid? actingUserId,
        bool bypassOwnership,
        string releaseReason,
        CancellationToken cancellationToken = default)
    {
        return _reservationRepository.ReleaseForOrderAsync(
            orderId,
            actingUserId,
            bypassOwnership,
            releaseReason,
            cancellationToken);
    }

    public Task<int> ReleaseExpiredAsync(
        TimeSpan maxAge,
        CancellationToken cancellationToken = default)
    {
        return _reservationRepository.ReleaseExpiredReservationsAsync(
            maxAge,
            cancellationToken);
    }
}
