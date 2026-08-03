using System.Security.Claims;
using HoraLife.Store.Application.Carts.Conversion.Dtos;
using HoraLife.Store.Application.Carts.Conversion.Models;
using HoraLife.Store.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.Api.Controllers.Store;

[ApiController]
[Authorize]
[Route("api/store/orders/{orderId:guid}/cart")]
public sealed class StoreOrderCartController :
    ControllerBase
{
    private readonly IStoreCartConversionService        _cartConversionService;

    public StoreOrderCartController(IStoreCartConversionService cartConversionService)
    {
        _cartConversionService =     cartConversionService;
    }

    [HttpPost("convert")]
    [ProducesResponseType( typeof(ConvertedCartDto),StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType( typeof(ProblemDetails),    StatusCodes.Status404NotFound)]
    [ProducesResponseType( typeof(ProblemDetails),  StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ConvertedCartDto>> Convert( Guid orderId,CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(
                new ProblemDetails
                {
                    Title = "Invalid authenticated user",
                    Detail =
                        "The access token does not contain a valid GUID user identifier.",
                    Status = StatusCodes.Status401Unauthorized
                });
        }

        var result =
            await _cartConversionService.ConvertAsync(
                userId,
                orderId,
                cancellationToken);

        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        if (result.ErrorCode ==
            CartConversionErrorCode.OrderNotFound)
        {
            return NotFound(
                CreateProblem(
                    "Order not found",
                    result.ErrorMessage,
                    StatusCodes.Status404NotFound));
        }

        if (result.ErrorCode ==
            CartConversionErrorCode.CartNotFound)
        {
            return NotFound(
                CreateProblem(
                    "Source cart not found",
                    result.ErrorMessage,
                    StatusCodes.Status404NotFound));
        }

        return Conflict(
            CreateProblem(
                GetConflictTitle(result.ErrorCode),
                result.ErrorMessage,
                StatusCodes.Status409Conflict));
    }

    private static ProblemDetails CreateProblem(
        string title,
        string? detail,
        int status)
    {
        return new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = status
        };
    }

    private static string GetConflictTitle(
        CartConversionErrorCode errorCode)
    {
        return errorCode switch
        {
            CartConversionErrorCode.OrderNotPending =>
                "Order is not pending",

            CartConversionErrorCode.OrderCartMissing =>
                "Order cart is missing",

            CartConversionErrorCode.OrderItemsMissing =>
                "Order items are missing",

            CartConversionErrorCode.InventoryNotReserved =>
                "Inventory is not reserved",

            CartConversionErrorCode.CartOwnershipMismatch =>
                "Cart ownership mismatch",

            CartConversionErrorCode.CartAlreadyConverted =>
                "Cart is already converted",

            CartConversionErrorCode.CartNotActive =>
                "Cart is not active",

            _ => "Cart conversion failed"
        };
    }

    private bool TryGetUserId(
        out Guid userId)
    {
        var claimValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue("userId") ??
            User.FindFirstValue("user_id");

        return Guid.TryParse(
            claimValue,
            out userId);
    }
}
