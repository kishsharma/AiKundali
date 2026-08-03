namespace HoraLife.Store.Application.Carts.Conversion.Dtos;

public sealed record ConvertedCartDto(
    Guid CartId,
    Guid OrderId,
    long OrderNumber,
    Guid UserId,
    string PreviousCartStatus,
    string CurrentCartStatus,
    string OrderStatus,
    string PaymentStatus,
    int OrderItemsCount,
    bool InventoryReserved,
    bool CartConverted,
    bool NewActiveCartCreated,
    DateTimeOffset ConvertedAt);
