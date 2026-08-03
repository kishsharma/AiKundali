using System.ComponentModel.DataAnnotations;
using HoraLife.Store.Application.Addresses.Models;
using HoraLife.Store.Domain.Enums;

namespace KundaliEngine.Api.DTOs.Store.Addresses;

public sealed class SaveCustomerAddressRequestDto
{
    [Required]
    [MaxLength(20)]
    public string AddressType { get; init; } = "shipping";

    [Required]
    [MinLength(2)]
    [MaxLength(200)]
    public string FullName { get; init; } = string.Empty;

    [Required]
    [MinLength(7)]
    [MaxLength(30)]
    public string Phone { get; init; } = string.Empty;

    [Required]
    [MinLength(3)]
    [MaxLength(250)]
    public string AddressLine1 { get; init; } = string.Empty;

    [MaxLength(250)]
    public string? AddressLine2 { get; init; }

    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public string City { get; init; } = string.Empty;

    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public string State { get; init; } = string.Empty;

    [Required]
    [MinLength(3)]
    [MaxLength(20)]
    public string PostalCode { get; init; } = string.Empty;

    [Required]
    [RegularExpression(
        "^[A-Za-z]{2}$",
        ErrorMessage =
            "CountryCode must be a two-letter ISO country code.")]
    public string CountryCode { get; init; } = "IN";

    public bool IsDefault { get; init; }

    public bool TryToModel(
        out SaveCustomerAddressModel model,
        out string? validationError)
    {
        var normalizedType = AddressType
            .Trim()
            .ToLowerInvariant();

        var parsedAddressType = normalizedType switch
        {
            "shipping" => HoraLife.Store.Domain.Enums.AddressType.Shipping,
            "billing" => HoraLife.Store.Domain.Enums.AddressType.Billing,
            _ => (HoraLife.Store.Domain.Enums.AddressType?)null
        };

        if (!parsedAddressType.HasValue)
        {
            model = default!;
            validationError =
                "AddressType must be either 'shipping' or 'billing'.";

            return false;
        }

        model = new SaveCustomerAddressModel(
            parsedAddressType.Value,
            FullName,
            Phone,
            AddressLine1,
            AddressLine2,
            City,
            State,
            PostalCode,
            CountryCode,
            IsDefault);

        validationError = null;
        return true;
    }
}
