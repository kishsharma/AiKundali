using HoraLife.Store.Application.Carts.Conversion.Models;

namespace HoraLife.Store.Application.Interfaces;

public interface IStoreCartConversionService
{
    Task<CartConversionOperationResult> ConvertAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken = default);
}
