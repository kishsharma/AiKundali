namespace HoraLife.Store.Application.Checkout.Revalidation.Dtos;

public sealed record CheckoutRevalidationItemDto(
    Guid CartItemId,
    Guid ProductId,
    Guid? VariantId,
    string ProductName,
    string? VariantName,
    string Sku,
    int Quantity,
    decimal PreviousUnitPrice,
    decimal CurrentUnitPrice,
    bool PriceChanged,
    int AvailableQuantity,
    bool ProductActive,
    bool VariantActive,
    bool HasSufficientStock,
    bool CurrencyMatches,
    bool IsValid,
    IReadOnlyList<CheckoutRevalidationIssueDto> Issues);
