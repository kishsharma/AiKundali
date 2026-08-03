namespace HoraLife.Store.Application.Inventory.Dtos;

public sealed record InventoryReservationDto(
    Guid OrderId,
    long OrderNumber,
    Guid UserId,
    string OrderStatus,
    string PaymentStatus,
    bool InventoryReserved,
    bool StockQuantityDeducted,
    int TotalReservedQuantity,
    IReadOnlyList<InventoryReservationItemDto> Items,
    DateTimeOffset ReservedAt);
