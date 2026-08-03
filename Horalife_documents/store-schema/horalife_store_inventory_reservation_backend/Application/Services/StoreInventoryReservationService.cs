using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Application.Inventory.Models;

namespace HoraLife.Store.Application.Services;

public sealed class StoreInventoryReservationService :
    IStoreInventoryReservationService
{
    private readonly IInventoryReservationRepository
        _reservationRepository;

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
}
