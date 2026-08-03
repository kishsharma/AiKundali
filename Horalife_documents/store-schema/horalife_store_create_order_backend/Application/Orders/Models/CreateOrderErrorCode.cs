namespace HoraLife.Store.Application.Orders.Models;

public enum CreateOrderErrorCode
{
    None,
    InvalidCustomerEmail,
    CartNotFound,
    CartEmpty,
    CartValidationFailed,
    ShippingAddressNotFound,
    BillingAddressNotFound,
    UnsupportedDeliveryLocation,
    CheckoutPreviewBlocked,
    OrderAlreadyExists
}
