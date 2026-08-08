namespace KundaliEngine.API.Prediction.DTOs;

public sealed class PredictionResponseDto
{
    public string LifeArea { get; set; } = string.Empty;

    public string TimeScope { get; set; } = string.Empty;

    public DateTimeOffset ReferenceDate { get; set; }

    public string? FocusKey { get; set; }

    public List<PredictionClaimResponseDto> Claims { get; set; } = new();
}

public sealed class PredictionClaimResponseDto
{
    public string Key { get; set; } = string.Empty;

    public string Tone { get; set; } = string.Empty;

    public string Stage { get; set; } = string.Empty;

    public DateTimeOffset? StartDate { get; set; }

    public DateTimeOffset? EndDate { get; set; }

    public IReadOnlyList<string> RuleIds { get; set; }
        = Array.Empty<string>();

    public IReadOnlyList<string> EvidenceKeys { get; set; }
        = Array.Empty<string>();
}