namespace HoraLife.Store.Application.Orders.Items.Models;

public enum CreateOrderItemsErrorCode
{
    None,
    OrderNotFound,
    OrderNotPending,
    OrderCartMissing,
    ActiveCartNotFound,
    CartMismatch,
    CartEmpty,
    CartChanged,
    OrderItemsAlreadyExist
}
