using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Application.Products.Models;
using HoraLife.Store.Domain.Entities;
using HoraLife.Store.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HoraLife.Store.Infrastructure.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly StoreDbContext _dbContext;

    public ProductRepository(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> SearchAsync(
        ProductSearchFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var normalizedFilter = filter.Normalize();

        IQueryable<Product> query = _dbContext.Products
            .AsNoTracking()
            .Where(product => product.IsActive);

        if (!string.IsNullOrWhiteSpace(normalizedFilter.CategorySlug))
        {
            query = query.Where(product =>
                product.Category.IsActive &&
                product.Category.Slug == normalizedFilter.CategorySlug);
        }

        if (!string.IsNullOrWhiteSpace(normalizedFilter.Search))
        {
            var searchPattern = $"%{normalizedFilter.Search}%";

            query = query.Where(product =>
                EF.Functions.ILike(product.Name, searchPattern) ||
                EF.Functions.ILike(product.Sku, searchPattern) ||
                EF.Functions.ILike(product.Slug, searchPattern) ||
                (product.ShortDescription != null &&
                 EF.Functions.ILike(
                     product.ShortDescription,
                     searchPattern)));
        }

        if (normalizedFilter.ProductType.HasValue)
        {
            query = query.Where(product =>
                product.ProductType == normalizedFilter.ProductType.Value);
        }

        if (normalizedFilter.IsFeatured.HasValue)
        {
            query = query.Where(product =>
                product.IsFeatured == normalizedFilter.IsFeatured.Value);
        }

        if (normalizedFilter.MinimumPrice.HasValue)
        {
            query = query.Where(product =>
                product.BasePrice >= normalizedFilter.MinimumPrice.Value ||
                product.Variants.Any(variant =>
                    variant.IsActive &&
                    variant.Price >= normalizedFilter.MinimumPrice.Value));
        }

        if (normalizedFilter.MaximumPrice.HasValue)
        {
            query = query.Where(product =>
                product.BasePrice <= normalizedFilter.MaximumPrice.Value ||
                product.Variants.Any(variant =>
                    variant.IsActive &&
                    variant.Price <= normalizedFilter.MaximumPrice.Value));
        }

        if (normalizedFilter.InStockOnly == true)
        {
            query = query.Where(product =>
                product.InventoryRecords.Any(inventory =>
                    inventory.StockQuantity >
                    inventory.ReservedQuantity));
        }

        var totalCount =
            await query.CountAsync(cancellationToken);

        query = ApplySorting(
            query,
            normalizedFilter.SortBy,
            normalizedFilter.SortDirection);

        var skip =
            (normalizedFilter.PageNumber - 1) *
            normalizedFilter.PageSize;

        var items = await query
            .Skip(skip)
            .Take(normalizedFilter.PageSize)
            .Include(product => product.Category)
            .Include(product => product.Images)
            .Include(product => product.InventoryRecords)
            .Include(product => product.Variants
                .Where(variant => variant.IsActive))
                .ThenInclude(variant => variant.InventoryRecords)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Product?> GetActiveBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        return await _dbContext.Products
            .AsNoTracking()
            .Where(product =>
                product.IsActive &&
                product.Slug == slug)
            .Include(product => product.Category)
            .Include(product => product.Images)
            .Include(product => product.InventoryRecords)
            .Include(product => product.Variants
                .Where(variant => variant.IsActive))
                .ThenInclude(variant => variant.InventoryRecords)
            .AsSplitQuery()
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<Product> ApplySorting(
        IQueryable<Product> query,
        string sortBy,
        string sortDirection)
    {
        var descending =
            string.Equals(
                sortDirection,
                "desc",
                StringComparison.OrdinalIgnoreCase);

        return sortBy switch
        {
            "name" => descending
                ? query.OrderByDescending(product => product.Name)
                : query.OrderBy(product => product.Name),

            "price" => descending
                ? query.OrderByDescending(product => product.BasePrice)
                    .ThenBy(product => product.Name)
                : query.OrderBy(product => product.BasePrice)
                    .ThenBy(product => product.Name),

            "newest" => descending
                ? query.OrderByDescending(product => product.CreatedAt)
                : query.OrderBy(product => product.CreatedAt),

            _ => descending
                ? query.OrderByDescending(
                        product => product.Category.DisplayOrder)
                    .ThenByDescending(product => product.IsFeatured)
                    .ThenBy(product => product.Name)
                : query.OrderBy(
                        product => product.Category.DisplayOrder)
                    .ThenByDescending(product => product.IsFeatured)
                    .ThenBy(product => product.Name)
        };
    }
}
