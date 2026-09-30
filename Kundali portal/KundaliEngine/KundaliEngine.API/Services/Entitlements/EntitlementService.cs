using KundaliEngine.API.Services.Billing;
using KundaliEngine.API.Services.Dhruva;
using KundaliEngine.Domain.Entities;
using KundaliEngine.Domain.Subscriptions;
using Microsoft.Extensions.Options;

namespace KundaliEngine.API.Services.Entitlements;

public class EntitlementService : IEntitlementService
{
    private readonly SubscriptionOptions _options;
    private readonly PlanCatalogCache _planCatalog;

    public EntitlementService(IOptions<SubscriptionOptions> options, PlanCatalogCache planCatalog)
    {
        _options = options.Value;
        _planCatalog = planCatalog;
    }

    public string GetEffectivePlanCode(User user)
    {
        var planCode = PlanCodes.Normalize(user.PlanCode);
        if (!PlanCodes.IsPaid(planCode))
        {
            return PlanCodes.Free;
        }

        if (user.PlanExpiresAt != null && user.PlanExpiresAt <= DateTimeOffset.UtcNow)
        {
            return PlanCodes.Free;
        }

        return planCode;
    }

    public bool IsPaid(User user) => PlanCodes.IsPaid(GetEffectivePlanCode(user));

    public bool IsPremium(User user) => IsPaid(user);

    public bool IsPlus(User user) => PlanCodes.IsPlus(GetEffectivePlanCode(user));

    public int GetDefaultKundaliSaveLimit(string planCode)
    {
        return FindPlan(planCode)?.KundaliSaveLimit ?? DefaultKundaliSaveLimit(planCode);
    }

    public int GetMatchSaveLimit(User user)
    {
        return FindPlan(GetEffectivePlanCode(user))?.MatchSaveLimit
            ?? DefaultMatchSaveLimit(GetEffectivePlanCode(user));
    }

    public EntitlementSnapshot GetSnapshot(User user)
    {
        var planCode = GetEffectivePlanCode(user);
        var isPaid = PlanCodes.IsPaid(planCode);
        var configured = _planCatalog.Find(planCode);
        var aiChatEnabled = configured?.AiChatEnabled ?? isPaid;
        int? lifeQuestionLimit = configured is null
            ? (isPaid ? null : DhruvaUsageService.FreeLifeQuestionsPerDay)
            : configured.LifeQuestionLimit;
        return new EntitlementSnapshot
        {
            PlanCode = planCode,
            PlanExpiresAt = user.PlanExpiresAt,
            IsPaid = isPaid,
            IsPremium = isPaid,
            IsPlus = PlanCodes.IsPlus(planCode),
            KundaliSaveLimit = user.KundaliSaveLimit,
            MatchSaveLimit = GetMatchSaveLimit(user),
            DhruvaAiEnabled = aiChatEnabled,
            LifeQuestionsRemaining = lifeQuestionLimit
        };
    }

    public void ApplyPlan(User user, string planCode, DateTimeOffset? expiresAt)
    {
        var normalized = PlanCodes.Normalize(planCode);
        user.PlanCode = normalized;
        user.PlanExpiresAt = normalized == PlanCodes.Free ? null : expiresAt;
        user.KundaliSaveLimit = GetDefaultKundaliSaveLimit(normalized);
        user.UpdatedAt = DateTime.UtcNow;
    }

    private SubscriptionPlanOption? FindPlan(string planCode)
    {
        var configured = _planCatalog.Find(planCode);
        if (configured != null)
        {
            return new SubscriptionPlanOption
            {
                Code = configured.Code,
                Name = configured.Name,
                Tagline = configured.Tagline,
                MonthlyAmountPaise = configured.MonthlyAmountPaise,
                YearlyAmountPaise = configured.YearlyAmountPaise,
                KundaliSaveLimit = configured.KundaliSaveLimit,
                MatchSaveLimit = configured.MatchSaveLimit,
                RazorpayMonthlyPlanId = configured.RazorpayMonthlyPlanId,
                RazorpayYearlyPlanId = configured.RazorpayYearlyPlanId
            };
        }

        var normalized = PlanCodes.Normalize(planCode);
        return _options.Plans.FirstOrDefault(plan => PlanCodes.Normalize(plan.Code) == normalized);
    }

    private static int DefaultKundaliSaveLimit(string planCode) => PlanCodes.Normalize(planCode) switch
    {
        PlanCodes.PremiumPlus => 500,
        PlanCodes.Premium => 50,
        _ => 5
    };

    private static int DefaultMatchSaveLimit(string planCode) => PlanCodes.Normalize(planCode) switch
    {
        PlanCodes.PremiumPlus => 200,
        PlanCodes.Premium => 25,
        _ => 2
    };
}
