using HoraLife.Store.Application.Products.Models;
using HoraLife.Store.Domain.Entities;

namespace HoraLife.Store.Application.Interfaces;

public interface IProductRepository
{
    Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchAsync(
        ProductSearchFilter filter,
        CancellationToken cancellationToken = default);

    Task<Product?> GetActiveBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);
}
