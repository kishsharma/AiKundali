using System.ComponentModel.DataAnnotations;

namespace KundaliEngine.API.DTOs.Store.Cart;

public sealed class UpdateCartItemRequestDto
{
    [Range(1, 99)]
    public int Quantity { get; init; }
}
