using HoraLife.PredictionEngine.Enums;

namespace HoraLife.PredictionEngine.Models;

public sealed record PredictionClaim
{
    /// <summary>
    /// Stable semantic key such as career_growth_support
    /// or career_delay_pressure.
    /// </summary>
    public required string Key { get; init; }

    public required PredictionLifeArea LifeArea { get; init; }

    public string? FocusKey { get; init; }

    public required PredictionTone Tone { get; init; }

    public DateTimeOffset? StartDate { get; init; }

    public DateTimeOffset? EndDate { get; init; }

    /// <summary>
    /// Rules responsible for producing this claim.
    /// </summary>
    public required IReadOnlyList<string> RuleIds { get; init; }

    /// <summary>
    /// Evidence used by the rules, such as house:10,
    /// planet:Saturn or dasha:period-id.
    /// </summary>
    public required IReadOnlyList<string> EvidenceKeys { get; init; }
    public required PredictionClaimStage Stage { get; init; }
}