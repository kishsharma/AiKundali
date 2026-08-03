using HoraLife.Store.Domain.Enums;

namespace HoraLife.Store.Application.Products.Models;

public sealed record ProductSearchFilter
{
    private const int DefaultPageSize = 12;
    private const int MaximumPageSize = 100;

    public string? CategorySlug { get; init; }

    public string? Search { get; init; }

    public ProductType? ProductType { get; init; }

    public bool? IsFeatured { get; init; }

    public bool? InStockOnly { get; init; }

    public decimal? MinimumPrice { get; init; }

    public decimal? MaximumPrice { get; init; }

    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = DefaultPageSize;

    public string SortBy { get; init; } = "display_order";

    public string SortDirection { get; init; } = "asc";

    public ProductSearchFilter Normalize()
    {
        var normalizedPageNumber = PageNumber < 1 ? 1 : PageNumber;
        var normalizedPageSize = PageSize switch
        {
            < 1 => DefaultPageSize,
            > MaximumPageSize => MaximumPageSize,
            _ => PageSize
        };

        var normalizedDirection =
            string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                ? "desc"
                : "asc";

        var normalizedSort = SortBy?.Trim().ToLowerInvariant() switch
        {
            "name" => "name",
            "price" => "price",
            "newest" => "newest",
            _ => "display_order"
        };

        return this with
        {
            CategorySlug = NormalizeText(CategorySlug),
            Search = NormalizeText(Search),
            PageNumber = normalizedPageNumber,
            PageSize = normalizedPageSize,
            SortBy = normalizedSort,
            SortDirection = normalizedDirection
        };
    }

    private static string? NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
}
