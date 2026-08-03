using HoraLife.Store.Domain.Entities;

namespace HoraLife.Store.Application.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByCartIdAsync(
        Guid userId,
        Guid cartId,
        bool trackChanges,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(
        Guid userId,
        Guid orderId,
        bool trackChanges,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
