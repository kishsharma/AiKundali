using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Domain.Entities;
using HoraLife.Store.Domain.Enums;
using HoraLife.Store.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HoraLife.Store.Infrastructure.Repositories;

public sealed class CartRepository : ICartRepository
{
    private readonly StoreDbContext _dbContext;
    private readonly ILogger<CartRepository> _logger;

    public CartRepository(StoreDbContext dbContext, ILogger<CartRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Cart?> GetActiveByUserIdAsync(  Guid userId,   bool trackChanges,  CancellationToken cancellationToken = default)
    {
        IQueryable<Cart> query = _dbContext.Carts
            .Where(cart =>cart.UserId == userId && cart.Status == CartStatus.Active);

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

    public async Task<Product?> GetActiveProductForCartAsync( Guid productId,  CancellationToken cancellationToken = default)
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

    public async Task AddItemAsync( CartItem item, CancellationToken cancellationToken = default)
    {
        await _dbContext.CartItems.AddAsync( item, cancellationToken);
    }
    public async Task AddCartAsync(  Cart cart,  CancellationToken cancellationToken = default)
    {
        await _dbContext.Carts.AddAsync(  cart, cancellationToken);
    }

    public void RemoveItem(CartItem item)
    {
        _dbContext.CartItems.Remove(item);
    }

    public void RemoveItems(IEnumerable<CartItem> items)
    {
        _dbContext.CartItems.RemoveRange(items);
    }

    public async Task<int> SaveChangesAsync( CancellationToken cancellationToken = default)
    {

        try
        {
            return await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogError(
                exception,
                "Cart concurrency exception was caught. Entries: {Count}",
                exception.Entries.Count);

            foreach (var entry in exception.Entries)
            {
                _logger.LogError(
                    "Concurrency entity: {Entity}; state: {State}",
                    entry.Metadata.ClrType.Name,
                    entry.State);

                var databaseValues =
                    await entry.GetDatabaseValuesAsync(    cancellationToken);

                _logger.LogError(
                    databaseValues is null
                        ? "Database row does not exist."
                        : "Database row exists but concurrency values differ.");
            }

            throw;
        }
        //return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
