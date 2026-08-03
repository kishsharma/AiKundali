namespace HoraLife.Store.Application.Carts.Models;

public enum CartErrorCode
{
    None,
    InvalidQuantity,
    ProductNotFound,
    VariantRequired,
    VariantNotFound,
    InsufficientStock,
    CartItemNotFound,
    CurrencyMismatch
}
