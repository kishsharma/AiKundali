using HoraLife.Store.Application.Products.Models;
using HoraLife.Store.Domain.Enums;

namespace HoraLife.Api.Models.Store;

public sealed class ProductSearchRequest
{
    public string? Category { get; init; }
    public string? Search { get; init; }
    public string? ProductType { get; init; }
    public bool? IsFeatured { get; init; }
    public bool? InStockOnly { get; init; }
    public decimal? MinimumPrice { get; init; }
    public decimal? MaximumPrice { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 12;
    public string SortBy { get; init; } = "display_order";
    public string SortDirection { get; init; } = "asc";

    public bool TryToFilter(
        out ProductSearchFilter filter,
        out string? validationError)
    {
        ProductType? parsedProductType = null;

        if (!string.IsNullOrWhiteSpace(ProductType))
        {
            if (!TryParseProductType(ProductType, out var value))
            {
                filter = new ProductSearchFilter();
                validationError =
                    $"Invalid productType '{ProductType}'. " +
                    "Allowed values: gemstone, yantra, rudraksha, " +
                    "puja_item, remedy_kit, other.";
                return false;
            }

            parsedProductType = value;
        }

        if (MinimumPrice.HasValue && MinimumPrice.Value < 0)
        {
            filter = new ProductSearchFilter();
            validationError = "minimumPrice cannot be negative.";
            return false;
        }

        if (MaximumPrice.HasValue && MaximumPrice.Value < 0)
        {
            filter = new ProductSearchFilter();
            validationError = "maximumPrice cannot be negative.";
            return false;
        }

        if (MinimumPrice.HasValue &&
            MaximumPrice.HasValue &&
            MinimumPrice.Value > MaximumPrice.Value)
        {
            filter = new ProductSearchFilter();
            validationError =
                "minimumPrice cannot be greater than maximumPrice.";
            return false;
        }

        filter = new ProductSearchFilter
        {
            CategorySlug = Category,
            Search = Search,
            ProductType = parsedProductType,
            IsFeatured = IsFeatured,
            InStockOnly = InStockOnly,
            MinimumPrice = MinimumPrice,
            MaximumPrice = MaximumPrice,
            PageNumber = PageNumber,
            PageSize = PageSize,
            SortBy = SortBy,
            SortDirection = SortDirection
        };

        validationError = null;
        return true;
    }

    private static bool TryParseProductType(
        string value,
        out ProductType productType)
    {
        var normalized = value
            .Trim()
            .Replace("-", "_", StringComparison.Ordinal)
            .ToLowerInvariant();

        productType = normalized switch
        {
            "gemstone" => HoraLife.Store.Domain.Enums.ProductType.Gemstone,
            "yantra" => HoraLife.Store.Domain.Enums.ProductType.Yantra,
            "rudraksha" => HoraLife.Store.Domain.Enums.ProductType.Rudraksha,
            "puja_item" => HoraLife.Store.Domain.Enums.ProductType.PujaItem,
            "remedy_kit" => HoraLife.Store.Domain.Enums.ProductType.RemedyKit,
            "other" => HoraLife.Store.Domain.Enums.ProductType.Other,
            _ => default
        };

        return normalized is
            "gemstone" or
            "yantra" or
            "rudraksha" or
            "puja_item" or
            "remedy_kit" or
            "other";
    }
}
