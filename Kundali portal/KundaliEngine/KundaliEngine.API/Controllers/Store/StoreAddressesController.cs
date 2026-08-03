using System.Security.Claims;
using HoraLife.Store.Application.Addresses.Dtos;
using HoraLife.Store.Application.Addresses.Models;
using HoraLife.Store.Application.Interfaces;
using KundaliEngine.Api.DTOs.Store.Addresses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.Api.Controllers.Store;

[ApiController]
[Authorize]
[Route("api/store/addresses")]
public sealed class StoreAddressesController : ControllerBase
{
    private readonly IStoreAddressService _addressService;

    public StoreAddressesController(
        IStoreAddressService addressService)
    {
        _addressService = addressService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<CustomerAddressDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<CustomerAddressDto>>>
        GetAddresses(
            CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return InvalidUserClaim();
        }

        var addresses =
            await _addressService.GetAddressesAsync(
                userId,
                cancellationToken);

        return Ok(addresses);
    }

    [HttpGet("{addressId:guid}")]
    [ProducesResponseType(
        typeof(CustomerAddressDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerAddressDto>> GetAddressById(
        Guid addressId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return InvalidUserClaim();
        }

        var address =
            await _addressService.GetAddressByIdAsync(
                userId,
                addressId,
                cancellationToken);

        return address is null
            ? NotFound()
            : Ok(address);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(CustomerAddressDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CustomerAddressDto>> CreateAddress(
        [FromBody] SaveCustomerAddressRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return InvalidUserClaim();
        }

        if (!request.TryToModel(
                out var model,
                out var validationError))
        {
            return InvalidAddressRequest(validationError);
        }

        var result =
            await _addressService.CreateAddressAsync(
                userId,
                model,
                cancellationToken);

        if (!result.Succeeded)
        {
            return ToProblemResult(
                result.ErrorCode,
                result.ErrorMessage);
        }

        return CreatedAtAction(
            nameof(GetAddressById),
            new
            {
                addressId = result.Value!.Id
            },
            result.Value);
    }

    [HttpPut("{addressId:guid}")]
    [ProducesResponseType(
        typeof(CustomerAddressDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerAddressDto>> UpdateAddress(
        Guid addressId,
        [FromBody] SaveCustomerAddressRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return InvalidUserClaim();
        }

        if (!request.TryToModel(
                out var model,
                out var validationError))
        {
            return InvalidAddressRequest(validationError);
        }

        var result =
            await _addressService.UpdateAddressAsync(
                userId,
                addressId,
                model,
                cancellationToken);

        return result.Succeeded
            ? Ok(result.Value)
            : ToProblemResult(
                result.ErrorCode,
                result.ErrorMessage);
    }

    [HttpDelete("{addressId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAddress(
        Guid addressId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return InvalidUserClaim();
        }

        var result =
            await _addressService.DeleteAddressAsync(
                userId,
                addressId,
                cancellationToken);

        return result.Succeeded
            ? NoContent()
            : ToProblemResult(
                result.ErrorCode,
                result.ErrorMessage);
    }

    [HttpPut("{addressId:guid}/default")]
    [ProducesResponseType(
        typeof(CustomerAddressDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerAddressDto>> SetDefaultAddress(
        Guid addressId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return InvalidUserClaim();
        }

        var result =
            await _addressService.SetDefaultAddressAsync(
                userId,
                addressId,
                cancellationToken);

        return result.Succeeded
            ? Ok(result.Value)
            : ToProblemResult(
                result.ErrorCode,
                result.ErrorMessage);
    }

    private ObjectResult ToProblemResult(
        AddressErrorCode errorCode,
        string? detail)
    {
        var status = errorCode switch
        {
            AddressErrorCode.AddressNotFound
                => StatusCodes.Status404NotFound,

            _ => StatusCodes.Status400BadRequest
        };

        return StatusCode(
            status,
            new ProblemDetails
            {
                Title = errorCode switch
                {
                    AddressErrorCode.AddressNotFound
                        => "Address not found",

                    _ => "Invalid address request"
                },
                Detail = detail,
                Status = status
            });
    }

    private BadRequestObjectResult InvalidAddressRequest(
        string? detail)
    {
        return BadRequest(
            new ProblemDetails
            {
                Title = "Invalid address request",
                Detail = detail,
                Status = StatusCodes.Status400BadRequest
            });
    }

    private bool TryGetUserId(out Guid userId)
    {
        var claimValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
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
