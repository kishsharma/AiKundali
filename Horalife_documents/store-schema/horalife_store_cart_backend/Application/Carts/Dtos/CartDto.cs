namespace HoraLife.Store.Application.Carts.Dtos;

public sealed record CartDto(
    Guid? Id,
    string Currency,
    int TotalQuantity,
    decimal Subtotal,
    IReadOnlyList<CartItemDto> Items)
{
    public static CartDto Empty(string currency = "INR")
        => new(
            null,
            currency,
            0,
            0m,
            Array.Empty<CartItemDto>());
}
