using System.ComponentModel.DataAnnotations;

namespace KundaliEngine.Api.DTOs.Store.Cart;

public sealed class AddCartItemRequestDto
{
    [Required]
    public Guid ProductId { get; init; }

    public Guid? VariantId { get; init; }

    [Range(1, 99)]
    public int Quantity { get; init; } = 1;
}
