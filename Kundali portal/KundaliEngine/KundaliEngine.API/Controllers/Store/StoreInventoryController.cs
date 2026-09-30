using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Application.Inventory.Dtos;
using HoraLife.Store.Application.Inventory.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.Api.Controllers.Store;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/store/inventory")]
public sealed class StoreInventoryController : ControllerBase
{
    private readonly IStoreInventoryAdminService _inventoryAdmin;
    private readonly IStoreInventoryReservationService _reservationService;

    public StoreInventoryController(
        IStoreInventoryAdminService inventoryAdmin,
        IStoreInventoryReservationService reservationService)
    {
        _inventoryAdmin = inventoryAdmin;
        _reservationService = reservationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InventoryListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InventoryListItemDto>>> List(
        [FromQuery] bool lowStockOnly = false,
        CancellationToken cancellationToken = default)
    {
        var items = await _inventoryAdmin.ListAsync(lowStockOnly, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{inventoryId:guid}/transactions")]
    [ProducesResponseType(typeof(IReadOnlyList<InventoryTransactionListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<InventoryTransactionListItemDto>>> Transactions(
        Guid inventoryId,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryAdmin.ListTransactionsAsync(inventoryId, cancellationToken);
        if (!result.Succeeded)
        {
            return NotFound(CreateProblem("Inventory not found", result.ErrorMessage, StatusCodes.Status404NotFound));
        }

        return Ok(result.Value);
    }

    [HttpPost("{inventoryId:guid}/adjust")]
    [ProducesResponseType(typeof(InventoryListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InventoryListItemDto>> Adjust(
        Guid inventoryId,
        [FromBody] InventoryAdjustRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryAdmin.AdjustAsync(inventoryId, request, cancellationToken);
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        return ToAdminError(result.ErrorCode, result.ErrorMessage);
    }

    [HttpPut("{inventoryId:guid}/reorder-level")]
    [ProducesResponseType(typeof(InventoryListItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryListItemDto>> SetReorderLevel(
        Guid inventoryId,
        [FromBody] InventoryReorderLevelRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryAdmin.SetReorderLevelAsync(inventoryId, request, cancellationToken);
        if (result.Succeeded)
        {
            return Ok(result.Value);
        }

        return ToAdminError(result.ErrorCode, result.ErrorMessage);
    }

    [HttpPost("release-expired")]
    [ProducesResponseType(typeof(InventoryExpiryReleaseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<InventoryExpiryReleaseDto>> ReleaseExpired(
        [FromQuery] int? maxAgeHours,
        CancellationToken cancellationToken)
    {
        var hours = Math.Max(1, maxAgeHours ?? 24);
        var released = await _reservationService.ReleaseExpiredAsync(
            TimeSpan.FromHours(hours),
            cancellationToken);

        return Ok(new InventoryExpiryReleaseDto(released, hours));
    }

    private ActionResult ToAdminError(
        InventoryAdminErrorCode errorCode,
        string? errorMessage)
    {
        if (errorCode == InventoryAdminErrorCode.InventoryNotFound)
        {
            return NotFound(CreateProblem("Inventory not found", errorMessage, StatusCodes.Status404NotFound));
        }

        if (errorCode == InventoryAdminErrorCode.StockWouldFallBelowReserved)
        {
            return Conflict(CreateProblem("Stock below reserved", errorMessage, StatusCodes.Status409Conflict));
        }

        return BadRequest(CreateProblem("Invalid inventory request", errorMessage, StatusCodes.Status400BadRequest));
    }

    private static ProblemDetails CreateProblem(string title, string? detail, int status)
    {
        return new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = status
        };
    }
}
