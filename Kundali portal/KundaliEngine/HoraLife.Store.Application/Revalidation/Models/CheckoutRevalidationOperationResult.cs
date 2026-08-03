namespace HoraLife.Store.Application.Checkout.Revalidation.Models;

public sealed record CheckoutRevalidationOperationResult<T>(
    T? Value,
    CheckoutRevalidationErrorCode ErrorCode,
    string? ErrorMessage)
{
    public bool Succeeded =>
        ErrorCode == CheckoutRevalidationErrorCode.None;

    public static CheckoutRevalidationOperationResult<T> Success(
        T value)
        => new(
            value,
            CheckoutRevalidationErrorCode.None,
            null);

    public static CheckoutRevalidationOperationResult<T> Failure(
        CheckoutRevalidationErrorCode errorCode,
        string errorMessage)
        => new(
            default,
            errorCode,
            errorMessage);
}
