using System.Security.Claims;
using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Application.Orders.Dtos;
using HoraLife.Store.Application.Orders.Models;
using KundaliEngine.Api.DTOs.Store.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.Api.Controllers.Store;

[ApiController]
[Authorize]
[Route("api/store/orders")]
public sealed class StoreOrdersController : ControllerBase
{
    private readonly IStoreOrderService _orderService;

    public StoreOrdersController(
        IStoreOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(CreatedOrderDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(CreateOrderConflictDto),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreatedOrderDto>> Create(
        [FromBody] CreateOrderRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return InvalidUserClaim();
        }

        var customerEmail =
            GetCustomerEmailOrLoginIdentifier();

        var result = await _orderService.CreateAsync(
            userId,
            customerEmail,
            request.ToModel(),
            cancellationToken);

        if (result.Succeeded)
        {
            return StatusCode(
                StatusCodes.Status201Created,
                result.Value);
        }

        return ToErrorResult(result);
    }

    private ObjectResult ToErrorResult(
        CreateOrderOperationResult result)
    {
        if (result.ErrorCode is
            CreateOrderErrorCode.CartValidationFailed or
            CreateOrderErrorCode.CheckoutPreviewBlocked or
            CreateOrderErrorCode.OrderAlreadyExists or
            CreateOrderErrorCode.CartEmpty)
        {
            return Conflict(
                new CreateOrderConflictDto(
                    result.ErrorCode.ToString(),
                    result.ErrorMessage ??
                    "The order could not be created.",
                    result.Revalidation));
        }

        var status = result.ErrorCode switch
        {
            CreateOrderErrorCode.CartNotFound or
            CreateOrderErrorCode.ShippingAddressNotFound or
            CreateOrderErrorCode.BillingAddressNotFound
                => StatusCodes.Status404NotFound,

            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(
            status,
            new ProblemDetails
            {
                Title = GetTitle(result.ErrorCode),
                Detail = result.ErrorMessage,
                Status = status
            });
    }

    private static string GetTitle(
        CreateOrderErrorCode errorCode)
    {
        return errorCode switch
        {
            CreateOrderErrorCode.InvalidCustomerEmail =>
                "Customer email is unavailable",

            CreateOrderErrorCode.CartNotFound =>
                "Active cart not found",

            CreateOrderErrorCode.ShippingAddressNotFound =>
                "Shipping address not found",

            CreateOrderErrorCode.BillingAddressNotFound =>
                "Billing address not found",

            CreateOrderErrorCode.UnsupportedDeliveryLocation =>
                "Unsupported delivery location",

            _ => "Order creation failed"
        };
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

    private string GetCustomerEmailOrLoginIdentifier()
    {
        return (
            User.FindFirstValue(ClaimTypes.Email) ??
            User.FindFirstValue("email") ??
            User.FindFirstValue(ClaimTypes.Name) ??
            User.FindFirstValue("name") ??
            string.Empty
        ).Trim();
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
