using HoraLife.Store.Application.Orders.Items.Dtos;

namespace HoraLife.Store.Application.Orders.Items.Models;

public sealed record CreateOrderItemsOperationResult(
    CreatedOrderItemsDto? Value,
    CreateOrderItemsErrorCode ErrorCode,
    string? ErrorMessage)
{
    public bool Succeeded => ErrorCode == CreateOrderItemsErrorCode.None;

    public static CreateOrderItemsOperationResult Success(CreatedOrderItemsDto value)
        => new(value, CreateOrderItemsErrorCode.None, null);

    public static CreateOrderItemsOperationResult Failure(
        CreateOrderItemsErrorCode errorCode,
        string errorMessage)
        => new(null, errorCode, errorMessage);
}
