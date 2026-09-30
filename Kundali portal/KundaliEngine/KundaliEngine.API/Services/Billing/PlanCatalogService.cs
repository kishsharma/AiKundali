using KundaliEngine.Domain.Entities;
using KundaliEngine.Domain.Subscriptions;
using KundaliEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KundaliEngine.API.Services.Billing;

public interface IPlanCatalog
{
    Task<IReadOnlyList<BillingPlanConfig>> GetPlansAsync(CancellationToken cancellationToken);

    Task<BillingPlanConfig?> FindAsync(string planCode, CancellationToken cancellationToken);

    Task<BillingPlanConfig> UpdateAsync(string planCode, UpdatePlanCatalogRequest request, CancellationToken cancellationToken);
}

public sealed class UpdatePlanCatalogRequest
{
    public string Name { get; set; } = string.Empty;

    public string Tagline { get; set; } = string.Empty;

    public int MonthlyAmountPaise { get; set; }

    public int YearlyAmountPaise { get; set; }

    public int KundaliSaveLimit { get; set; }

    public int MatchSaveLimit { get; set; }

    public bool AiChatEnabled { get; set; }

    public int? LifeQuestionLimit { get; set; }

    public bool CheckoutOpen { get; set; }
}

public sealed class PlanCatalogCache
{
    private IReadOnlyList<BillingPlanConfig> _plans = [];

    public IReadOnlyList<BillingPlanConfig> Plans => _plans;

    public void Replace(IReadOnlyList<BillingPlanConfig> plans)
    {
        _plans = plans;
    }

    public BillingPlanConfig? Find(string planCode)
    {
        var normalized = PlanCodes.Normalize(planCode);
        return _plans.FirstOrDefault(plan => PlanCodes.Normalize(plan.Code) == normalized);
    }
}

public sealed class PlanCatalogService : IPlanCatalog
{
    private readonly KundaliDbContext _dbContext;
    private readonly SubscriptionOptions _options;
    private readonly PlanCatalogCache _cache;

    public PlanCatalogService(
        KundaliDbContext dbContext,
        IOptions<SubscriptionOptions> options,
        PlanCatalogCache cache)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _cache = cache;
    }

    public async Task<IReadOnlyList<BillingPlanConfig>> GetPlansAsync(CancellationToken cancellationToken)
    {
        await EnsureSeededAsync(cancellationToken);
        return _cache.Plans;
    }

    public async Task<BillingPlanConfig?> FindAsync(string planCode, CancellationToken cancellationToken)
    {
        await EnsureSeededAsync(cancellationToken);
        return _cache.Find(planCode);
    }

    public async Task<BillingPlanConfig> UpdateAsync(string planCode, UpdatePlanCatalogRequest request, CancellationToken cancellationToken)
    {
        await EnsureSeededAsync(cancellationToken);
        if (!PlanCodes.TryParse(planCode, out var normalized))
        {
            throw new ArgumentException("That plan is not one of the current plans.");
        }

        var plan = await _dbContext.BillingPlanConfigs
            .FirstOrDefaultAsync(item => item.Code == normalized, cancellationToken)
            ?? throw new ArgumentException("That plan is not one of the current plans.");

        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length is < 2 or > 80)
        {
            throw new ArgumentException("Plan name must be 2 to 80 characters.");
        }

        var tagline = (request.Tagline ?? string.Empty).Trim();
        if (tagline.Length > 160)
        {
            throw new ArgumentException("Tagline must be 160 characters or less.");
        }

        if (request.KundaliSaveLimit < 0 || request.MatchSaveLimit < 0 || request.LifeQuestionLimit is < 0)
        {
            throw new ArgumentException("Save limits and the life question limit cannot be negative.");
        }

        if (normalized == PlanCodes.Free)
        {
            plan.MonthlyAmountPaise = 0;
            plan.YearlyAmountPaise = 0;
            plan.CheckoutOpen = false;
        }
        else
        {
            if (request.CheckoutOpen && (request.MonthlyAmountPaise < 100 || request.YearlyAmountPaise < 100))
            {
                throw new ArgumentException("An open paid plan needs a monthly and yearly price of at least ₹1.");
            }

            plan.MonthlyAmountPaise = request.MonthlyAmountPaise;
            plan.YearlyAmountPaise = request.YearlyAmountPaise;
            plan.CheckoutOpen = request.CheckoutOpen;
        }

        plan.Name = name;
        plan.Tagline = tagline;
        plan.KundaliSaveLimit = request.KundaliSaveLimit;
        plan.MatchSaveLimit = request.MatchSaveLimit;
        plan.AiChatEnabled = request.AiChatEnabled;
        plan.LifeQuestionLimit = request.LifeQuestionLimit;
        plan.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await RefreshCacheAsync(cancellationToken);
        return plan;
    }

    private async Task EnsureSeededAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.BillingPlanConfigs.AnyAsync(cancellationToken))
        {
            if (_cache.Plans.Count == 0)
            {
                await RefreshCacheAsync(cancellationToken);
            }

            return;
        }

        var now = DateTime.UtcNow;
        var seeded = _options.Plans
            .Select(plan => PlanCodes.Normalize(plan.Code))
            .Distinct()
            .Where(code => PlanCodes.TryParse(code, out _))
            .Select(code =>
            {
                var source = _options.Plans.First(plan => PlanCodes.Normalize(plan.Code) == code);
                return new BillingPlanConfig
                {
                    Code = code,
                    Name = string.IsNullOrWhiteSpace(source.Name) ? code : source.Name.Trim(),
                    Tagline = source.Tagline?.Trim() ?? string.Empty,
                    MonthlyAmountPaise = code == PlanCodes.Free ? 0 : source.MonthlyAmountPaise,
                    YearlyAmountPaise = code == PlanCodes.Free ? 0 : source.YearlyAmountPaise,
                    KundaliSaveLimit = source.KundaliSaveLimit,
                    MatchSaveLimit = source.MatchSaveLimit,
                    AiChatEnabled = PlanCodes.IsPaid(code),
                    LifeQuestionLimit = PlanCodes.IsPaid(code) ? null : 2,
                    CheckoutOpen = PlanCodes.IsPaid(code),
                    RazorpayMonthlyPlanId = source.RazorpayMonthlyPlanId,
                    RazorpayYearlyPlanId = source.RazorpayYearlyPlanId,
                    UpdatedAt = now
                };
            })
            .ToList();

        if (seeded.All(plan => plan.Code != PlanCodes.Free))
        {
            seeded.Insert(0, new BillingPlanConfig
            {
                Code = PlanCodes.Free,
                Name = "Free",
                Tagline = "Try the portal",
                KundaliSaveLimit = 5,
                MatchSaveLimit = 2,
                AiChatEnabled = false,
                LifeQuestionLimit = 2,
                CheckoutOpen = false,
                UpdatedAt = now
            });
        }

        _dbContext.BillingPlanConfigs.AddRange(seeded);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await RefreshCacheAsync(cancellationToken);
    }

    private async Task RefreshCacheAsync(CancellationToken cancellationToken)
    {
        var plans = await _dbContext.BillingPlanConfigs.AsNoTracking().ToListAsync(cancellationToken);
        _cache.Replace(plans
            .OrderBy(plan => plan.Code == PlanCodes.Free ? 0 : plan.Code == PlanCodes.Premium ? 1 : 2)
            .ToList());
    }
}
