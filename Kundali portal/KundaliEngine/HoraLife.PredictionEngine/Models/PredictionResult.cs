using HoraLife.PredictionEngine.Enums;

namespace HoraLife.PredictionEngine.Models;

public sealed record PredictionResult
{
    public required PredictionLifeArea LifeArea { get; init; }

    public string? FocusKey { get; init; }

    public required PredictionTimeScope TimeScope { get; init; }

    public required DateTimeOffset ReferenceDate { get; init; }

    public DateTimeOffset? FromDate { get; init; }

    public DateTimeOffset? ToDate { get; init; }

    public IReadOnlyList<PredictionClaim> Claims { get; init; }
        = Array.Empty<PredictionClaim>();

    public PredictionSynthesis? Synthesis { get; init; }

    public PredictionAnswer? Answer { get; init; }
}
