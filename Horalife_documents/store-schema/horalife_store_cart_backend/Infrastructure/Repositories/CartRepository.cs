using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Domain.Entities;
using HoraLife.Store.Domain.Enums;
using HoraLife.Store.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HoraLife.Store.Infrastructure.Repositories;

public sealed class CartRepository : ICartRepository
{
    private readonly StoreDbContext _dbContext;

    public CartRepository(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Cart?> GetActiveByUserIdAsync(
        Guid userId,
        bool trackChanges,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Cart> query = _dbContext.Carts
            .Where(cart =>
                cart.UserId == userId &&
                cart.Status == CartStatus.Active);

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query
            .Include(cart => cart.Items)
                .ThenInclude(item => item.Product)
                    .ThenInclude(product => product.Images)
            .Include(cart => cart.Items)
                .ThenInclude(item => item.Product)
                    .ThenInclude(product => product.InventoryRecords)
            .Include(cart => cart.Items)
                .ThenInclude(item => item.Variant)
                    .ThenInclude(variant => variant!.InventoryRecords)
            .AsSplitQuery()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Product?> GetActiveProductForCartAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Where(product =>
                product.Id == productId &&
                product.IsActive)
            .Include(product => product.Images)
            .Include(product => product.InventoryRecords)
            .Include(product => product.Variants
                .Where(variant => variant.IsActive))
                .ThenInclude(variant => variant.InventoryRecords)
            .AsSplitQuery()
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task AddCartAsync(
        Cart cart,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Carts.AddAsync(
            cart,
            cancellationToken);
    }

    public void RemoveItem(CartItem item)
    {
        _dbContext.CartItems.Remove(item);
    }

    public void RemoveItems(IEnumerable<CartItem> items)
    {
        _dbContext.CartItems.RemoveRange(items);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
