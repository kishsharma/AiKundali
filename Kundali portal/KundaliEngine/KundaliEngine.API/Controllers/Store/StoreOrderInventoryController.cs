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
public sealed class StoreOrderInventoryController :  ControllerBase
{
    private readonly IStoreInventoryReservationService _reservationService;

    public StoreOrderInventoryController(  IStoreInventoryReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    [HttpPost("reserve")] [ProducesResponseType(typeof(InventoryReservationDto), StatusCodes.Status201Created)] 
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType( typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType( typeof(ProblemDetails),  StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InventoryReservationDto>> Reserve( Guid orderId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized( new ProblemDetails     {
                    Title = "Invalid authenticated user",
                    Detail ="The access token does not contain a valid GUID user identifier.",
                    Status = StatusCodes.Status401Unauthorized
                });
        }

        var result = await _reservationService.ReserveAsync(userId, orderId,cancellationToken);

        if (result.Succeeded)
        {
            return StatusCode(
                StatusCodes.Status201Created,
                result.Value);
        }

        if (result.ErrorCode ==
            InventoryReservationErrorCode.OrderNotFound)
        {
            return NotFound(
                CreateProblem(
                    "Order not found",
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

            _ => "Inventory reservation failed"
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
