namespace HoraLife.Store.Application.Checkout.Dtos;

public sealed record CheckoutPreviewDto(
    Guid CartId,
    string Currency,
    CheckoutAddressDto ShippingAddress,
    CheckoutAddressDto BillingAddress,
    bool BillingSameAsShipping,
    IReadOnlyList<CheckoutPreviewItemDto> Items,
    int TotalQuantity,
    decimal Subtotal,
    decimal ShippingCharge,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal GrandTotal,
    int EstimatedDeliveryMinDays,
    int EstimatedDeliveryMaxDays,
    bool RequiresConsultation,
    bool CanCheckout,
    IReadOnlyList<string> Warnings,
    DateTimeOffset GeneratedAt);
