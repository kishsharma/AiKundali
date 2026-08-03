using HoraLife.Store.Application.Checkout.Revalidation.Dtos;
using HoraLife.Store.Application.Checkout.Revalidation.Models;

namespace HoraLife.Store.Application.Interfaces;

public interface IStoreCheckoutRevalidationService
{
    Task<CheckoutRevalidationOperationResult<CheckoutRevalidationDto>>
        RevalidateAsync(
            Guid userId,
            CancellationToken cancellationToken = default);
}
