using HoraLife.Store.Application.Checkout.Revalidation.Dtos;
using HoraLife.Store.Application.Checkout.Revalidation.Models;
using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Domain.Entities;

namespace HoraLife.Store.Application.Services;

public sealed class StoreCheckoutRevalidationService :
    IStoreCheckoutRevalidationService
{
    private readonly ICartRepository _cartRepository;

    public StoreCheckoutRevalidationService(
        ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<
        CheckoutRevalidationOperationResult<CheckoutRevalidationDto>>
        RevalidateAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        var cart = await _cartRepository.GetActiveByUserIdAsync(
            userId,
            trackChanges: true,
            cancellationToken);

        if (cart is null)
        {
            return CheckoutRevalidationOperationResult<
                CheckoutRevalidationDto>.Failure(
                    CheckoutRevalidationErrorCode.CartNotFound,
                    "No active cart was found for the authenticated user.");
        }

        if (cart.Items.Count == 0)
        {
            return CheckoutRevalidationOperationResult<
                CheckoutRevalidationDto>.Failure(
                    CheckoutRevalidationErrorCode.CartEmpty,
                    "The active cart does not contain any items.");
        }

        var itemResults =
            new List<CheckoutRevalidationItemDto>();

        var aggregateIssues =
            new List<CheckoutRevalidationIssueDto>();

        var previousSubtotal = 0m;
        var currentSubtotal = 0m;
        var cartPricesUpdated = false;

        foreach (var item in cart.Items.OrderBy(x => x.CreatedAt))
        {
            var result = ValidateItem(
                item,
                cart.Currency);

            itemResults.Add(result);

            previousSubtotal +=
                result.PreviousUnitPrice * result.Quantity;

            currentSubtotal +=
                result.CurrentUnitPrice * result.Quantity;

            foreach (var issue in result.Issues)
            {
                aggregateIssues.Add(
                    new CheckoutRevalidationIssueDto(
                        issue.Code,
                        $"{result.ProductName}: {issue.Message}"));
            }

            if (CanSynchronizePrice(result))
            {
                item.UnitPrice = result.CurrentUnitPrice;
                cartPricesUpdated = true;
            }
        }

        if (cartPricesUpdated)
        {
            await _cartRepository.SaveChangesAsync(
                cancellationToken);
        }

        var requiresNewPreview =
            itemResults.Any(x => x.PriceChanged);

        if (requiresNewPreview)
        {
            aggregateIssues.Add(
                new CheckoutRevalidationIssueDto(
                    "CHECKOUT_PREVIEW_REQUIRED",
                    "One or more prices changed. The cart was updated; generate a new Checkout Preview before continuing."));
        }

        var isValid =
            itemResults.All(x => x.IsValid) &&
            !requiresNewPreview;

        var response = new CheckoutRevalidationDto(
            cart.Id,
            cart.Currency.Trim(),
            isValid,
            requiresNewPreview,
            cartPricesUpdated,
            previousSubtotal,
            currentSubtotal,
            itemResults,
            aggregateIssues
                .Distinct()
                .ToList(),
            DateTimeOffset.UtcNow);

        return CheckoutRevalidationOperationResult<
            CheckoutRevalidationDto>.Success(response);
    }

    private static CheckoutRevalidationItemDto ValidateItem(
        CartItem item,
        string cartCurrency)
    {
        var product = item.Product;
        var variant = item.Variant;

        var issues =
            new List<CheckoutRevalidationIssueDto>();

        var productActive = product.IsActive;
        var variantActive =
            variant is null || variant.IsActive;

        var currentUnitPrice =
            variant?.Price ?? product.BasePrice;

        var priceChanged =
            item.UnitPrice != currentUnitPrice;

        var availableQuantity =
            CalculateAvailableQuantity(
                product,
                variant);

        var hasSufficientStock =
            availableQuantity >= item.Quantity;

        var currencyMatches =
            string.Equals(
                cartCurrency.Trim(),
                product.Currency.Trim(),
                StringComparison.OrdinalIgnoreCase);

        if (!productActive)
        {
            issues.Add(
                new CheckoutRevalidationIssueDto(
                    "PRODUCT_INACTIVE",
                    "The product is no longer available."));
        }

        if (!variantActive)
        {
            issues.Add(
                new CheckoutRevalidationIssueDto(
                    "VARIANT_INACTIVE",
                    "The selected variant is no longer available."));
        }

        if (!currencyMatches)
        {
            issues.Add(
                new CheckoutRevalidationIssueDto(
                    "CURRENCY_MISMATCH",
                    $"Cart currency {cartCurrency.Trim()} does not match product currency {product.Currency.Trim()}."));
        }

        if (!hasSufficientStock)
        {
            issues.Add(
                new CheckoutRevalidationIssueDto(
                    "INSUFFICIENT_STOCK",
                    $"Requested quantity is {item.Quantity}, but only {availableQuantity} item(s) are currently available."));
        }

        if (priceChanged)
        {
            issues.Add(
                new CheckoutRevalidationIssueDto(
                    "PRICE_CHANGED",
                    $"Unit price changed from {item.UnitPrice:0.00} to {currentUnitPrice:0.00}."));
        }

        return new CheckoutRevalidationItemDto(
            item.Id,
            item.ProductId,
            item.VariantId,
            product.Name,
            variant?.Name,
            variant?.Sku ?? product.Sku,
            item.Quantity,
            item.UnitPrice,
            currentUnitPrice,
            priceChanged,
            availableQuantity,
            productActive,
            variantActive,
            hasSufficientStock,
            currencyMatches,
            issues.Count == 0,
            issues);
    }

    private static int CalculateAvailableQuantity(
        Product product,
        ProductVariant? variant)
    {
        var records = variant is null
            ? product.InventoryRecords
                .Where(x => x.VariantId == null)
            : variant.InventoryRecords;

        return records.Sum(
            x => Math.Max(
                0,
                x.StockQuantity - x.ReservedQuantity));
    }

    private static bool CanSynchronizePrice(
        CheckoutRevalidationItemDto item)
    {
        return item.PriceChanged &&
               item.ProductActive &&
               item.VariantActive &&
               item.CurrencyMatches;
    }
}
