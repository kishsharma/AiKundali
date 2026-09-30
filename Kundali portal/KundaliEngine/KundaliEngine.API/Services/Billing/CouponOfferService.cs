using KundaliEngine.Domain.Entities;
using KundaliEngine.Domain.Subscriptions;
using KundaliEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Services.Billing;

public interface ICouponOfferService
{
    Task<IReadOnlyList<CouponOffer>> ListAsync(CancellationToken cancellationToken);

    Task<CouponOffer> CreateAsync(SaveCouponRequest request, CancellationToken cancellationToken);

    Task<CouponOffer> UpdateAsync(Guid id, SaveCouponRequest request, CancellationToken cancellationToken);

    Task<CouponQuote> QuoteAsync(string? code, string planCode, string billingCycle, int amountPaise, CancellationToken cancellationToken);

    Task RedeemAsync(UserSubscription subscription, CancellationToken cancellationToken);
}

public sealed class SaveCouponRequest
{
    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public int PercentOff { get; set; }

    public List<string> PlanCodes { get; set; } = [];

    public List<string> BillingCycles { get; set; } = [];

    public DateTimeOffset? StartsAt { get; set; }

    public DateTimeOffset? EndsAt { get; set; }

    public int? MaxRedemptions { get; set; }

    public bool Active { get; set; } = true;
}

public sealed class CouponQuote
{
    public Guid CouponOfferId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int PercentOff { get; set; }

    public int OriginalAmountPaise { get; set; }

    public int AmountPaise { get; set; }
}

public sealed class CouponOfferService : ICouponOfferService
{
    private readonly KundaliDbContext _dbContext;

    public CouponOfferService(KundaliDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CouponOffer>> ListAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.CouponOffers
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<CouponOffer> CreateAsync(SaveCouponRequest request, CancellationToken cancellationToken)
    {
        var offer = new CouponOffer
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow
        };
        Apply(offer, request, isNew: true);
        var codeTaken = await _dbContext.CouponOffers.AnyAsync(item => item.Code == offer.Code, cancellationToken);
        if (codeTaken)
        {
            throw new ArgumentException("That coupon code is already in use.");
        }

        _dbContext.CouponOffers.Add(offer);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return offer;
    }

    public async Task<CouponOffer> UpdateAsync(Guid id, SaveCouponRequest request, CancellationToken cancellationToken)
    {
        var offer = await _dbContext.CouponOffers.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new ArgumentException("That coupon was not found.");
        Apply(offer, request, isNew: false);
        var codeTaken = await _dbContext.CouponOffers.AnyAsync(item => item.Id != id && item.Code == offer.Code, cancellationToken);
        if (codeTaken)
        {
            throw new ArgumentException("That coupon code is already in use.");
        }

        offer.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return offer;
    }

    public async Task<CouponQuote> QuoteAsync(string? code, string planCode, string billingCycle, int amountPaise, CancellationToken cancellationToken)
    {
        var normalizedCode = NormalizeCode(code);
        if (normalizedCode.Length == 0)
        {
            throw new ArgumentException("Enter a coupon code.");
        }

        var offer = await _dbContext.CouponOffers.AsNoTracking().FirstOrDefaultAsync(item => item.Code == normalizedCode, cancellationToken)
            ?? throw new ArgumentException("That coupon code is not valid.");

        if (!offer.Active)
        {
            throw new ArgumentException("That coupon is no longer active.");
        }

        var now = DateTimeOffset.UtcNow;
        if (offer.StartsAt is { } starts && now < starts)
        {
            throw new ArgumentException("That coupon is not active yet.");
        }

        if (offer.EndsAt is { } ends && now > ends)
        {
            throw new ArgumentException("That coupon has expired.");
        }

        if (offer.MaxRedemptions is { } max && offer.RedeemedCount >= max)
        {
            throw new ArgumentException("That coupon has reached its redemption limit.");
        }

        if (!Targets(offer.PlanCodes, planCode))
        {
            throw new ArgumentException("That coupon does not apply to this plan.");
        }

        var cycle = string.Equals(billingCycle, "yearly", StringComparison.OrdinalIgnoreCase) ? "yearly" : "monthly";
        if (!Targets(offer.BillingCycles, cycle))
        {
            throw new ArgumentException("That coupon does not apply to this billing cycle.");
        }

        var discounted = (int)Math.Round(amountPaise * (100m - offer.PercentOff) / 100m, MidpointRounding.AwayFromZero);
        if (discounted < 100)
        {
            throw new ArgumentException("This coupon would bring the price below ₹1.");
        }

        return new CouponQuote
        {
            CouponOfferId = offer.Id,
            Code = offer.Code,
            Name = offer.Name,
            PercentOff = offer.PercentOff,
            OriginalAmountPaise = amountPaise,
            AmountPaise = discounted
        };
    }

    public async Task RedeemAsync(UserSubscription subscription, CancellationToken cancellationToken)
    {
        if (subscription.CouponOfferId is not { } offerId || string.IsNullOrWhiteSpace(subscription.RazorpayOrderId))
        {
            return;
        }

        var already = await _dbContext.CouponRedemptions.AnyAsync(
            item => item.RazorpayOrderId == subscription.RazorpayOrderId,
            cancellationToken);
        if (already)
        {
            return;
        }

        var offer = await _dbContext.CouponOffers.FirstOrDefaultAsync(item => item.Id == offerId, cancellationToken);
        if (offer == null)
        {
            return;
        }

        offer.RedeemedCount += 1;
        offer.UpdatedAt = DateTime.UtcNow;
        _dbContext.CouponRedemptions.Add(new CouponRedemption
        {
            Id = Guid.NewGuid(),
            CouponOfferId = offer.Id,
            UserId = subscription.UserId,
            UserSubscriptionId = subscription.Id,
            RazorpayOrderId = subscription.RazorpayOrderId,
            PlanCode = subscription.PlanCode,
            BillingCycle = subscription.BillingCycle,
            PercentOff = offer.PercentOff,
            AmountPaise = 0,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static void Apply(CouponOffer offer, SaveCouponRequest request, bool isNew)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length is < 2 or > 80)
        {
            throw new ArgumentException("Offer name must be 2 to 80 characters.");
        }

        var code = NormalizeCode(request.Code);
        if (code.Length is < 3 or > 24 || code.Any(ch => !char.IsLetterOrDigit(ch) && ch != '-'))
        {
            throw new ArgumentException("Coupon code must be 3 to 24 letters, numbers, or hyphens.");
        }

        if (request.PercentOff is < 1 or > 99)
        {
            throw new ArgumentException("Discount percent must be from 1 to 99.");
        }

        var plans = NormalizeTargets(request.PlanCodes, static value =>
        {
            if (!PlanCodes.TryParse(value, out var parsed) || !PlanCodes.IsPaid(parsed))
            {
                return null;
            }

            return parsed;
        });
        if (plans.Count == 0)
        {
            throw new ArgumentException("Choose Premium, Premium Plus, or both.");
        }

        var cycles = NormalizeTargets(request.BillingCycles, value =>
        {
            if (string.Equals(value, "monthly", StringComparison.OrdinalIgnoreCase))
            {
                return "monthly";
            }

            if (string.Equals(value, "yearly", StringComparison.OrdinalIgnoreCase))
            {
                return "yearly";
            }

            return null;
        });
        if (cycles.Count == 0)
        {
            throw new ArgumentException("Choose monthly, yearly, or both.");
        }

        if (request.StartsAt is { } starts && request.EndsAt is { } ends && ends <= starts)
        {
            throw new ArgumentException("The end date must be after the start date.");
        }

        if (request.MaxRedemptions is < 1)
        {
            throw new ArgumentException("Redemption cap must be at least 1, or left empty.");
        }

        if (!isNew && request.MaxRedemptions is { } max && max < offer.RedeemedCount)
        {
            throw new ArgumentException("Redemption cap cannot be lower than the number already used.");
        }

        offer.Name = name;
        offer.Code = code;
        offer.PercentOff = request.PercentOff;
        offer.PlanCodes = string.Join(',', plans);
        offer.BillingCycles = string.Join(',', cycles);
        offer.StartsAt = request.StartsAt;
        offer.EndsAt = request.EndsAt;
        offer.MaxRedemptions = request.MaxRedemptions;
        offer.Active = request.Active;
    }

    private static string NormalizeCode(string? code)
    {
        return (code ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static List<string> NormalizeTargets(IEnumerable<string>? values, Func<string, string?> map)
    {
        return (values ?? [])
            .Select(map)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool Targets(string stored, string value)
    {
        return stored.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase));
    }
}
