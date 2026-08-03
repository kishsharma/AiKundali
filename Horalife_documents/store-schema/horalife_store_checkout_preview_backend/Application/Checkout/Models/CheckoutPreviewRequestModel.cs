namespace HoraLife.Store.Application.Checkout.Models;

public sealed record CheckoutPreviewRequestModel(
    Guid ShippingAddressId,
    Guid? BillingAddressId,
    bool UseShippingAsBilling);
