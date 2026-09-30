namespace KundaliEngine.API.Services.Entitlements;

public sealed class EntitlementSnapshot
{
    public string PlanCode { get; init; } = "Free";

    public DateTimeOffset? PlanExpiresAt { get; init; }

    public bool IsPaid { get; init; }

    public bool IsPremium { get; init; }

    public bool IsPlus { get; init; }

    public int KundaliSaveLimit { get; init; }

    public int MatchSaveLimit { get; init; }

    public bool DhruvaAiEnabled { get; init; }

    public int? LifeQuestionsRemaining { get; init; }
}
