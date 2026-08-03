using HoraLife.Store.Application.Carts.Dtos;
using HoraLife.Store.Application.Carts.Models;

namespace HoraLife.Store.Application.Interfaces;

public interface IStoreCartService
{
    Task<CartDto> GetCartAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<CartOperationResult<CartDto>> AddItemAsync(
        Guid userId,
        Guid productId,
        Guid? variantId,
        int quantity,
        CancellationToken cancellationToken = default);

    Task<CartOperationResult<CartDto>> UpdateItemQuantityAsync(
        Guid userId,
        Guid cartItemId,
        int quantity,
        CancellationToken cancellationToken = default);

    Task<CartOperationResult<bool>> RemoveItemAsync(
        Guid userId,
        Guid cartItemId,
        CancellationToken cancellationToken = default);

    Task<CartOperationResult<bool>> ClearCartAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
