namespace HoraLife.Store.Application.Checkout.Models;

public enum CheckoutPreviewErrorCode
{
    None,
    CartNotFound,
    CartEmpty,
    ShippingAddressNotFound,
    BillingAddressNotFound,
    UnsupportedDeliveryLocation
}
