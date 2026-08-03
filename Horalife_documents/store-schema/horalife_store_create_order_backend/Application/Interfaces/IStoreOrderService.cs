using HoraLife.Store.Application.Orders.Models;

namespace HoraLife.Store.Application.Interfaces;

public interface IStoreOrderService
{
    Task<CreateOrderOperationResult> CreateAsync(
        Guid userId,
        string customerEmail,
        CreateOrderRequestModel request,
        CancellationToken cancellationToken = default);
}
