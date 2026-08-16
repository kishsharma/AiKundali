using HoraLife.PredictionEngine.Facts.HouseAspect;
using HoraLife.PredictionEngine.Facts.HouseLordAspect;
using HoraLife.PredictionEngine.Facts.PlanetAspect;

namespace HoraLife.PredictionEngine.Models;

internal sealed record PredictionEvidenceSet
{
    public IReadOnlyList<HousePredictionData> PrimaryHouses { get; init; }
        = Array.Empty<HousePredictionData>();

    public IReadOnlyList<PlanetPredictionData> PrimaryHouseLords { get; init; }
        = Array.Empty<PlanetPredictionData>();

    public IReadOnlyList<HousePredictionData> SupportingHouses { get; init; }
        = Array.Empty<HousePredictionData>();

    public IReadOnlyList<PlanetPredictionData> NaturalSignificators { get; init; }
        = Array.Empty<PlanetPredictionData>();

    public IReadOnlyList<PlanetPredictionData> DashaLordPlanets { get; init; }
        = Array.Empty<PlanetPredictionData>();

    public IReadOnlyList<DashaPeriod> DashaPeriods { get; init; }
        = Array.Empty<DashaPeriod>();

    public IReadOnlyList<YogaPredictionData> Yogas { get; init; }
        = Array.Empty<YogaPredictionData>();

    public IReadOnlyList<DoshaPredictionData> Doshas { get; init; }
        = Array.Empty<DoshaPredictionData>();

    public IReadOnlyList<HouseLordPlacementFact> HouseLordPlacements { get; init; }
        = Array.Empty<HouseLordPlacementFact>();

    public IReadOnlyList<HouseAspectFact> HouseAspects { get; init; }
        = Array.Empty<HouseAspectFact>();

    public IReadOnlyList<PlanetAspectFact> PlanetAspects { get; init; }
        = Array.Empty<PlanetAspectFact>();

    public IReadOnlyList<DivisionalChartFocusEvidence> DivisionalFocusEvidence { get; init; }
        = Array.Empty<DivisionalChartFocusEvidence>();
}
