using HoraLife.Store.Application.Carts.Dtos;
using HoraLife.Store.Application.Carts.Models;
using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Domain.Entities;
using HoraLife.Store.Domain.Enums;

namespace HoraLife.Store.Application.Services;

public sealed class StoreCartService : IStoreCartService
{
    private const int MaximumItemQuantity = 99;

    private readonly ICartRepository _cartRepository;

    public StoreCartService(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<CartDto> GetCartAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var cart = await _cartRepository.GetActiveByUserIdAsync(
            userId,
            trackChanges: false,
            cancellationToken);

        return cart is null
            ? CartDto.Empty()
            : MapCart(cart);
    }

    public async Task<CartOperationResult<CartDto>> AddItemAsync(
        Guid userId,
        Guid productId,
        Guid? variantId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        var quantityValidation = ValidateQuantity(quantity);
        if (quantityValidation is not null)
        {
            return CartOperationResult<CartDto>.Failure(
                CartErrorCode.InvalidQuantity,
                quantityValidation);
        }

        var product = await _cartRepository.GetActiveProductForCartAsync(
            productId,
            cancellationToken);

        if (product is null)
        {
            return CartOperationResult<CartDto>.Failure(
                CartErrorCode.ProductNotFound,
                "The requested product was not found or is inactive.");
        }

        var variantResult = ResolveVariant(product, variantId);
        if (!variantResult.Succeeded)
        {
            return CartOperationResult<CartDto>.Failure(
                variantResult.ErrorCode,
                variantResult.ErrorMessage!);
        }

        var variant = variantResult.Value;
        var availableQuantity = CalculateAvailableQuantity(
            product,
            variant);

        var cart = await _cartRepository.GetActiveByUserIdAsync(
            userId,
            trackChanges: true,
            cancellationToken);

        var existingItem = cart?.Items.SingleOrDefault(item =>
            item.ProductId == product.Id &&
            item.VariantId == variant?.Id);

        var requestedTotalQuantity =
            (existingItem?.Quantity ?? 0) + quantity;

        if (requestedTotalQuantity > availableQuantity)
        {
            return CartOperationResult<CartDto>.Failure(
                CartErrorCode.InsufficientStock,
                $"Only {availableQuantity} item(s) are currently available.");
        }

        var currency = product.Currency.Trim();

        if (cart is not null &&
            !string.Equals(
                cart.Currency.Trim(),
                currency,
                StringComparison.OrdinalIgnoreCase))
        {
            return CartOperationResult<CartDto>.Failure(
                CartErrorCode.CurrencyMismatch,
                "This product uses a different currency from the active cart.");
        }

        var unitPrice = variant?.Price ?? product.BasePrice;

        if (cart is null)
        {
            cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = CartStatus.Active,
                Currency = currency
            };

            cart.Items.Add(new CartItem
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                VariantId = variant?.Id,
                Quantity = quantity,
                UnitPrice = unitPrice
            });

            await _cartRepository.AddCartAsync(
                cart,
                cancellationToken);
        }
        else if (existingItem is null)
        {
            cart.Items.Add(new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductId = product.Id,
                VariantId = variant?.Id,
                Quantity = quantity,
                UnitPrice = unitPrice
            });
        }
        else
        {
            existingItem.Quantity = requestedTotalQuantity;
            existingItem.UnitPrice = unitPrice;
        }

        await _cartRepository.SaveChangesAsync(cancellationToken);

        var refreshedCart = await _cartRepository.GetActiveByUserIdAsync(
            userId,
            trackChanges: false,
            cancellationToken);

        return CartOperationResult<CartDto>.Success(
            MapCart(refreshedCart!));
    }

    public async Task<CartOperationResult<CartDto>> UpdateItemQuantityAsync(
        Guid userId,
        Guid cartItemId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        var quantityValidation = ValidateQuantity(quantity);
        if (quantityValidation is not null)
        {
            return CartOperationResult<CartDto>.Failure(
                CartErrorCode.InvalidQuantity,
                quantityValidation);
        }

        var cart = await _cartRepository.GetActiveByUserIdAsync(
            userId,
            trackChanges: true,
            cancellationToken);

        var item = cart?.Items.SingleOrDefault(x => x.Id == cartItemId);

        if (item is null)
        {
            return CartOperationResult<CartDto>.Failure(
                CartErrorCode.CartItemNotFound,
                "The requested cart item was not found.");
        }

        var availableQuantity = CalculateAvailableQuantity(
            item.Product,
            item.Variant);

        if (quantity > availableQuantity)
        {
            return CartOperationResult<CartDto>.Failure(
                CartErrorCode.InsufficientStock,
                $"Only {availableQuantity} item(s) are currently available.");
        }

        item.Quantity = quantity;
        item.UnitPrice = item.Variant?.Price ?? item.Product.BasePrice;

        await _cartRepository.SaveChangesAsync(cancellationToken);

        var refreshedCart = await _cartRepository.GetActiveByUserIdAsync(
            userId,
            trackChanges: false,
            cancellationToken);

        return CartOperationResult<CartDto>.Success(
            MapCart(refreshedCart!));
    }

    public async Task<CartOperationResult<bool>> RemoveItemAsync(
        Guid userId,
        Guid cartItemId,
        CancellationToken cancellationToken = default)
    {
        var cart = await _cartRepository.GetActiveByUserIdAsync(
            userId,
            trackChanges: true,
            cancellationToken);

        var item = cart?.Items.SingleOrDefault(x => x.Id == cartItemId);

        if (item is null)
        {
            return CartOperationResult<bool>.Failure(
                CartErrorCode.CartItemNotFound,
                "The requested cart item was not found.");
        }

        _cartRepository.RemoveItem(item);
        await _cartRepository.SaveChangesAsync(cancellationToken);

        return CartOperationResult<bool>.Success(true);
    }

    public async Task<CartOperationResult<bool>> ClearCartAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var cart = await _cartRepository.GetActiveByUserIdAsync(
            userId,
            trackChanges: true,
            cancellationToken);

        if (cart is null || cart.Items.Count == 0)
        {
            return CartOperationResult<bool>.Success(true);
        }

        _cartRepository.RemoveItems(cart.Items.ToList());
        await _cartRepository.SaveChangesAsync(cancellationToken);

        return CartOperationResult<bool>.Success(true);
    }

    private static CartOperationResult<ProductVariant?> ResolveVariant(
        Product product,
        Guid? variantId)
    {
        var activeVariants = product.Variants
            .Where(x => x.IsActive)
            .ToList();

        if (activeVariants.Count > 0 && !variantId.HasValue)
        {
            return CartOperationResult<ProductVariant?>.Failure(
                CartErrorCode.VariantRequired,
                "A product variant must be selected.");
        }

        if (!variantId.HasValue)
        {
            return CartOperationResult<ProductVariant?>.Success(null);
        }

        var variant = activeVariants.SingleOrDefault(
            x => x.Id == variantId.Value);

        return variant is null
            ? CartOperationResult<ProductVariant?>.Failure(
                CartErrorCode.VariantNotFound,
                "The requested product variant was not found or is inactive.")
            : CartOperationResult<ProductVariant?>.Success(variant);
    }

    private static int CalculateAvailableQuantity(
        Product product,
        ProductVariant? variant)
    {
        var records = variant is null
            ? product.InventoryRecords.Where(x => x.VariantId == null)
            : variant.InventoryRecords;

        return records.Sum(
            x => Math.Max(
                0,
                x.StockQuantity - x.ReservedQuantity));
    }

    private static CartDto MapCart(Cart cart)
    {
        var items = cart.Items
            .OrderBy(x => x.CreatedAt)
            .Select(MapItem)
            .ToList();

        return new CartDto(
            cart.Id,
            cart.Currency.Trim(),
            items.Sum(x => x.Quantity),
            items.Sum(x => x.LineTotal),
            items);
    }

    private static CartItemDto MapItem(CartItem item)
    {
        var selectedImage = item.Product.Images
            .Where(image =>
                image.VariantId == item.VariantId ||
                image.VariantId == null)
            .OrderByDescending(image =>
                image.VariantId == item.VariantId)
            .ThenByDescending(image => image.IsPrimary)
            .ThenBy(image => image.DisplayOrder)
            .FirstOrDefault();

        var availableQuantity = CalculateAvailableQuantity(
            item.Product,
            item.Variant);

        return new CartItemDto(
            item.Id,
            item.ProductId,
            item.VariantId,
            item.Product.Slug,
            item.Product.Name,
            item.Variant?.Name,
            item.Variant?.Sku ?? item.Product.Sku,
            selectedImage?.ImageUrl,
            selectedImage?.AltText,
            item.UnitPrice,
            item.Quantity,
            item.UnitPrice * item.Quantity,
            item.Product.Currency.Trim(),
            availableQuantity,
            item.Product.RequiresConsultation);
    }

    private static string? ValidateQuantity(int quantity)
    {
        if (quantity < 1)
        {
            return "Quantity must be at least 1. Use the remove endpoint to delete an item.";
        }

        return quantity > MaximumItemQuantity
            ? $"Quantity cannot exceed {MaximumItemQuantity}."
            : null;
    }
}
