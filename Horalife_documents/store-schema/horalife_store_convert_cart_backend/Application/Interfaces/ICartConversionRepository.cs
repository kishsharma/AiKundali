using HoraLife.Store.Application.Carts.Conversion.Models;

namespace HoraLife.Store.Application.Interfaces;

public interface ICartConversionRepository
{
    Task<CartConversionOperationResult> ConvertForOrderAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken = default);
}
