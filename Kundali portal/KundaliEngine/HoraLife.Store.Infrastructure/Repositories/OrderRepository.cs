using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Domain.Entities;
using HoraLife.Store.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HoraLife.Store.Infrastructure.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly StoreDbContext _dbContext;

    public OrderRepository(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Order?> GetByCartIdAsync(
        Guid userId,
        Guid cartId,
        bool trackChanges,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Order> query =
            _dbContext.Orders
                .Where(order =>
                    order.UserId == userId &&
                    order.CartId == cartId);

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(
            cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(
        Guid userId,
        Guid orderId,
        bool trackChanges,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Order> query =
            _dbContext.Orders
                .Where(order =>
                    order.UserId == userId &&
                    order.Id == orderId);

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(
            cancellationToken);
    }

    public async Task AddAsync(
        Order order,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Orders.AddAsync(
            order,
            cancellationToken);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
