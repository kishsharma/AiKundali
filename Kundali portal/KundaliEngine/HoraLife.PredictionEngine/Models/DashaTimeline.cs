using HoraLife.PredictionEngine.Enums;

public sealed record DashaTimeline
{
    public IReadOnlyList<DashaPeriod> Periods { get; init; }
        = Array.Empty<DashaPeriod>();
}

public sealed record DashaPeriod
{
    public required string PeriodId { get; init; }

    public string? ParentPeriodId { get; init; }

    public required DashaLevel Level { get; init; }

    public required string Planet { get; init; }

    public required DateTimeOffset StartDate { get; init; }

    public required DateTimeOffset EndDate { get; init; }
}