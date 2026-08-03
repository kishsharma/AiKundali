namespace HoraLife.Store.Application.Orders.Dtos;

public sealed record CreatedOrderDto(
    Guid OrderId,
    long OrderNumber,
    Guid CartId,
    Guid UserId,
    string CustomerEmail,
    string? CustomerPhone,
    string OrderStatus,
    string PaymentStatus,
    decimal SubtotalAmount,
    decimal DiscountAmount,
    decimal ShippingAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Currency,
    OrderAddressSnapshotDto ShippingAddress,
    OrderAddressSnapshotDto BillingAddress,
    bool BillingSameAsShipping,
    string OrderSource,
    string? Notes,
    bool OrderItemsCreated,
    DateTimeOffset PlacedAt);
