using HoraLife.Store.Domain.Entities;

namespace HoraLife.Store.Application.Interfaces;

public interface IOrderItemRepository
{
    Task<bool> AnyForOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IReadOnlyCollection<OrderItem> orderItems,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
