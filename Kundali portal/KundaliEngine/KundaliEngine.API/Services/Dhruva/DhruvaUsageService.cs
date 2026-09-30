using KundaliEngine.API.Services.Billing;
using KundaliEngine.API.Services.Entitlements;
using KundaliEngine.Domain.Entities;
using KundaliEngine.Domain.Subscriptions;
using KundaliEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Services.Dhruva;

public interface IDhruvaUsageService
{
    Task<DhruvaAllowance> GetAllowanceAsync(User user, CancellationToken cancellationToken);

    Task<bool> TryConsumeLifeQuestionAsync(User user, CancellationToken cancellationToken);
}

public sealed class DhruvaAllowance
{
    public bool DhruvaAiEnabled { get; init; }

    public int? LifeQuestionsRemaining { get; init; }

    public int? LifeQuestionDailyLimit { get; init; }
}

public sealed class DhruvaUsageService : IDhruvaUsageService
{
    public const int FreeLifeQuestionsPerDay = 2;

    private readonly KundaliDbContext _dbContext;
    private readonly IEntitlementService _entitlements;
    private readonly IPlanCatalog _planCatalog;

    public DhruvaUsageService(KundaliDbContext dbContext, IEntitlementService entitlements, IPlanCatalog planCatalog)
    {
        _dbContext = dbContext;
        _entitlements = entitlements;
        _planCatalog = planCatalog;
    }

    public static int? Remaining(int? dailyLimit, int usedToday) =>
        dailyLimit is null ? null : Math.Max(0, dailyLimit.Value - usedToday);

    public static string LifeQuestionLimitMessage(int dailyLimit)
    {
        var noun = dailyLimit == 1 ? "life question" : "life questions";
        return $"Free includes {dailyLimit} {noun} a day. Upgrade to Premium for every life question and AI mode.";
    }

    public async Task<DhruvaAllowance> GetAllowanceAsync(User user, CancellationToken cancellationToken)
    {
        var settings = await ReadSettingsAsync(user, cancellationToken);
        var used = settings.LifeQuestionLimit is null ? 0 : await CountTodayAsync(user.Id, cancellationToken);
        return new DhruvaAllowance
        {
            DhruvaAiEnabled = settings.AiChatEnabled,
            LifeQuestionsRemaining = Remaining(settings.LifeQuestionLimit, used),
            LifeQuestionDailyLimit = settings.LifeQuestionLimit
        };
    }

    public async Task<bool> TryConsumeLifeQuestionAsync(User user, CancellationToken cancellationToken)
    {
        var settings = await ReadSettingsAsync(user, cancellationToken);
        if (settings.LifeQuestionLimit is null)
        {
            return true;
        }

        if (settings.LifeQuestionLimit <= 0)
        {
            return false;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var row = await _dbContext.DhruvaDailyUsages
            .FirstOrDefaultAsync(item => item.UserId == user.Id && item.UsageDate == today, cancellationToken);

        if (row is null)
        {
            _dbContext.DhruvaDailyUsages.Add(new DhruvaDailyUsage
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                UsageDate = today,
                LifeQuestionCount = 1
            });
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        if (row.LifeQuestionCount >= settings.LifeQuestionLimit)
        {
            return false;
        }

        row.LifeQuestionCount += 1;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<(bool AiChatEnabled, int? LifeQuestionLimit)> ReadSettingsAsync(User user, CancellationToken cancellationToken)
    {
        var planCode = _entitlements.GetEffectivePlanCode(user);
        var plan = await _planCatalog.FindAsync(planCode, cancellationToken);
        if (plan is not null)
        {
            return (plan.AiChatEnabled, plan.LifeQuestionLimit);
        }

        var paid = PlanCodes.IsPaid(planCode);
        return (paid, paid ? null : FreeLifeQuestionsPerDay);
    }

    private async Task<int> CountTodayAsync(Guid userId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _dbContext.DhruvaDailyUsages
            .Where(item => item.UserId == userId && item.UsageDate == today)
            .Select(item => item.LifeQuestionCount)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
