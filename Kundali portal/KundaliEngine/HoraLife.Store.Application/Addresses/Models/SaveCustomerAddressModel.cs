using HoraLife.Store.Domain.Enums;

namespace HoraLife.Store.Application.Addresses.Models;

public sealed record SaveCustomerAddressModel(
    AddressType AddressType,
    string FullName,
    string Phone,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string CountryCode,
    bool IsDefault);
