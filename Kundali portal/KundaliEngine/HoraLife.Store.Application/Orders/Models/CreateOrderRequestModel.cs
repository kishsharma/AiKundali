namespace HoraLife.Store.Application.Orders.Models;

public sealed record CreateOrderRequestModel(
    Guid ShippingAddressId,
    Guid? BillingAddressId,
    bool UseShippingAsBilling,
    string? Notes);
