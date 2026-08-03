using System.Security.Claims;
using HoraLife.Store.Application.Checkout.Dtos;
using HoraLife.Store.Application.Checkout.Models;
using HoraLife.Store.Application.Interfaces;
using KundaliEngine.Api.DTOs.Store.Checkout;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.Api.Controllers.Store;

[ApiController]
[Authorize]
[Route("api/store/checkout")]
public sealed class StoreCheckoutController : ControllerBase
{
    private readonly IStoreCheckoutPreviewService _checkoutPreviewService;

    public StoreCheckoutController(IStoreCheckoutPreviewService checkoutPreviewService)
    {
        _checkoutPreviewService = checkoutPreviewService;
    }

    [HttpPost("preview")]
    [ProducesResponseType(typeof(CheckoutPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CheckoutPreviewDto>> CreatePreview(
        [FromBody] CreateCheckoutPreviewRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return InvalidUserClaim();

        var result = await _checkoutPreviewService.CreatePreviewAsync(
            userId,
            request.ToModel(),
            cancellationToken);

        return result.Succeeded
            ? Ok(result.Value)
            : ToProblemResult(result.ErrorCode, result.ErrorMessage);
    }

    private ObjectResult ToProblemResult(
        CheckoutPreviewErrorCode errorCode,
        string? detail)
    {
        var status = errorCode switch
        {
            CheckoutPreviewErrorCode.CartNotFound or
            CheckoutPreviewErrorCode.ShippingAddressNotFound or
            CheckoutPreviewErrorCode.BillingAddressNotFound
                => StatusCodes.Status404NotFound,
            CheckoutPreviewErrorCode.CartEmpty
                => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        var title = errorCode switch
        {
            CheckoutPreviewErrorCode.CartNotFound => "Active cart not found",
            CheckoutPreviewErrorCode.CartEmpty => "Cart is empty",
            CheckoutPreviewErrorCode.ShippingAddressNotFound => "Shipping address not found",
            CheckoutPreviewErrorCode.BillingAddressNotFound => "Billing address not found",
            CheckoutPreviewErrorCode.UnsupportedDeliveryLocation => "Unsupported delivery location",
            _ => "Checkout preview failed"
        };

        return StatusCode(status, new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = status
        });
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
        => Unauthorized(new ProblemDetails
        {
            Title = "Invalid authenticated user",
            Detail = "The access token does not contain a valid GUID user identifier.",
            Status = StatusCodes.Status401Unauthorized
        });
}
