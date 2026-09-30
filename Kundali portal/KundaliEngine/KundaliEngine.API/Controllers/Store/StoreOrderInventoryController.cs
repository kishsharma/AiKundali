using System.Security.Claims;
using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Application.Inventory.Dtos;
using HoraLife.Store.Application.Inventory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.Api.Controllers.Store;

[ApiController]
[Authorize]
[Route("api/store/orders/{orderId:guid}/inventory")]
public sealed class StoreOrderInventoryController : ControllerBase
{
    private readonly IStoreInventoryReservationService _reservationService;

    public StoreOrderInventoryController(
        IStoreInventoryReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    [HttpPost("reserve")]
    [ProducesResponseType(typeof(InventoryReservationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InventoryReservationDto>> Reserve(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(InvalidUserProblem());
        }

        var result = await _reservationService.ReserveAsync(
            userId,
            orderId,
            cancellationToken);

        if (result.Succeeded)
        {
            return StatusCode(StatusCodes.Status201Created, result.Value);
        }

        return ToErrorResult(result.ErrorCode, result.ErrorMessage);
    }

    [HttpPost("confirm")]
    [ProducesResponseType(typeof(InventoryLifecycleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InventoryLifecycleDto>> Confirm(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(InvalidUserProblem());
        }

        var bypassOwnership = User.IsInRole("Admin");
        var result = await _reservationService.ConfirmAsync(
            orderId,
            userId,
            bypassOwnership,
            cancellationToken);

        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        return ToErrorResult(result.ErrorCode, result.ErrorMessage);
    }

    [HttpPost("release")]
    [ProducesResponseType(typeof(InventoryLifecycleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InventoryLifecycleDto>> Release(
        Guid orderId,
        [FromBody] InventoryReleaseRequestDto? request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized(InvalidUserProblem());
        }

        var bypassOwnership = User.IsInRole("Admin");
        var reason = string.IsNullOrWhiteSpace(request?.Reason)
            ? "cancelled"
            : request.Reason.Trim();

        var result = await _reservationService.ReleaseAsync(
            orderId,
            userId,
            bypassOwnership,
            reason,
            cancellationToken);

        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        return ToErrorResult(result.ErrorCode, result.ErrorMessage);
    }

    private ActionResult ToErrorResult(
        InventoryReservationErrorCode errorCode,
        string? errorMessage)
    {
        if (errorCode == InventoryReservationErrorCode.OrderNotFound)
        {
            return NotFound(
                CreateProblem(
                    "Order not found",
                    errorMessage,
                    StatusCodes.Status404NotFound));
        }

        return Conflict(
            CreateProblem(
                GetConflictTitle(errorCode),
                errorMessage,
                StatusCodes.Status409Conflict));
    }

    private static ProblemDetails InvalidUserProblem()
    {
        return new ProblemDetails
        {
            Title = "Invalid authenticated user",
            Detail =
                "The access token does not contain a valid GUID user identifier.",
            Status = StatusCodes.Status401Unauthorized
        };
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
        InventoryReservationErrorCode errorCode)
    {
        return errorCode switch
        {
            InventoryReservationErrorCode.OrderNotPending =>
                "Order is not pending",
            InventoryReservationErrorCode.OrderItemsMissing =>
                "Order items are missing",
            InventoryReservationErrorCode.OrderItemProductMissing =>
                "Order item product is missing",
            InventoryReservationErrorCode.InventoryRecordMissing =>
                "Inventory record is missing",
            InventoryReservationErrorCode.InsufficientStock =>
                "Insufficient stock",
            InventoryReservationErrorCode.InventoryAlreadyReserved =>
                "Inventory already reserved",
            InventoryReservationErrorCode.InventoryNotReserved =>
                "Inventory not reserved",
            InventoryReservationErrorCode.InventoryAlreadyConfirmed =>
                "Inventory already confirmed",
            InventoryReservationErrorCode.InventoryAlreadyReleased =>
                "Inventory already released",
            _ => "Inventory operation failed"
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
}

public sealed class InventoryReleaseRequestDto
{
    public string? Reason { get; set; }
}
