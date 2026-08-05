namespace HoraLife.PredictionEngine.Models;

public sealed record PredictionContext
{
    public string SchemaVersion { get; init; } = "1.0";

    public required IReadOnlyList<HousePredictionData> Houses { get; init; }

    public required IReadOnlyList<PlanetPredictionData> Planets { get; init; }

    public required DashaTimeline DashaTimeline { get; init; }

    public IReadOnlyList<YogaPredictionData> Yogas { get; init; }
        = Array.Empty<YogaPredictionData>();

    public IReadOnlyList<DoshaPredictionData> Doshas { get; init; }
        = Array.Empty<DoshaPredictionData>();
}