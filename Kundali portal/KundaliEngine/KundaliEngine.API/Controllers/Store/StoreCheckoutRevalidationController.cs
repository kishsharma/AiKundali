using System.Security.Claims;
using HoraLife.Store.Application.Checkout.Revalidation.Dtos;
using HoraLife.Store.Application.Checkout.Revalidation.Models;
using HoraLife.Store.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.Api.Controllers.Store;

[ApiController]
[Authorize]
[Route("api/store/checkout")]
public sealed class StoreCheckoutRevalidationController :
    ControllerBase
{
    private readonly IStoreCheckoutRevalidationService
        _revalidationService;

    public StoreCheckoutRevalidationController(
        IStoreCheckoutRevalidationService revalidationService)
    {
        _revalidationService = revalidationService;
    }

    [HttpPost("revalidate")]
    [ProducesResponseType(
        typeof(CheckoutRevalidationDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(CheckoutRevalidationDto),
        StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CheckoutRevalidationDto>>
        Revalidate(
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return InvalidUserClaim();
        }

        var result =
            await _revalidationService.RevalidateAsync(
                userId,
                cancellationToken);

        if (!result.Succeeded)
        {
            return ToProblemResult(
                result.ErrorCode,
                result.ErrorMessage);
        }

        if (result.Value!.IsValid)
        {
            return Ok(result.Value);
        }

        return Conflict(result.Value);
    }

    private ObjectResult ToProblemResult(
        CheckoutRevalidationErrorCode errorCode,
        string? detail)
    {
        var status = errorCode switch
        {
            CheckoutRevalidationErrorCode.CartNotFound
                => StatusCodes.Status404NotFound,

            CheckoutRevalidationErrorCode.CartEmpty
                => StatusCodes.Status409Conflict,

            _ => StatusCodes.Status400BadRequest
        };

        var title = errorCode switch
        {
            CheckoutRevalidationErrorCode.CartNotFound
                => "Active cart not found",

            CheckoutRevalidationErrorCode.CartEmpty
                => "Cart is empty",

            _ => "Checkout revalidation failed"
        };

        return StatusCode(
            status,
            new ProblemDetails
            {
                Title = title,
                Detail = detail,
                Status = status
            });
    }

    private bool TryGetUserId(out Guid userId)
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

    private UnauthorizedObjectResult InvalidUserClaim()
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
}
