using System.Security.Claims;
using KundaliEngine.API.DTOs.Billing;
using KundaliEngine.API.Services.Billing;
using KundaliEngine.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Controllers;

[ApiController]
public class RazorpayPaymentsController : ControllerBase
{
    private readonly IBillingService _billingService;
    private readonly KundaliDbContext _dbContext;

    public RazorpayPaymentsController(IBillingService billingService, KundaliDbContext dbContext)
    {
        _billingService = billingService;
        _dbContext = dbContext;
    }

    [Authorize]
    [HttpPost("/api/create-order")]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateRazorpayOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = await _billingService.CreatePaymentOrderAsync(request, cancellationToken);
            return Ok(new
            {
                order_id = order.OrderId,
                amount = order.Amount,
                currency = order.Currency
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Razorpay authentication failed." });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("/api/verify-payment")]
    public async Task<IActionResult> VerifyPayment(
        [FromBody] VerifyRazorpayPaymentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await GetCurrentUserAsync(cancellationToken);
            var result = await _billingService.VerifyPaymentAsync(user, request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("signature", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { verified = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    private async Task<KundaliEngine.Domain.Entities.User?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return null;
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(item => item.Id == parsedUserId, cancellationToken);
        return user == null || !user.IsActive ? null : user;
    }
}
