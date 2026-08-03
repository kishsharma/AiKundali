namespace HoraLife.Store.Application.Checkout.Dtos;

public sealed record CheckoutPreviewItemDto(
    Guid CartItemId,
    Guid ProductId,
    Guid? VariantId,
    string ProductSlug,
    string ProductName,
    string? VariantName,
    string? Sku,
    string? ImageUrl,
    decimal CartUnitPrice,
    decimal CurrentUnitPrice,
    bool PriceChanged,
    int Quantity,
    decimal LineTotal,
    int AvailableQuantity,
    bool ProductActive,
    bool VariantActive,
    bool IsAvailable,
    bool RequiresConsultation,
    IReadOnlyList<string> Warnings);
