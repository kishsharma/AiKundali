using HoraLife.Store.Application.Checkout.Revalidation.Dtos;

namespace KundaliEngine.Api.DTOs.Store.Orders;

public sealed record CreateOrderConflictDto(
    string Code,
    string Message,
    CheckoutRevalidationDto? Revalidation);
