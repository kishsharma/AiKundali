using System.Text.Json;

namespace HoraLife.Store.Application.Products.Dtos;

public sealed record ProductDetailDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string CategorySlug,
    string Sku,
    string Slug,
    string Name,
    string? ShortDescription,
    string? Description,
    string ProductType,
    decimal BasePrice,
    decimal? CompareAtPrice,
    string Currency,
    JsonElement Attributes,
    bool RequiresConsultation,
    bool IsFeatured,
    bool InStock,
    int AvailableQuantity,
    IReadOnlyList<ProductVariantDto> Variants,
    IReadOnlyList<ProductImageDto> Images);
