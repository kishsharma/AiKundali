using HoraLife.Store.Application.Orders.Items.Models;

namespace HoraLife.Store.Application.Interfaces;

public interface IStoreOrderItemService
{
    Task<CreateOrderItemsOperationResult> CreateAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken = default);
}
