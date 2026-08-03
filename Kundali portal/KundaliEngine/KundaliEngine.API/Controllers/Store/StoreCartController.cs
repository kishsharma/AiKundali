using System.Security.Claims;
using HoraLife.Store.Application.Carts.Dtos;
using HoraLife.Store.Application.Carts.Models;
using HoraLife.Store.Application.Interfaces;
using KundaliEngine.API.DTOs.Store.Cart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.Api.Controllers.Store;

[ApiController]
[Authorize]
[Route("api/store/cart")]
public sealed class StoreCartController : ControllerBase
{
    private readonly IStoreCartService _cartService;

    public StoreCartController(IStoreCartService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CartDto>> GetCart(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return InvalidUserClaim();
        }

        var cart = await _cartService.GetCartAsync(
            userId,
            cancellationToken);

        return Ok(cart);
    }

    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CartDto>> AddItem(
        [FromBody] AddCartItemRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return InvalidUserClaim();
        }

        var result = await _cartService.AddItemAsync(
            userId,
            request.ProductId,
            request.VariantId,
            request.Quantity,
            cancellationToken);

        return ToCartActionResult(result);
    }

    [HttpPut("items/{cartItemId:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CartDto>> UpdateItemQuantity(
        Guid cartItemId,
        [FromBody] UpdateCartItemRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return InvalidUserClaim();
        }

        var result = await _cartService.UpdateItemQuantityAsync(
            userId,
            cartItemId,
            request.Quantity,
            cancellationToken);

        return ToCartActionResult(result);
    }

    [HttpDelete("items/{cartItemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveItem(
        Guid cartItemId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return InvalidUserClaim();
        }

        var result = await _cartService.RemoveItemAsync(
            userId,
            cartItemId,
            cancellationToken);

        if (result.Succeeded)
        {
            return NoContent();
        }

        return ToProblemResult(
            result.ErrorCode,
            result.ErrorMessage);
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ClearCart(
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return InvalidUserClaim();
        }

        await _cartService.ClearCartAsync(
            userId,
            cancellationToken);

        return NoContent();
    }

    private ActionResult<CartDto> ToCartActionResult(
        CartOperationResult<CartDto> result)
    {
        return result.Succeeded
            ? Ok(result.Value)
            : ToProblemResult(
                result.ErrorCode,
                result.ErrorMessage);
    }

    private ObjectResult ToProblemResult(
        CartErrorCode errorCode,
        string? detail)
    {
        var status = errorCode switch
        {
            CartErrorCode.ProductNotFound or
            CartErrorCode.VariantNotFound or
            CartErrorCode.CartItemNotFound
                => StatusCodes.Status404NotFound,

            CartErrorCode.InsufficientStock
                => StatusCodes.Status409Conflict,

            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(
            status,
            new ProblemDetails
            {
                Title = GetErrorTitle(errorCode),
                Detail = detail,
                Status = status
            });
    }

    private static string GetErrorTitle(CartErrorCode errorCode)
    {
        return errorCode switch
        {
            CartErrorCode.ProductNotFound => "Product not found",
            CartErrorCode.VariantRequired => "Product variant required",
            CartErrorCode.VariantNotFound => "Product variant not found",
            CartErrorCode.InsufficientStock => "Insufficient stock",
            CartErrorCode.CartItemNotFound => "Cart item not found",
            CartErrorCode.CurrencyMismatch => "Cart currency mismatch",
            _ => "Invalid cart request"
        };
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claimValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue("userId") ??
            User.FindFirstValue("user_id");

        return Guid.TryParse(claimValue, out userId);
    }

    private UnauthorizedObjectResult InvalidUserClaim()
    {
        return Unauthorized(new ProblemDetails
        {
            Title = "Invalid authenticated user",
            Detail =
                "The access token does not contain a valid GUID user identifier.",
            Status = StatusCodes.Status401Unauthorized
        });
    }
}
