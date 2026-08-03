using HoraLife.Store.Application.Checkout.Dtos;
using HoraLife.Store.Application.Checkout.Models;

namespace HoraLife.Store.Application.Interfaces;

public interface IStoreCheckoutPreviewService
{
    Task<CheckoutPreviewResult<CheckoutPreviewDto>> CreatePreviewAsync(
        Guid userId,
        CheckoutPreviewRequestModel request,
        CancellationToken cancellationToken = default);
}
