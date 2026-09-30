using KundaliEngine.API.Services.Billing;
using KundaliEngine.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/billing/admin")]
public class BillingAdminController : ControllerBase
{
    private readonly IPlanCatalog _planCatalog;
    private readonly ICouponOfferService _couponOffers;

    public BillingAdminController(IPlanCatalog planCatalog, ICouponOfferService couponOffers)
    {
        _planCatalog = planCatalog;
        _couponOffers = couponOffers;
    }

    [HttpGet("plans")]
    public async Task<ActionResult<IReadOnlyList<AdminPlanDto>>> GetPlans(CancellationToken cancellationToken)
    {
        var plans = await _planCatalog.GetPlansAsync(cancellationToken);
        return Ok(plans.Select(ToPlanDto).ToList());
    }

    [HttpPut("plans/{code}")]
    public async Task<ActionResult<AdminPlanDto>> UpdatePlan(
        string code,
        [FromBody] UpdatePlanCatalogRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var plan = await _planCatalog.UpdateAsync(code, request, cancellationToken);
            return Ok(ToPlanDto(plan));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("coupons")]
    public async Task<ActionResult<IReadOnlyList<AdminCouponDto>>> GetCoupons(CancellationToken cancellationToken)
    {
        var coupons = await _couponOffers.ListAsync(cancellationToken);
        return Ok(coupons.Select(ToCouponDto).ToList());
    }

    [HttpPost("coupons")]
    public async Task<ActionResult<AdminCouponDto>> CreateCoupon(
        [FromBody] SaveCouponRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var offer = await _couponOffers.CreateAsync(request, cancellationToken);
            return Ok(ToCouponDto(offer));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("coupons/{id:guid}")]
    public async Task<ActionResult<AdminCouponDto>> UpdateCoupon(
        Guid id,
        [FromBody] SaveCouponRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var offer = await _couponOffers.UpdateAsync(id, request, cancellationToken);
            return Ok(ToCouponDto(offer));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static AdminPlanDto ToPlanDto(BillingPlanConfig plan)
    {
        return new AdminPlanDto
        {
            Code = plan.Code,
            Name = plan.Name,
            Tagline = plan.Tagline,
            MonthlyAmountPaise = plan.MonthlyAmountPaise,
            YearlyAmountPaise = plan.YearlyAmountPaise,
            KundaliSaveLimit = plan.KundaliSaveLimit,
            MatchSaveLimit = plan.MatchSaveLimit,
            AiChatEnabled = plan.AiChatEnabled,
            LifeQuestionLimit = plan.LifeQuestionLimit,
            CheckoutOpen = plan.CheckoutOpen,
            UpdatedAt = plan.UpdatedAt
        };
    }

    private static AdminCouponDto ToCouponDto(CouponOffer offer)
    {
        return new AdminCouponDto
        {
            Id = offer.Id,
            Name = offer.Name,
            Code = offer.Code,
            PercentOff = offer.PercentOff,
            PlanCodes = offer.PlanCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            BillingCycles = offer.BillingCycles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            StartsAt = offer.StartsAt,
            EndsAt = offer.EndsAt,
            MaxRedemptions = offer.MaxRedemptions,
            RedeemedCount = offer.RedeemedCount,
            Active = offer.Active
        };
    }
}

public sealed class AdminPlanDto
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Tagline { get; set; } = string.Empty;

    public int MonthlyAmountPaise { get; set; }

    public int YearlyAmountPaise { get; set; }

    public int KundaliSaveLimit { get; set; }

    public int MatchSaveLimit { get; set; }

    public bool AiChatEnabled { get; set; }

    public int? LifeQuestionLimit { get; set; }

    public bool CheckoutOpen { get; set; }

    public DateTime UpdatedAt { get; set; }
}

public sealed class AdminCouponDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public int PercentOff { get; set; }

    public string[] PlanCodes { get; set; } = [];

    public string[] BillingCycles { get; set; } = [];

    public DateTimeOffset? StartsAt { get; set; }

    public DateTimeOffset? EndsAt { get; set; }

    public int? MaxRedemptions { get; set; }

    public int RedeemedCount { get; set; }

    public bool Active { get; set; }
}
