using System.ComponentModel.DataAnnotations;
using HoraLife.Store.Application.Orders.Models;

namespace KundaliEngine.Api.DTOs.Store.Orders;

public sealed class CreateOrderRequestDto
{
    [Required]
    public Guid ShippingAddressId { get; init; }

    public Guid? BillingAddressId { get; init; }

    public bool UseShippingAsBilling { get; init; } = true;

    [MaxLength(1000)]
    public string? Notes { get; init; }

    public CreateOrderRequestModel ToModel()
    {
        return new CreateOrderRequestModel(
            ShippingAddressId,
            BillingAddressId,
            UseShippingAsBilling,
            Notes);
    }
}
