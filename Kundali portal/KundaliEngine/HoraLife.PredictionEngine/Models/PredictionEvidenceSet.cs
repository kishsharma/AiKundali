namespace HoraLife.PredictionEngine.Models;

internal sealed record PredictionEvidenceSet
{
    public IReadOnlyList<HousePredictionData> PrimaryHouses { get; init; }
        = Array.Empty<HousePredictionData>();

    public IReadOnlyList<HousePredictionData> SupportingHouses { get; init; }
        = Array.Empty<HousePredictionData>();

    public IReadOnlyList<PlanetPredictionData> NaturalSignificators { get; init; }
        = Array.Empty<PlanetPredictionData>();

    public IReadOnlyList<DashaPeriod> DashaPeriods { get; init; }
        = Array.Empty<DashaPeriod>();

    public IReadOnlyList<YogaPredictionData> Yogas { get; init; }
        = Array.Empty<YogaPredictionData>();

    public IReadOnlyList<DoshaPredictionData> Doshas { get; init; }
        = Array.Empty<DoshaPredictionData>();
}