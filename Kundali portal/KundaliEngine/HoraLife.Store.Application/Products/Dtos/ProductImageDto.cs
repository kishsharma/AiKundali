namespace HoraLife.Store.Application.Products.Dtos;

public sealed record ProductImageDto(
    Guid Id,
    Guid? VariantId,
    string ImageUrl,
    string? AltText,
    int DisplayOrder,
    bool IsPrimary);
