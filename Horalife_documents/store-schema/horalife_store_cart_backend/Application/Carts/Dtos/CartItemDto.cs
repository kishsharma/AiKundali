namespace HoraLife.Store.Application.Carts.Dtos;

public sealed record CartItemDto(
    Guid Id,
    Guid ProductId,
    Guid? VariantId,
    string ProductSlug,
    string ProductName,
    string? VariantName,
    string? Sku,
    string? ImageUrl,
    string? ImageAltText,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    string Currency,
    int AvailableQuantity,
    bool RequiresConsultation);
