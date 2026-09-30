using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Facts.HouseAspect;
using HoraLife.PredictionEngine.Facts.HouseLordAspect;
using HoraLife.PredictionEngine.Facts.PlanetAspect;
using HoraLife.PredictionEngine.Models;

namespace HoraLife.PredictionEngine.Services;

internal sealed class PredictionEvidenceSelectorService
{
    private readonly HouseLordPlacementFactExtractor _houseLordPlacementExtractor = new();
    private readonly HouseAspectFactExtractor _houseAspectExtractor = new();
    private readonly PlanetAspectFactExtractor _planetAspectExtractor = new();

    public PredictionEvidenceSet Select(
        PredictionContext context,
        PredictionRequest request,
        PredictionAreaProfile profile,
        IReadOnlyList<DashaPeriod> dashaPeriods)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(dashaPeriods);

        var focus = request.Mode == PredictionMode.CustomQuestion
            ? LifeQuestionCatalog.Find(request.LifeArea, request.FocusKey)
            : null;

        var primaryHouseNumbers = (focus?.PrimaryHouses ?? profile.PrimaryHouses).ToHashSet();
        var supportingHouseNumbers = profile.SupportingHouses.ToHashSet();

        var significatorNames = (focus?.Significators ?? profile.NaturalSignificators).ToHashSet(
            StringComparer.OrdinalIgnoreCase);

        var primaryHouses = context.Houses
            .Where(house => primaryHouseNumbers.Contains(house.HouseNumber))
            .OrderBy(house => house.HouseNumber)
            .ToArray();

        var primaryHouseLordNames = primaryHouses
            .Select(house => house.HouseLord)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var primaryHouseLords = context.Planets
            .Where(planet => primaryHouseLordNames.Contains(planet.Planet))
            .ToArray();

        var supportingHouses = context.Houses
            .Where(house => supportingHouseNumbers.Contains(house.HouseNumber))
            .OrderBy(house => house.HouseNumber)
            .ToArray();

        var naturalSignificators = context.Planets
            .Where(planet => significatorNames.Contains(planet.Planet))
            .ToArray();

        var dashaLordNames = dashaPeriods
            .Select(period => period.Planet)
            .Where(planet => !string.IsNullOrWhiteSpace(planet))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var dashaLordPlanets = context.Planets
            .Where(planet => dashaLordNames.Contains(planet.Planet))
            .GroupBy(planet => planet.Planet, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();

        var relevantYogas = context.Yogas
            .Where(yoga => yoga.Impacts.Any(impact => impact.LifeArea == profile.LifeArea))
            .ToArray();

        var relevantDoshas = context.Doshas
            .Where(dosha => dosha.Impacts.Any(impact => impact.LifeArea == profile.LifeArea))
            .ToArray();

        var houseLordPlacements = _houseLordPlacementExtractor.Extract(context);
        var houseAspects = _houseAspectExtractor.Extract(context);
        var planetAspects = _planetAspectExtractor.Extract(context);
        var divisionalFocusEvidence = ResolveDivisionalFocusEvidence(context, request);

        return new PredictionEvidenceSet
        {
            PrimaryHouses = primaryHouses,
            SupportingHouses = supportingHouses,
            NaturalSignificators = naturalSignificators,
            DashaLordPlanets = dashaLordPlanets,
            DashaPeriods = dashaPeriods.ToArray(),
            Yogas = relevantYogas,
            Doshas = relevantDoshas,
            PrimaryHouseLords = primaryHouseLords,
            HouseLordPlacements = houseLordPlacements,
            HouseAspects = houseAspects,
            PlanetAspects = planetAspects,
            DivisionalFocusEvidence = divisionalFocusEvidence
        };
    }

    private static IReadOnlyList<DivisionalChartFocusEvidence> ResolveDivisionalFocusEvidence(
        PredictionContext context,
        PredictionRequest request)
    {
        var metadata = ResolveDivisionalFocusMetadata(request);
        if (metadata.Count == 0 || context.DivisionalCharts.Count == 0)
        {
            return Array.Empty<DivisionalChartFocusEvidence>();
        }

        var chartsByType = context.DivisionalCharts
            .GroupBy(chart => chart.ChartType, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var resolved = new List<DivisionalChartFocusEvidence>();

        foreach (var focus in metadata)
        {
            if (!chartsByType.TryGetValue(focus.ChartType, out var chart))
            {
                continue;
            }

            var houseEvidence = focus.HouseNumbers
                .Select(houseNumber => ResolveDivisionalHouseFocusEvidence(chart, houseNumber))
                .Where(house => house is not null)
                .Cast<DivisionalHouseFocusEvidence>()
                .ToArray();

            var naturalSignificators = chart.Planets
                .Where(planet => focus.SignificatorPlanets.Contains(planet.Planet, StringComparer.OrdinalIgnoreCase))
                .ToArray();

            resolved.Add(new DivisionalChartFocusEvidence
            {
                ChartType = chart.ChartType,
                Houses = houseEvidence,
                NaturalSignificators = naturalSignificators,
                NaturalSignificatorEvidenceKeys = naturalSignificators
                    .Select(planet => $"divisional:{chart.ChartType}:planet:{planet.Planet}:house:{planet.HouseNumber}")
                    .ToArray()
            });
        }

        return resolved;
    }

    private static IReadOnlyList<DivisionalQuestionFocusMetadata> ResolveDivisionalFocusMetadata(
        PredictionRequest request)
    {
        if (request.Mode != PredictionMode.CustomQuestion ||
            string.IsNullOrWhiteSpace(request.FocusKey))
        {
            return Array.Empty<DivisionalQuestionFocusMetadata>();
        }

        var focus = LifeQuestionCatalog.Find(request.LifeArea, request.FocusKey);
        if (focus is null || string.IsNullOrWhiteSpace(focus.DivisionalChart) || focus.DivisionalHouses.Length == 0)
        {
            return Array.Empty<DivisionalQuestionFocusMetadata>();
        }

        return
        [
            new DivisionalQuestionFocusMetadata
            {
                ChartType = focus.DivisionalChart,
                HouseNumbers = focus.DivisionalHouses,
                SignificatorPlanets = focus.DivisionalSignificators
            }
        ];
    }

    private static DivisionalHouseFocusEvidence? ResolveDivisionalHouseFocusEvidence(
        DivisionalChartPredictionData chart,
        int houseNumber)
    {
        var house = chart.Houses.FirstOrDefault(candidate => candidate.HouseNumber == houseNumber);
        if (house is null)
        {
            return null;
        }

        var houseLordPlanet = chart.Planets.FirstOrDefault(planet =>
            planet.Planet.Equals(house.HouseLord, StringComparison.OrdinalIgnoreCase));

        var occupants = chart.Planets
            .Where(planet =>
                planet.HouseNumber == houseNumber ||
                house.Occupants.Contains(planet.Planet, StringComparer.OrdinalIgnoreCase))
            .GroupBy(planet => planet.Planet, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();

        return new DivisionalHouseFocusEvidence
        {
            HouseNumber = house.HouseNumber,
            Sign = house.Sign,
            HouseLord = house.HouseLord,
            HouseLordPlanet = houseLordPlanet,
            Occupants = occupants,
            HouseEvidenceKey = $"divisional:{chart.ChartType}:house:{house.HouseNumber}",
            HouseLordEvidenceKey = $"divisional:{chart.ChartType}:house:{house.HouseNumber}:lord:{house.HouseLord}",
            OccupantEvidenceKeys = occupants
                .Select(planet => $"divisional:{chart.ChartType}:house:{house.HouseNumber}:occupant:{planet.Planet}")
                .ToArray()
        };
    }
}
