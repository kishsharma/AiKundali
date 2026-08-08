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
    public PredictionEvidenceSet Select( PredictionContext context,  PredictionAreaProfile profile, IReadOnlyList<DashaPeriod> dashaPeriods)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(dashaPeriods);

        var primaryHouseNumbers = profile.PrimaryHouses.ToHashSet();
        var supportingHouseNumbers = profile.SupportingHouses.ToHashSet();

        var significatorNames = profile.NaturalSignificators.ToHashSet(
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
            PlanetAspects = planetAspects
        };
    }
}