using System.Text.Json;

namespace HoraLife.Store.Application.Products.Dtos;

public sealed record ProductVariantDto(
    Guid Id,
    string Sku,
    string Name,
    decimal Price,
    decimal? CompareAtPrice,
    decimal? WeightGrams,
    JsonElement Attributes,
    int AvailableQuantity,
    bool IsActive);
