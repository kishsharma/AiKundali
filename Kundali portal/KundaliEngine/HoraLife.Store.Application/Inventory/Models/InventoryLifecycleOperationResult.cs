using HoraLife.Store.Application.Inventory.Dtos;

namespace HoraLife.Store.Application.Inventory.Models;

public sealed record InventoryLifecycleOperationResult(
    InventoryLifecycleDto? Value,
    InventoryReservationErrorCode ErrorCode,
    string? ErrorMessage)
{
    public bool Succeeded => ErrorCode == InventoryReservationErrorCode.None;

    public static InventoryLifecycleOperationResult Success(InventoryLifecycleDto value)
        => new(value, InventoryReservationErrorCode.None, null);

    public static InventoryLifecycleOperationResult Failure(
        InventoryReservationErrorCode errorCode,
        string errorMessage)
        => new(null, errorCode, errorMessage);
}
