namespace HoraLife.Store.Application.Inventory.Dtos;

public sealed record InventoryReservationAllocationDto(
    Guid InventoryId,
    Guid InventoryTransactionId,
    int StockQuantity,
    int ReservedQuantityBefore,
    int ReservedNow,
    int ReservedQuantityAfter,
    int AvailableQuantityAfter);
