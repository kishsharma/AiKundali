using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Domain.Entities;
using HoraLife.Store.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HoraLife.Store.Infrastructure.Repositories;

public sealed class OrderItemRepository : IOrderItemRepository
{
    private readonly StoreDbContext _dbContext;

    public OrderItemRepository(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> AnyForOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
        => _dbContext.OrderItems.AnyAsync(
            x => x.OrderId == orderId,
            cancellationToken);

    public async Task AddRangeAsync(
        IReadOnlyCollection<OrderItem> orderItems,
        CancellationToken cancellationToken = default)
        => await _dbContext.OrderItems.AddRangeAsync(orderItems, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);
}
