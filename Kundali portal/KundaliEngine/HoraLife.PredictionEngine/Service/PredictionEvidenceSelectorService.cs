using HoraLife.PredictionEngine.Models;

namespace HoraLife.PredictionEngine.Services;

internal sealed class PredictionEvidenceSelectorService
{
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

        var supportingHouses = context.Houses
            .Where(house => supportingHouseNumbers.Contains(house.HouseNumber))
            .OrderBy(house => house.HouseNumber)
            .ToArray();

        var naturalSignificators = context.Planets
            .Where(planet => significatorNames.Contains(planet.Planet))
            .ToArray();

        var relevantYogas = context.Yogas
            .Where(yoga => yoga.Impacts.Any(
                impact => impact.LifeArea == profile.LifeArea))
            .ToArray();

        var relevantDoshas = context.Doshas
            .Where(dosha => dosha.Impacts.Any(
                impact => impact.LifeArea == profile.LifeArea))
            .ToArray();

        return new PredictionEvidenceSet
        {
            PrimaryHouses = primaryHouses,
            SupportingHouses = supportingHouses,
            NaturalSignificators = naturalSignificators,
            DashaPeriods = dashaPeriods.ToArray(),
            Yogas = relevantYogas,
            Doshas = relevantDoshas
        };
    }
}