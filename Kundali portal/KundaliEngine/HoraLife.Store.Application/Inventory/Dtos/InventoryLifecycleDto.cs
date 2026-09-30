namespace HoraLife.Store.Application.Inventory.Dtos;

public sealed record InventoryLifecycleDto(
    Guid OrderId,
    long OrderNumber,
    Guid? UserId,
    string OrderStatus,
    string PaymentStatus,
    bool InventoryReserved,
    bool StockQuantityDeducted,
    bool ReservationReleased,
    int TotalQuantity,
    DateTimeOffset CompletedAt);
