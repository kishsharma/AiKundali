using HoraLife.Store.Application.Carts.Conversion.Models;
using HoraLife.Store.Application.Interfaces;

namespace HoraLife.Store.Application.Services;

public sealed class StoreCartConversionService :
    IStoreCartConversionService
{
    private readonly ICartConversionRepository
        _cartConversionRepository;

    public StoreCartConversionService(
        ICartConversionRepository cartConversionRepository)
    {
        _cartConversionRepository =
            cartConversionRepository;
    }

    public Task<CartConversionOperationResult> ConvertAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return _cartConversionRepository.ConvertForOrderAsync(
            userId,
            orderId,
            cancellationToken);
    }
}
