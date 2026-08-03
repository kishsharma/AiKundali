using HoraLife.Store.Application.Inventory.Dtos;

namespace HoraLife.Store.Application.Inventory.Models;

public sealed record InventoryReservationOperationResult(
    InventoryReservationDto? Value,
    InventoryReservationErrorCode ErrorCode,
    string? ErrorMessage)
{
    public bool Succeeded =>
        ErrorCode == InventoryReservationErrorCode.None;

    public static InventoryReservationOperationResult Success(
        InventoryReservationDto value)
        => new(
            value,
            InventoryReservationErrorCode.None,
            null);

    public static InventoryReservationOperationResult Failure(
        InventoryReservationErrorCode errorCode,
        string errorMessage)
        => new(
            null,
            errorCode,
            errorMessage);
}
