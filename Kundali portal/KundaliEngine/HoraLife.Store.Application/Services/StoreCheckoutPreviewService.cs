using HoraLife.Store.Application.Checkout.Dtos;
using HoraLife.Store.Application.Checkout.Models;
using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Domain.Entities;

namespace HoraLife.Store.Application.Services;

public sealed class StoreCheckoutPreviewService : IStoreCheckoutPreviewService
{
    // V1 policy. Move these values to configuration when shipping expands.
    private const decimal FlatShippingCharge = 99m;
    private const decimal FreeShippingThreshold = 1500m;
    private const int EstimatedDeliveryMinDays = 4;
    private const int EstimatedDeliveryMaxDays = 7;
    private const string SupportedCountryCode = "IN";

    private readonly ICartRepository _cartRepository;
    private readonly ICustomerAddressRepository _addressRepository;

    public StoreCheckoutPreviewService(
        ICartRepository cartRepository,
        ICustomerAddressRepository addressRepository)
    {
        _cartRepository = cartRepository;
        _addressRepository = addressRepository;
    }

    public async Task<CheckoutPreviewResult<CheckoutPreviewDto>> CreatePreviewAsync(
        Guid userId,
        CheckoutPreviewRequestModel request,
        CancellationToken cancellationToken = default)
    {
        var cart = await _cartRepository.GetActiveByUserIdAsync(
            userId,
            trackChanges: false,
            cancellationToken);

        if (cart is null)
        {
            return CheckoutPreviewResult<CheckoutPreviewDto>.Failure(
                CheckoutPreviewErrorCode.CartNotFound,
                "No active cart was found for the authenticated user.");
        }

        if (cart.Items.Count == 0)
        {
            return CheckoutPreviewResult<CheckoutPreviewDto>.Failure(
                CheckoutPreviewErrorCode.CartEmpty,
                "The active cart does not contain any items.");
        }

        var shippingAddress = await _addressRepository.GetByIdAsync(
            userId,
            request.ShippingAddressId,
            trackChanges: false,
            cancellationToken);

        if (shippingAddress is null)
        {
            return CheckoutPreviewResult<CheckoutPreviewDto>.Failure(
                CheckoutPreviewErrorCode.ShippingAddressNotFound,
                "The selected Shipping address was not found.");
        }

        CustomerAddress billingAddress;

        if (request.UseShippingAsBilling || !request.BillingAddressId.HasValue)
        {
            billingAddress = shippingAddress;
        }
        else
        {
            var selectedBillingAddress = await _addressRepository.GetByIdAsync(
                userId,
                request.BillingAddressId.Value,
                trackChanges: false,
                cancellationToken);

            if (selectedBillingAddress is null)
            {
                return CheckoutPreviewResult<CheckoutPreviewDto>.Failure(
                    CheckoutPreviewErrorCode.BillingAddressNotFound,
                    "The selected Billing address was not found.");
            }

            billingAddress = selectedBillingAddress;
        }

        if (!IsSupportedCountry(shippingAddress.CountryCode))
        {
            return CheckoutPreviewResult<CheckoutPreviewDto>.Failure(
                CheckoutPreviewErrorCode.UnsupportedDeliveryLocation,
                "HoraLife Store currently supports delivery only within India.");
        }

        var previewItems = cart.Items
            .OrderBy(x => x.CreatedAt)
            .Select(CreateItemPreview)
            .ToList();

        var subtotal = previewItems.Sum(x => x.LineTotal);
        var shippingCharge = subtotal >= FreeShippingThreshold ? 0m : FlatShippingCharge;
        const decimal discountAmount = 0m;
        const decimal taxAmount = 0m;

        var warnings = previewItems
            .SelectMany(item => item.Warnings.Select(warning => $"{item.ProductName}: {warning}"))
            .ToList();

        var requiresConsultation = previewItems.Any(x => x.RequiresConsultation);
        if (requiresConsultation)
        {
            warnings.Add(
                "One or more selected remedies require suitability review or consultation before fulfilment.");
        }

        var preview = new CheckoutPreviewDto(
            cart.Id,
            cart.Currency.Trim(),
            MapAddress(shippingAddress),
            MapAddress(billingAddress),
            shippingAddress.Id == billingAddress.Id,
            previewItems,
            previewItems.Sum(x => x.Quantity),
            subtotal,
            shippingCharge,
            discountAmount,
            taxAmount,
            subtotal + shippingCharge - discountAmount + taxAmount,
            EstimatedDeliveryMinDays,
            EstimatedDeliveryMaxDays,
            requiresConsultation,
            previewItems.All(x => x.IsAvailable),
            warnings.Distinct().ToList(),
            DateTimeOffset.UtcNow);

        return CheckoutPreviewResult<CheckoutPreviewDto>.Success(preview);
    }

    private static CheckoutPreviewItemDto CreateItemPreview(CartItem item)
    {
        var product = item.Product;
        var variant = item.Variant;
        var productActive = product.IsActive;
        var variantActive = variant is null || variant.IsActive;
        var currentUnitPrice = variant?.Price ?? product.BasePrice;
        var availableQuantity = CalculateAvailableQuantity(product, variant);
        var warnings = new List<string>();

        if (!productActive) warnings.Add("Product is no longer active.");
        if (!variantActive) warnings.Add("Selected variant is no longer active.");
        if (item.Quantity > availableQuantity)
            warnings.Add($"Only {availableQuantity} item(s) are currently available.");

        var priceChanged = item.UnitPrice != currentUnitPrice;
        if (priceChanged)
            warnings.Add($"Price changed from {item.UnitPrice:0.00} to {currentUnitPrice:0.00}.");

        var selectedImage = product.Images
            .Where(image => image.VariantId == item.VariantId || image.VariantId == null)
            .OrderByDescending(image => image.VariantId == item.VariantId)
            .ThenByDescending(image => image.IsPrimary)
            .ThenBy(image => image.DisplayOrder)
            .FirstOrDefault();

        return new CheckoutPreviewItemDto(
            item.Id,
            item.ProductId,
            item.VariantId,
            product.Slug,
            product.Name,
            variant?.Name,
            variant?.Sku ?? product.Sku,
            selectedImage?.ImageUrl,
            item.UnitPrice,
            currentUnitPrice,
            priceChanged,
            item.Quantity,
            currentUnitPrice * item.Quantity,
            availableQuantity,
            productActive,
            variantActive,
            productActive && variantActive && item.Quantity <= availableQuantity,
            product.RequiresConsultation,
            warnings);
    }

    private static int CalculateAvailableQuantity(Product product, ProductVariant? variant)
    {
        var records = variant is null
            ? product.InventoryRecords.Where(x => x.VariantId == null)
            : variant.InventoryRecords;

        return records.Sum(x => Math.Max(0, x.StockQuantity - x.ReservedQuantity));
    }

    private static CheckoutAddressDto MapAddress(CustomerAddress address)
        => new(
            address.Id,
            address.AddressType.ToString().ToLowerInvariant(),
            address.FullName,
            address.Phone,
            address.AddressLine1,
            address.AddressLine2,
            address.City,
            address.State,
            address.PostalCode,
            address.CountryCode.Trim());

    private static bool IsSupportedCountry(string countryCode)
        => string.Equals(
            countryCode.Trim(),
            SupportedCountryCode,
            StringComparison.OrdinalIgnoreCase);
}
