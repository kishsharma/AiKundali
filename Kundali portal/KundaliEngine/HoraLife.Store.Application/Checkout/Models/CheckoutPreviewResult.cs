namespace HoraLife.Store.Application.Checkout.Models;

public sealed record CheckoutPreviewResult<T>(
    T? Value,
    CheckoutPreviewErrorCode ErrorCode,
    string? ErrorMessage)
{
    public bool Succeeded => ErrorCode == CheckoutPreviewErrorCode.None;

    public static CheckoutPreviewResult<T> Success(T value)
        => new(value, CheckoutPreviewErrorCode.None, null);

    public static CheckoutPreviewResult<T> Failure(
        CheckoutPreviewErrorCode errorCode,
        string errorMessage)
        => new(default, errorCode, errorMessage);
}
