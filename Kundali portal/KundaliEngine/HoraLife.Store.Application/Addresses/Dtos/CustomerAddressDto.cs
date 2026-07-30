namespace HoraLife.Store.Application.Addresses.Dtos;

public sealed record CustomerAddressDto(
    Guid Id,
    string AddressType,
    string FullName,
    string Phone,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string CountryCode,
    bool IsDefault,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
