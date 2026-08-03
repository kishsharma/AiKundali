using HoraLife.Store.Application.Checkout.Revalidation.Dtos;
using HoraLife.Store.Application.Orders.Dtos;

namespace HoraLife.Store.Application.Orders.Models;

public sealed record CreateOrderOperationResult(
    CreatedOrderDto? Value,
    CreateOrderErrorCode ErrorCode,
    string? ErrorMessage,
    CheckoutRevalidationDto? Revalidation)
{
    public bool Succeeded =>
        ErrorCode == CreateOrderErrorCode.None;

    public static CreateOrderOperationResult Success(
        CreatedOrderDto value)
        => new(
            value,
            CreateOrderErrorCode.None,
            null,
            null);

    public static CreateOrderOperationResult Failure(
        CreateOrderErrorCode errorCode,
        string errorMessage,
        CheckoutRevalidationDto? revalidation = null)
        => new(
            null,
            errorCode,
            errorMessage,
            revalidation);
}
