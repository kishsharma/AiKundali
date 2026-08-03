namespace HoraLife.Store.Application.Checkout.Revalidation.Dtos;

public sealed record CheckoutRevalidationDto(
    Guid CartId,
    string Currency,
    bool IsValid,
    bool RequiresNewPreview,
    bool CartPricesUpdated,
    decimal PreviousSubtotal,
    decimal CurrentSubtotal,
    IReadOnlyList<CheckoutRevalidationItemDto> Items,
    IReadOnlyList<CheckoutRevalidationIssueDto> Issues,
    DateTimeOffset ValidatedAt);
