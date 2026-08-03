namespace HoraLife.Store.Application.Inventory.Dtos;

public sealed record InventoryReservationItemDto(
    Guid OrderItemId,
    Guid ProductId,
    Guid? VariantId,
    string ProductName,
    string? VariantName,
    string Sku,
    int RequestedQuantity,
    int AvailableQuantityBefore,
    int ReservedQuantity,
    int AvailableQuantityAfter,
    IReadOnlyList<InventoryReservationAllocationDto> Allocations);
