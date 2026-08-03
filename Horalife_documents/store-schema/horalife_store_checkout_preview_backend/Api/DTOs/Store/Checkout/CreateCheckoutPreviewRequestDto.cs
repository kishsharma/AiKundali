using System.ComponentModel.DataAnnotations;
using HoraLife.Store.Application.Checkout.Models;

namespace KundaliEngine.Api.DTOs.Store.Checkout;

public sealed class CreateCheckoutPreviewRequestDto
{
    [Required]
    public Guid ShippingAddressId { get; init; }

    public Guid? BillingAddressId { get; init; }

    public bool UseShippingAsBilling { get; init; } = true;

    public CheckoutPreviewRequestModel ToModel()
        => new(ShippingAddressId, BillingAddressId, UseShippingAsBilling);
}
