namespace HoraLife.PredictionEngine.Models;

public sealed record PredictionAnswer
{
    public required string Outcome { get; init; }

    public IReadOnlyList<string> ReasonKeys { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> EvidenceKeys { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<PredictionTimingWindow> TimingWindows { get; init; }
        = Array.Empty<PredictionTimingWindow>();
}

public sealed record PredictionTimingWindow
{
    public required DateTimeOffset StartDate { get; init; }

    public required DateTimeOffset EndDate { get; init; }

    public required string Strength { get; init; }

    public IReadOnlyList<string> TriggerPlanets { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> EvidenceKeys { get; init; }
        = Array.Empty<string>();
}
