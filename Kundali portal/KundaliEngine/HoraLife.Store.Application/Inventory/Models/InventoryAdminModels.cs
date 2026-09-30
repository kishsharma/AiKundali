namespace HoraLife.Store.Application.Inventory.Models;

public enum InventoryAdminErrorCode
{
    None,
    InventoryNotFound,
    InvalidQuantityChange,
    InvalidReorderLevel,
    StockWouldFallBelowReserved,
    InvalidTransactionType
}

public sealed record InventoryAdminOperationResult<T>(
    T? Value,
    InventoryAdminErrorCode ErrorCode,
    string? ErrorMessage)
{
    public bool Succeeded => ErrorCode == InventoryAdminErrorCode.None;

    public static InventoryAdminOperationResult<T> Success(T value)
        => new(value, InventoryAdminErrorCode.None, null);

    public static InventoryAdminOperationResult<T> Failure(
        InventoryAdminErrorCode errorCode,
        string errorMessage)
        => new(default, errorCode, errorMessage);
}
