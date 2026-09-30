using System.Security.Claims;
using KundaliEngine.API.DTOs.Billing;
using KundaliEngine.API.Services.Billing;
using KundaliEngine.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Controllers;

[ApiController]
[Route("api/billing")]
public class BillingController : ControllerBase
{
    private readonly IBillingService _billingService;
    private readonly KundaliDbContext _dbContext;

    public BillingController(IBillingService billingService, KundaliDbContext dbContext)
    {
        _billingService = billingService;
        _dbContext = dbContext;
    }

    [AllowAnonymous]
    [HttpGet("plans")]
    public async Task<ActionResult<IReadOnlyList<BillingPlanDto>>> GetPlans(CancellationToken cancellationToken)
    {
        return Ok(await _billingService.ListPlansAsync(cancellationToken));
    }

    [Authorize]
    [HttpGet("status")]
    public async Task<ActionResult<BillingStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user == null)
        {
            return Unauthorized();
        }

        return Ok(await _billingService.GetStatusAsync(user, cancellationToken));
    }

    [Authorize]
    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutSessionDto>> Checkout(
        [FromBody] CreateCheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user == null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _billingService.CreateCheckoutAsync(user, request, cancellationToken));
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
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("coupons/preview")]
    public async Task<ActionResult<CouponPreviewDto>> PreviewCoupon(
        [FromBody] CouponPreviewRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _billingService.PreviewCouponAsync(request, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("verify-payment")]
    public async Task<ActionResult<VerifyRazorpayPaymentResponse>> VerifyPayment(
        [FromBody] VerifyRazorpayPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user == null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _billingService.VerifyPaymentAsync(user, request, cancellationToken));
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
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user == null)
        {
            return Unauthorized();
        }

        try
        {
            await _billingService.CancelAsync(user, cancellationToken);
            return Ok(new { message = "Subscription will stop at the end of the current period." });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["X-Razorpay-Signature"].ToString();

        try
        {
            await _billingService.HandleWebhookAsync(payload, signature, cancellationToken);
            return Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
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
