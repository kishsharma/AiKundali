using HoraLife.PredictionEngine.Models;

namespace HoraLife.PredictionEngine.Facts.HouseLordAspect;

internal sealed class HouseLordPlacementFactExtractor
{
    public IReadOnlyList<HouseLordPlacementFact> Extract(
        PredictionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var planetsByName = context.Planets
            .ToDictionary(planet => planet.Planet,StringComparer.OrdinalIgnoreCase);

        var facts = new List<HouseLordPlacementFact>();

        foreach (var house in context.Houses)
        {
            if (!planetsByName.TryGetValue(
                    house.HouseLord,
                    out var lordPlanet))
            {
                continue;
            }

            facts.Add(
                new HouseLordPlacementFact
                {
                    SourceHouse = house.HouseNumber,
                    LordPlanet = lordPlanet.Planet,
                    TargetHouse = lordPlanet.HouseNumber,

                    // Evidence key format:
                    // house-lord:{SourceHouse}:{LordPlanet}:house:{TargetHouse}
                    //
                    // Example:
                    // house-lord:10:Mercury:house:11
                    EvidenceKey =
                        $"house-lord:{house.HouseNumber}:{lordPlanet.Planet}:house:{lordPlanet.HouseNumber}"
                });
        }

        return facts;
    }
}