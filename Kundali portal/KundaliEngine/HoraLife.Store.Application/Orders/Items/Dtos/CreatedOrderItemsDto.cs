namespace HoraLife.Store.Application.Orders.Items.Dtos;

public sealed record CreatedOrderItemsDto(
    Guid OrderId,
    long OrderNumber,
    Guid CartId,
    string Currency,
    int TotalQuantity,
    decimal OrderSubtotal,
    decimal ItemsSubtotal,
    bool TotalsMatch,
    bool OrderItemsCreated,
    IReadOnlyList<CreatedOrderItemDto> Items,
    DateTimeOffset CreatedAt);
