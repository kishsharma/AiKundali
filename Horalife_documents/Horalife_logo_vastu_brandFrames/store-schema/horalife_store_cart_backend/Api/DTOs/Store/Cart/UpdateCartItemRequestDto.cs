using System.ComponentModel.DataAnnotations;

namespace KundaliEngine.Api.DTOs.Store.Cart;

public sealed class UpdateCartItemRequestDto
{
    [Range(1, 99)]
    public int Quantity { get; init; }
}
