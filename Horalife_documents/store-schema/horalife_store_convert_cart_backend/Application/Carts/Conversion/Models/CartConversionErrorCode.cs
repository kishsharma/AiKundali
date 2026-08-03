namespace HoraLife.Store.Application.Carts.Conversion.Models;

public enum CartConversionErrorCode
{
    None,
    OrderNotFound,
    OrderNotPending,
    OrderCartMissing,
    OrderItemsMissing,
    InventoryNotReserved,
    CartNotFound,
    CartOwnershipMismatch,
    CartAlreadyConverted,
    CartNotActive
}
