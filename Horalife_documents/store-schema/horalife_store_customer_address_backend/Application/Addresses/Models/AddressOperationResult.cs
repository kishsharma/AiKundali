namespace HoraLife.Store.Application.Addresses.Models;

public sealed record AddressOperationResult<T>(
    T? Value,
    AddressErrorCode ErrorCode,
    string? ErrorMessage)
{
    public bool Succeeded => ErrorCode == AddressErrorCode.None;

    public static AddressOperationResult<T> Success(T value)
        => new(value, AddressErrorCode.None, null);

    public static AddressOperationResult<T> Failure(
        AddressErrorCode errorCode,
        string errorMessage)
        => new(default, errorCode, errorMessage);
}
