namespace HoraLife.Store.Application.Orders.Items.Dtos;

public sealed record CreatedOrderItemDto(
    Guid OrderItemId,
    Guid ProductId,
    Guid? VariantId,
    string ProductName,
    string? VariantName,
    string Sku,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    bool RequiresConsultation);
