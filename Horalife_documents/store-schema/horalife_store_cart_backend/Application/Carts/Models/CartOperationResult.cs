namespace HoraLife.Store.Application.Carts.Models;

public sealed record CartOperationResult<T>(
    T? Value,
    CartErrorCode ErrorCode,
    string? ErrorMessage)
{
    public bool Succeeded => ErrorCode == CartErrorCode.None;

    public static CartOperationResult<T> Success(T value)
        => new(value, CartErrorCode.None, null);

    public static CartOperationResult<T> Failure(
        CartErrorCode errorCode,
        string errorMessage)
        => new(default, errorCode, errorMessage);
}
