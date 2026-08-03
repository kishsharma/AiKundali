using System.Security.Claims;
using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Application.Orders.Items.Dtos;
using HoraLife.Store.Application.Orders.Items.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.Api.Controllers.Store;

[ApiController]
[Authorize]
[Route("api/store/orders/{orderId:guid}/items")]
public sealed class StoreOrderItemsController : ControllerBase
{
    private readonly IStoreOrderItemService _service;

    public StoreOrderItemsController(IStoreOrderItemService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreatedOrderItemsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreatedOrderItemsDto>> Create(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var result = await _service.CreateAsync(userId, orderId, cancellationToken);
        if (result.Succeeded)
            return StatusCode(StatusCodes.Status201Created, result.Value);

        if (result.ErrorCode == CreateOrderItemsErrorCode.OrderNotFound)
            return NotFound(new ProblemDetails
            {
                Title = "Order not found",
                Detail = result.ErrorMessage,
                Status = StatusCodes.Status404NotFound
            });

        return Conflict(new ProblemDetails
        {
            Title = result.ErrorCode.ToString(),
            Detail = result.ErrorMessage,
            Status = StatusCodes.Status409Conflict
        });
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? User.FindFirstValue("userId")
            ?? User.FindFirstValue("user_id");

        return Guid.TryParse(value, out userId);
    }
}
