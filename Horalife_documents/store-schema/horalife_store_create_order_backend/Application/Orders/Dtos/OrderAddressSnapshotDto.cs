namespace HoraLife.Store.Application.Orders.Dtos;

public sealed record OrderAddressSnapshotDto(
    Guid AddressId,
    string AddressType,
    string FullName,
    string Phone,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string CountryCode);
