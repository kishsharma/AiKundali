namespace HoraLife.Store.Application.Checkout.Dtos;

public sealed record CheckoutAddressDto(
    Guid Id,
    string AddressType,
    string FullName,
    string Phone,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string CountryCode);
