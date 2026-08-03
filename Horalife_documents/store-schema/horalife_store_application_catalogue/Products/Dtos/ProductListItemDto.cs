namespace HoraLife.Store.Application.Products.Dtos;

public sealed record ProductListItemDto(
    Guid Id,
    string Sku,
    string Slug,
    string Name,
    string? ShortDescription,
    string ProductType,
    decimal DisplayPrice,
    decimal? CompareAtPrice,
    string Currency,
    string? PrimaryImageUrl,
    string? PrimaryImageAltText,
    bool RequiresConsultation,
    bool IsFeatured,
    bool InStock,
    int AvailableQuantity);
