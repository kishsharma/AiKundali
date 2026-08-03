namespace HoraLife.Store.Application.Inventory.Models;

public enum InventoryReservationErrorCode
{
    None,
    OrderNotFound,
    OrderNotPending,
    OrderItemsMissing,
    OrderItemProductMissing,
    InventoryRecordMissing,
    InsufficientStock,
    InventoryAlreadyReserved
}
