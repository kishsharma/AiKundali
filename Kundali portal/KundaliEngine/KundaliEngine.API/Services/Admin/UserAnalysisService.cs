using KundaliEngine.API.Services.Dhruva;
using KundaliEngine.API.Services.Entitlements;
using KundaliEngine.Domain.Entities;
using KundaliEngine.Domain.Subscriptions;
using KundaliEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Services.Admin;

public interface IUserAnalysisService
{
    Task RecordQuestionAsync(
        Guid userId,
        bool isAiMode,
        string kind,
        string? question,
        int promptTokens,
        int completionTokens,
        int totalTokens,
        CancellationToken cancellationToken);

    Task<UserAnalysisResponse> GetAsync(CancellationToken cancellationToken);
}

public sealed class UserAnalysisService : IUserAnalysisService
{
    private readonly KundaliDbContext _dbContext;
    private readonly IEntitlementService _entitlements;

    public UserAnalysisService(KundaliDbContext dbContext, IEntitlementService entitlements)
    {
        _dbContext = dbContext;
        _entitlements = entitlements;
    }

    public async Task RecordQuestionAsync(
        Guid userId,
        bool isAiMode,
        string kind,
        string? question,
        int promptTokens,
        int completionTokens,
        int totalTokens,
        CancellationToken cancellationToken)
    {
        var normalizedKind = NormalizeKind(kind);
        _dbContext.DhruvaQuestionLogs.Add(new DhruvaQuestionLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AskedAt = DateTimeOffset.UtcNow,
            IsAiMode = isAiMode,
            Kind = normalizedKind,
            Language = isAiMode && normalizedKind == "custom"
                ? DhruvaQuestionLanguage.Classify(question)
                : DhruvaQuestionLanguage.English,
            PromptTokens = Math.Max(0, promptTokens),
            CompletionTokens = Math.Max(0, completionTokens),
            TotalTokens = Math.Max(0, totalTokens),
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserAnalysisResponse> GetAsync(CancellationToken cancellationToken)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .Select(user => new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.PreferredLanguage,
                user.PlanCode,
                user.PlanExpiresAt,
                user.KundaliSaveLimit,
                user.IsActive,
            })
            .ToListAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var questionsToday = await _dbContext.DhruvaDailyUsages
            .AsNoTracking()
            .Where(item => item.UsageDate == today)
            .ToDictionaryAsync(item => item.UserId, item => item.LifeQuestionCount, cancellationToken);

        var chartCounts = await _dbContext.Kundalis
            .AsNoTracking()
            .Where(item => item.UserId != null)
            .GroupBy(item => item.UserId)
            .Select(group => new { UserId = group.Key!.Value, Count = group.Count() })
            .ToDictionaryAsync(item => item.UserId, item => item.Count, cancellationToken);

        var matchCounts = await _dbContext.SavedCompatibilities
            .AsNoTracking()
            .GroupBy(item => item.UserId)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);

        var logs = await _dbContext.DhruvaQuestionLogs
            .AsNoTracking()
            .Select(item => new
            {
                item.UserId,
                item.AskedAt,
                item.IsAiMode,
                item.Kind,
                item.Language,
                item.TotalTokens,
            })
            .ToListAsync(cancellationToken);

        var logStats = logs
            .GroupBy(item => item.UserId)
            .ToDictionary(group => group.Key, group => group.ToList());

        var rows = users.Select(user =>
        {
            var planCode = EffectivePlan(user.PlanCode, user.PlanExpiresAt);
            var snapshot = _entitlements.GetSnapshot(new User
            {
                Id = user.Id,
                PlanCode = user.PlanCode,
                PlanExpiresAt = user.PlanExpiresAt,
                KundaliSaveLimit = user.KundaliSaveLimit,
            });
            questionsToday.TryGetValue(user.Id, out var usedToday);
            chartCounts.TryGetValue(user.Id, out var chartsUsed);
            matchCounts.TryGetValue(user.Id, out var matchesUsed);
            logStats.TryGetValue(user.Id, out var events);
            events ??= [];
            var customAi = events.Where(item => item.IsAiMode && item.Kind == "custom").ToList();
            var loggedToday = events.Count(item => item.AskedAt.UtcDateTime.Date == DateTime.UtcNow.Date);
            return new UserAnalysisRow
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PreferredLanguage = user.PreferredLanguage,
                PlanCode = planCode,
                IsActive = user.IsActive,
                ChartsUsed = chartsUsed,
                ChartsLimit = snapshot.KundaliSaveLimit,
                MatchesUsed = matchesUsed,
                MatchesLimit = snapshot.MatchSaveLimit,
                QuestionsUsedToday = Math.Max(usedToday, loggedToday),
                QuestionsDailyLimit = snapshot.LifeQuestionsRemaining,
                AiQuestions = events.Count(item => item.IsAiMode),
                NonAiQuestions = events.Count(item => !item.IsAiMode),
                HindiQuestions = customAi.Count(item => item.Language == DhruvaQuestionLanguage.Hindi),
                EnglishQuestions = customAi.Count(item => item.Language == DhruvaQuestionLanguage.English),
                OtherLanguageQuestions = customAi.Count(item => item.Language == DhruvaQuestionLanguage.Other),
                TotalTokens = events.Sum(item => item.TotalTokens),
            };
        }).ToList();

        return new UserAnalysisResponse
        {
            Plans = PlanCodes.All.Select(code => new UserAnalysisPlanTile
            {
                Code = code,
                Name = PlanName(code),
                UserCount = rows.Count(row => row.PlanCode == code),
            }).ToList(),
            Users = rows
                .OrderBy(row => row.FullName)
                .ThenBy(row => row.Email)
                .ToList(),
        };
    }

    private static string NormalizeKind(string? kind)
    {
        var value = string.IsNullOrWhiteSpace(kind) ? "custom" : kind.Trim().ToLowerInvariant();
        var normalized = value switch
        {
            "qa" or "custom" => "custom",
            "janma_story" or "story" => "story",
            "dasha_focus" or "dasha" => "dasha",
            "compatibility" => "compatibility",
            "catalog" => "catalog",
            _ => "custom",
        };
        return normalized.Length > 40 ? normalized[..40] : normalized;
    }

    private static string EffectivePlan(string? planCode, DateTimeOffset? expiresAt)
    {
        var normalized = PlanCodes.Normalize(planCode);
        if (!PlanCodes.IsPaid(normalized))
        {
            return PlanCodes.Free;
        }

        if (expiresAt != null && expiresAt <= DateTimeOffset.UtcNow)
        {
            return PlanCodes.Free;
        }

        return normalized;
    }

    private static string PlanName(string code) => code switch
    {
        PlanCodes.PremiumPlus => "Premium Plus",
        PlanCodes.Premium => "Premium",
        _ => "Free",
    };
}

public sealed class UserAnalysisResponse
{
    public IReadOnlyList<UserAnalysisPlanTile> Plans { get; init; } = [];

    public IReadOnlyList<UserAnalysisRow> Users { get; init; } = [];
}

public sealed class UserAnalysisPlanTile
{
    public string Code { get; init; } = PlanCodes.Free;

    public string Name { get; init; } = "Free";

    public int UserCount { get; init; }
}

public sealed class UserAnalysisRow
{
    public Guid Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string? PreferredLanguage { get; init; }

    public string PlanCode { get; init; } = PlanCodes.Free;

    public bool IsActive { get; init; }

    public int ChartsUsed { get; init; }

    public int ChartsLimit { get; init; }

    public int MatchesUsed { get; init; }

    public int MatchesLimit { get; init; }

    public int QuestionsUsedToday { get; init; }

    public int? QuestionsDailyLimit { get; init; }

    public int AiQuestions { get; init; }

    public int NonAiQuestions { get; init; }

    public int HindiQuestions { get; init; }

    public int EnglishQuestions { get; init; }

    public int OtherLanguageQuestions { get; init; }

    public int TotalTokens { get; init; }
}
