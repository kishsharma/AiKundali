using System.Text.Json;
using HoraLife.Store.Application.Checkout.Models;
using HoraLife.Store.Application.Checkout.Revalidation.Models;
using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Application.Orders.Dtos;
using HoraLife.Store.Application.Orders.Models;
using HoraLife.Store.Domain.Entities;
using HoraLife.Store.Domain.Enums;

namespace HoraLife.Store.Application.Services;

public sealed class StoreOrderService : IStoreOrderService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IStoreCheckoutRevalidationService
        _revalidationService;

    private readonly IStoreCheckoutPreviewService
        _checkoutPreviewService;

    private readonly IOrderRepository _orderRepository;

    public StoreOrderService(
        IStoreCheckoutRevalidationService revalidationService,
        IStoreCheckoutPreviewService checkoutPreviewService,
        IOrderRepository orderRepository)
    {
        _revalidationService = revalidationService;
        _checkoutPreviewService = checkoutPreviewService;
        _orderRepository = orderRepository;
    }

    public async Task<CreateOrderOperationResult> CreateAsync(
        Guid userId,
        string customerEmail,
        CreateOrderRequestModel request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerEmail))
        {
            return CreateOrderOperationResult.Failure(
                CreateOrderErrorCode.InvalidCustomerEmail,
                "The authenticated user does not contain a usable email or login identifier.");
        }

        // Step 3 is always repeated by the server. Angular/Postman
        // cannot bypass authoritative cart validation.
        var revalidation =
            await _revalidationService.RevalidateAsync(
                userId,
                cancellationToken);

        if (!revalidation.Succeeded)
        {
            return revalidation.ErrorCode switch
            {
                CheckoutRevalidationErrorCode.CartNotFound =>
                    CreateOrderOperationResult.Failure(
                        CreateOrderErrorCode.CartNotFound,
                        revalidation.ErrorMessage ??
                        "No active cart was found."),

                CheckoutRevalidationErrorCode.CartEmpty =>
                    CreateOrderOperationResult.Failure(
                        CreateOrderErrorCode.CartEmpty,
                        revalidation.ErrorMessage ??
                        "The active cart is empty."),

                _ => CreateOrderOperationResult.Failure(
                    CreateOrderErrorCode.CartValidationFailed,
                    revalidation.ErrorMessage ??
                    "The active cart could not be validated.")
            };
        }

        if (!revalidation.Value!.IsValid)
        {
            return CreateOrderOperationResult.Failure(
                CreateOrderErrorCode.CartValidationFailed,
                "The cart changed and must be corrected or previewed again before creating an order.",
                revalidation.Value);
        }

        var cartId = revalidation.Value.CartId;

        // A cart represents one checkout attempt and must not produce
        // multiple order headers.
        var existingOrder =
            await _orderRepository.GetByCartIdAsync(
                userId,
                cartId,
                trackChanges: false,
                cancellationToken);

        if (existingOrder is not null)
        {
            return CreateOrderOperationResult.Failure(
                CreateOrderErrorCode.OrderAlreadyExists,
                $"Order {existingOrder.OrderNumber} already exists for this cart.");
        }

        // Reuse Step 2 so totals and shipping policy have one source.
        var preview =
            await _checkoutPreviewService.CreatePreviewAsync(
                userId,
                new CheckoutPreviewRequestModel(
                    request.ShippingAddressId,
                    request.BillingAddressId,
                    request.UseShippingAsBilling),
                cancellationToken);

        if (!preview.Succeeded)
        {
            return MapPreviewFailure(
                preview.ErrorCode,
                preview.ErrorMessage);
        }

        if (!preview.Value!.CanCheckout)
        {
            return CreateOrderOperationResult.Failure(
                CreateOrderErrorCode.CheckoutPreviewBlocked,
                "Checkout Preview contains unavailable items and cannot be converted into an order.");
        }

        var now = DateTimeOffset.UtcNow;

        var shippingSnapshot =
            MapAddress(preview.Value.ShippingAddress);

        var billingSnapshot =
            MapAddress(preview.Value.BillingAddress);

        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CartId = cartId,
            CustomerEmail = customerEmail.Trim(),
            CustomerPhone =
                preview.Value.ShippingAddress.Phone,
            OrderStatus = OrderStatus.Pending,
            PaymentStatus = OrderPaymentStatus.Pending,
            SubtotalAmount = preview.Value.Subtotal,
            DiscountAmount = preview.Value.DiscountAmount,
            ShippingAmount = preview.Value.ShippingCharge,
            TaxAmount = preview.Value.TaxAmount,
            TotalAmount = preview.Value.GrandTotal,
            Currency = preview.Value.Currency.Trim(),
            ShippingAddress =
                JsonSerializer.SerializeToDocument(
                    shippingSnapshot,
                    JsonOptions),
            BillingAddress =
                JsonSerializer.SerializeToDocument(
                    billingSnapshot,
                    JsonOptions),
            OrderSource = OrderSource.DirectStore,
            Notes = NormalizeNotes(request.Notes),
            PlacedAt = now,
            UpdatedAt = now
        };

        await _orderRepository.AddAsync(
            order,
            cancellationToken);

        await _orderRepository.SaveChangesAsync(
            cancellationToken);

        var result = new CreatedOrderDto(
            order.Id,
            order.OrderNumber,
            cartId,
            userId,
            order.CustomerEmail,
            order.CustomerPhone,
            ToApiValue(order.OrderStatus),
            ToApiValue(order.PaymentStatus),
            order.SubtotalAmount,
            order.DiscountAmount,
            order.ShippingAmount,
            order.TaxAmount,
            order.TotalAmount,
            order.Currency.Trim(),
            shippingSnapshot,
            billingSnapshot,
            preview.Value.BillingSameAsShipping,
            ToApiValue(order.OrderSource),
            order.Notes,
            OrderItemsCreated: false,
            order.PlacedAt);

        return CreateOrderOperationResult.Success(result);
    }

    private static CreateOrderOperationResult MapPreviewFailure(
        CheckoutPreviewErrorCode errorCode,
        string? message)
    {
        var mappedError = errorCode switch
        {
            CheckoutPreviewErrorCode.CartNotFound =>
                CreateOrderErrorCode.CartNotFound,

            CheckoutPreviewErrorCode.CartEmpty =>
                CreateOrderErrorCode.CartEmpty,

            CheckoutPreviewErrorCode.ShippingAddressNotFound =>
                CreateOrderErrorCode.ShippingAddressNotFound,

            CheckoutPreviewErrorCode.BillingAddressNotFound =>
                CreateOrderErrorCode.BillingAddressNotFound,

            CheckoutPreviewErrorCode.UnsupportedDeliveryLocation =>
                CreateOrderErrorCode.UnsupportedDeliveryLocation,

            _ => CreateOrderErrorCode.CheckoutPreviewBlocked
        };

        return CreateOrderOperationResult.Failure(
            mappedError,
            message ?? "Checkout Preview could not be generated.");
    }

    private static OrderAddressSnapshotDto MapAddress(
        HoraLife.Store.Application.Checkout.Dtos.CheckoutAddressDto address)
    {
        return new OrderAddressSnapshotDto(
            address.Id,
            address.AddressType,
            address.FullName,
            address.Phone,
            address.AddressLine1,
            address.AddressLine2,
            address.City,
            address.State,
            address.PostalCode,
            address.CountryCode.Trim());
    }

    private static string? NormalizeNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var normalized = notes.Trim();

        return normalized.Length <= 1000
            ? normalized
            : normalized[..1000];
    }

    private static string ToApiValue<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        return string.Concat(
            value.ToString()
                .Select((character, index) =>
                    index > 0 &&
                    char.IsUpper(character)
                        ? $"_{char.ToLowerInvariant(character)}"
                        : char.ToLowerInvariant(character).ToString()));
    }
}
