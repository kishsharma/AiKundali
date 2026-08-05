using HoraLife.PredictionEngine.Enums;

namespace HoraLife.PredictionEngine.Models;

public sealed record DoshaPredictionData
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public string Category { get; init; } = string.Empty;

    public string Severity { get; init; } = string.Empty;

    public IReadOnlyList<string> InvolvedPlanets { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<int> InvolvedHouses { get; init; }
        = Array.Empty<int>();

    public string Reason { get; init; } = string.Empty;

    public IReadOnlyList<DoshaImpactData> Impacts { get; init; }
        = Array.Empty<DoshaImpactData>();

    public IReadOnlyList<DoshaActivationPredictionData> Activations { get; init; }
        = Array.Empty<DoshaActivationPredictionData>();
}

public sealed record DoshaImpactData
{
    public required PredictionLifeArea LifeArea { get; init; }

    public required string RuleId { get; init; }

    public required string InterpretationKey { get; init; }
}

public sealed record DoshaActivationPredictionData
{
    public required DateTimeOffset StartDate { get; init; }

    public required DateTimeOffset EndDate { get; init; }

    public required string MahadashaPlanet { get; init; }

    public string? AntardashaPlanet { get; init; }

    public string? PratyantardashaPlanet { get; init; }

    public string ActivationLevel { get; init; } = string.Empty;

    public IReadOnlyList<string> TriggeredBy { get; init; }
        = Array.Empty<string>();
}