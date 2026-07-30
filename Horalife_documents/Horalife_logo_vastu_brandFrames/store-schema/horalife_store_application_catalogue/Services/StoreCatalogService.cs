using System.Text.Json;
using HoraLife.Store.Application.Categories.Dtos;
using HoraLife.Store.Application.Common;
using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Application.Products.Dtos;
using HoraLife.Store.Application.Products.Models;
using HoraLife.Store.Domain.Entities;

namespace HoraLife.Store.Application.Services;

public sealed class StoreCatalogService : IStoreCatalogService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;

    public StoreCatalogService(
        ICategoryRepository categoryRepository,
        IProductRepository productRepository)
    {
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        var categories =
            await _categoryRepository.GetActiveAsync(cancellationToken);

        return categories
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Select(x => new CategoryDto(
                x.Id,
                x.ParentId,
                x.Name,
                x.Slug,
                x.Description,
                x.DisplayOrder,
                x.Products.Count(product => product.IsActive)))
            .ToList();
    }

    public async Task<PagedResult<ProductListItemDto>> GetProductsAsync(
        ProductSearchFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var normalizedFilter = filter.Normalize();

        var (products, totalCount) =
            await _productRepository.SearchAsync(
                normalizedFilter,
                cancellationToken);

        var items = products
            .Select(MapListItem)
            .ToList();

        return new PagedResult<ProductListItemDto>(
            items,
            normalizedFilter.PageNumber,
            normalizedFilter.PageSize,
            totalCount);
    }

    public async Task<ProductDetailDto?> GetProductBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        var product =
            await _productRepository.GetActiveBySlugAsync(
                slug.Trim(),
                cancellationToken);

        return product is null
            ? null
            : MapDetail(product);
    }

    private static ProductListItemDto MapListItem(Product product)
    {
        var activeVariants = product.Variants
            .Where(x => x.IsActive)
            .ToList();

        var displayPrice = activeVariants.Count > 0
            ? activeVariants.Min(x => x.Price)
            : product.BasePrice;

        var compareAtPrice = activeVariants
            .Where(x => x.CompareAtPrice.HasValue)
            .Select(x => x.CompareAtPrice)
            .OrderBy(x => x)
            .FirstOrDefault()
            ?? product.CompareAtPrice;

        var primaryImage = product.Images
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.DisplayOrder)
            .FirstOrDefault();

        var availableQuantity = CalculateAvailableQuantity(product);

        return new ProductListItemDto(
            product.Id,
            product.Sku,
            product.Slug,
            product.Name,
            product.ShortDescription,
            ToSnakeCase(product.ProductType.ToString()),
            displayPrice,
            compareAtPrice,
            product.Currency.Trim(),
            primaryImage?.ImageUrl,
            primaryImage?.AltText,
            product.RequiresConsultation,
            product.IsFeatured,
            availableQuantity > 0,
            availableQuantity);
    }

    private static ProductDetailDto MapDetail(Product product)
    {
        var availableQuantity = CalculateAvailableQuantity(product);

        var variants = product.Variants
            .OrderBy(x => x.Price)
            .ThenBy(x => x.Name)
            .Select(variant => new ProductVariantDto(
                variant.Id,
                variant.Sku,
                variant.Name,
                variant.Price,
                variant.CompareAtPrice,
                variant.WeightGrams,
                CloneJsonElement(variant.Attributes),
                CalculateAvailableQuantity(variant),
                variant.IsActive))
            .ToList();

        var images = product.Images
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.DisplayOrder)
            .Select(image => new ProductImageDto(
                image.Id,
                image.VariantId,
                image.ImageUrl,
                image.AltText,
                image.DisplayOrder,
                image.IsPrimary))
            .ToList();

        return new ProductDetailDto(
            product.Id,
            product.CategoryId,
            product.Category.Name,
            product.Category.Slug,
            product.Sku,
            product.Slug,
            product.Name,
            product.ShortDescription,
            product.Description,
            ToSnakeCase(product.ProductType.ToString()),
            product.BasePrice,
            product.CompareAtPrice,
            product.Currency.Trim(),
            CloneJsonElement(product.Attributes),
            product.RequiresConsultation,
            product.IsFeatured,
            availableQuantity > 0,
            availableQuantity,
            variants,
            images);
    }

    private static int CalculateAvailableQuantity(Product product)
        => product.InventoryRecords.Sum(
            x => Math.Max(0, x.StockQuantity - x.ReservedQuantity));

    private static int CalculateAvailableQuantity(ProductVariant variant)
        => variant.InventoryRecords.Sum(
            x => Math.Max(0, x.StockQuantity - x.ReservedQuantity));

    private static JsonElement CloneJsonElement(JsonDocument? document)
        => document is null
            ? JsonDocument.Parse("{}").RootElement.Clone()
            : document.RootElement.Clone();

    private static string ToSnakeCase(string value)
    {
        var result = new List<char>(value.Length + 8);

        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];

            if (char.IsUpper(current) &&
                index > 0 &&
                (char.IsLower(value[index - 1]) ||
                 char.IsDigit(value[index - 1])))
            {
                result.Add('_');
            }

            result.Add(char.ToLowerInvariant(current));
        }

        return new string(result.ToArray());
    }
}
