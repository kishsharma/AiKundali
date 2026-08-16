namespace KundaliEngine.API.Prediction.DTOs;

public sealed class PredictionResponseDto
{
    public string LifeArea { get; set; } = string.Empty;

    public string TimeScope { get; set; } = string.Empty;

    public DateTimeOffset ReferenceDate { get; set; }

    public string? FocusKey { get; set; }

    public List<PredictionClaimResponseDto> Claims { get; set; } = new();

    public PredictionSynthesisResponseDto? Synthesis { get; set; }

    public PredictionAnswerResponseDto? Answer { get; set; }
}

public sealed class PredictionClaimResponseDto
{
    public string Key { get; set; } = string.Empty;

    public string Tone { get; set; } = string.Empty;

    public string Stage { get; set; } = string.Empty;

    public string? DashaLevel { get; set; }

    public string? DashaPlanet { get; set; }

    public DateTimeOffset? StartDate { get; set; }

    public DateTimeOffset? EndDate { get; set; }

    public IReadOnlyList<string> RuleIds { get; set; }
        = Array.Empty<string>();

    public IReadOnlyList<string> EvidenceKeys { get; set; }
        = Array.Empty<string>();
}

public sealed class PredictionSynthesisResponseDto
{
    public string? OverallTone { get; set; }

    public DateTimeOffset? EffectiveFrom { get; set; }

    public DateTimeOffset? EffectiveTo { get; set; }

    public List<PredictionHouseConnectionResponseDto> HouseConnections { get; set; }
        = new();
}

public sealed class PredictionHouseConnectionResponseDto
{
    public int SourceHouse { get; set; }

    public string LordPlanet { get; set; } = string.Empty;

    public int TargetHouse { get; set; }

    public IReadOnlyList<string> SourceSignificationKeys { get; set; }
        = Array.Empty<string>();

    public IReadOnlyList<string> TargetSignificationKeys { get; set; }
        = Array.Empty<string>();

    public string EvidenceKey { get; set; } = string.Empty;
}

public sealed class PredictionAnswerResponseDto
{
    public string Outcome { get; set; } = string.Empty;

    public IReadOnlyList<string> ReasonKeys { get; set; }
        = Array.Empty<string>();

    public IReadOnlyList<string> EvidenceKeys { get; set; }
        = Array.Empty<string>();

    public List<PredictionTimingWindowResponseDto> TimingWindows { get; set; }
        = new();
}

public sealed class PredictionTimingWindowResponseDto
{
    public DateTimeOffset StartDate { get; set; }

    public DateTimeOffset EndDate { get; set; }

    public string Strength { get; set; } = string.Empty;

    public IReadOnlyList<string> TriggerPlanets { get; set; }
        = Array.Empty<string>();

    public IReadOnlyList<string> EvidenceKeys { get; set; }
        = Array.Empty<string>();
}
