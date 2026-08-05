using HoraLife.PredictionEngine.Enums;

namespace HoraLife.PredictionEngine.Models;

public sealed record PredictionRequest
{
    public required PredictionMode Mode { get; init; }

    public required PredictionLifeArea LifeArea { get; init; }

    public required PredictionTimeScope TimeScope { get; init; }

    /// <summary>
    /// Explicit date against which the prediction is evaluated.
    /// Never replace this with DateTime.Now inside the engine.
    /// </summary>
    public required DateTimeOffset ReferenceDate { get; init; }

    public DateTimeOffset? FromDate { get; init; }

    public DateTimeOffset? ToDate { get; init; }

    /// <summary>
    /// Optional normalized concern such as job_change,
    /// promotion, business_growth, or marriage_timing.
    /// </summary>
    public string? FocusKey { get; init; }
}