using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Application.Orders.Items.Dtos;
using HoraLife.Store.Application.Orders.Items.Models;
using HoraLife.Store.Domain.Entities;
using HoraLife.Store.Domain.Enums;

namespace HoraLife.Store.Application.Services;

public sealed class StoreOrderItemService : IStoreOrderItemService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderItemRepository _orderItemRepository;
    private readonly ICartRepository _cartRepository;

    public StoreOrderItemService(
        IOrderRepository orderRepository,
        IOrderItemRepository orderItemRepository,
        ICartRepository cartRepository)
    {
        _orderRepository = orderRepository;
        _orderItemRepository = orderItemRepository;
        _cartRepository = cartRepository;
    }

    public async Task<CreateOrderItemsOperationResult> CreateAsync(
        Guid userId,
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(
            userId, orderId, trackChanges: false, cancellationToken);

        if (order is null)
            return CreateOrderItemsOperationResult.Failure(
                CreateOrderItemsErrorCode.OrderNotFound,
                "The requested order was not found.");

        if (order.OrderStatus != OrderStatus.Pending)
            return CreateOrderItemsOperationResult.Failure(
                CreateOrderItemsErrorCode.OrderNotPending,
                "Order items can be created only while the order is pending.");

        if (!order.CartId.HasValue)
            return CreateOrderItemsOperationResult.Failure(
                CreateOrderItemsErrorCode.OrderCartMissing,
                "The order is not linked to a source cart.");

        if (await _orderItemRepository.AnyForOrderAsync(order.Id, cancellationToken))
            return CreateOrderItemsOperationResult.Failure(
                CreateOrderItemsErrorCode.OrderItemsAlreadyExist,
                $"Order items already exist for order {order.OrderNumber}.");

        var cart = await _cartRepository.GetActiveByUserIdAsync(
            userId, trackChanges: false, cancellationToken);

        if (cart is null)
            return CreateOrderItemsOperationResult.Failure(
                CreateOrderItemsErrorCode.ActiveCartNotFound,
                "The active source cart was not found.");

        if (cart.Id != order.CartId.Value)
            return CreateOrderItemsOperationResult.Failure(
                CreateOrderItemsErrorCode.CartMismatch,
                "The active cart does not match the cart linked to this order.");

        if (cart.Items.Count == 0)
            return CreateOrderItemsOperationResult.Failure(
                CreateOrderItemsErrorCode.CartEmpty,
                "The source cart does not contain any items.");

        var sourceSubtotal = cart.Items.Sum(x => x.UnitPrice * x.Quantity);
        if (sourceSubtotal != order.SubtotalAmount)
            return CreateOrderItemsOperationResult.Failure(
                CreateOrderItemsErrorCode.CartChanged,
                $"Cart subtotal {sourceSubtotal:0.00} no longer matches order subtotal {order.SubtotalAmount:0.00}.");

        var now = DateTimeOffset.UtcNow;
        var orderItems = cart.Items
            .OrderBy(x => x.CreatedAt)
            .Select(x => new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = x.ProductId,
                VariantId = x.VariantId,
                ProductName = x.Product.Name,
                VariantName = x.Variant?.Name,
                Sku = x.Variant?.Sku ?? x.Product.Sku,
                UnitPrice = x.UnitPrice,
                Quantity = x.Quantity,
                LineTotal = x.UnitPrice * x.Quantity,
                RequiresConsultation = x.Product.RequiresConsultation,
                CreatedAt = now,
                UpdatedAt = now
            })
            .ToList();

        await _orderItemRepository.AddRangeAsync(orderItems, cancellationToken);
        await _orderItemRepository.SaveChangesAsync(cancellationToken);

        var itemsSubtotal = orderItems.Sum(x => x.LineTotal);
        var dto = new CreatedOrderItemsDto(
            order.Id,
            order.OrderNumber,
            cart.Id,
            order.Currency.Trim(),
            orderItems.Sum(x => x.Quantity),
            order.SubtotalAmount,
            itemsSubtotal,
            order.SubtotalAmount == itemsSubtotal,
            true,
            orderItems.Select(x => new CreatedOrderItemDto(
                x.Id, x.ProductId, x.VariantId, x.ProductName,
                x.VariantName, x.Sku, x.UnitPrice, x.Quantity,
                x.LineTotal, x.RequiresConsultation)).ToList(),
            now);

        return CreateOrderItemsOperationResult.Success(dto);
    }
}
