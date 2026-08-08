using HoraLife.PredictionEngine.Enums;

namespace HoraLife.PredictionEngine.Models;

public sealed record PredictionSynthesis
{
    public PredictionTone? OverallTone { get; init; }

    public DateTimeOffset? EffectiveFrom { get; init; }

    public DateTimeOffset? EffectiveTo { get; init; }

    public IReadOnlyList<PredictionHouseConnection> HouseConnections { get; init; }
        = Array.Empty<PredictionHouseConnection>();
}

public sealed record PredictionHouseConnection
{
    public required int SourceHouse { get; init; }

    public required string LordPlanet { get; init; }

    public required int TargetHouse { get; init; }

    public IReadOnlyList<string> SourceSignificationKeys { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> TargetSignificationKeys { get; init; }
        = Array.Empty<string>();

    public required string EvidenceKey { get; init; }
}