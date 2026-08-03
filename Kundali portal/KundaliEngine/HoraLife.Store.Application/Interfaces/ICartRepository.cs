using HoraLife.Store.Domain.Entities;

namespace HoraLife.Store.Application.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetActiveByUserIdAsync(Guid userId, bool trackChanges,CancellationToken cancellationToken = default);

    Task<Product?> GetActiveProductForCartAsync( Guid productId, CancellationToken cancellationToken = default);
    Task AddItemAsync(CartItem item, CancellationToken cancellationToken = default);
    Task AddCartAsync( Cart cart,CancellationToken cancellationToken = default);

    void RemoveItem(CartItem item);

    void RemoveItems(IEnumerable<CartItem> items);

    Task<int> SaveChangesAsync( CancellationToken cancellationToken = default);
}
