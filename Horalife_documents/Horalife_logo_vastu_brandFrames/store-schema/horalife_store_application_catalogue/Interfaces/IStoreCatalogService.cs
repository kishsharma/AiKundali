using HoraLife.Store.Application.Categories.Dtos;
using HoraLife.Store.Application.Common;
using HoraLife.Store.Application.Products.Dtos;
using HoraLife.Store.Application.Products.Models;

namespace HoraLife.Store.Application.Interfaces;

public interface IStoreCatalogService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default);

    Task<PagedResult<ProductListItemDto>> GetProductsAsync(
        ProductSearchFilter filter,
        CancellationToken cancellationToken = default);

    Task<ProductDetailDto?> GetProductBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default);
}
