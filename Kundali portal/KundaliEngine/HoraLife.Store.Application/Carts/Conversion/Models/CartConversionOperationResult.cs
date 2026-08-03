using HoraLife.Store.Application.Carts.Conversion.Dtos;

namespace HoraLife.Store.Application.Carts.Conversion.Models;

public sealed record CartConversionOperationResult(
    ConvertedCartDto? Value,
    CartConversionErrorCode ErrorCode,
    string? ErrorMessage)
{
    public bool Succeeded =>
        ErrorCode == CartConversionErrorCode.None;

    public static CartConversionOperationResult Success(
        ConvertedCartDto value)
        => new(value, CartConversionErrorCode.None, null);

    public static CartConversionOperationResult Failure(
        CartConversionErrorCode errorCode,
        string errorMessage)
        => new(null, errorCode, errorMessage);
}
