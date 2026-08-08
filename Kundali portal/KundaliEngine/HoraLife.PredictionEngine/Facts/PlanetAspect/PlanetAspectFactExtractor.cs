using HoraLife.PredictionEngine.Models;

namespace HoraLife.PredictionEngine.Facts.PlanetAspect;

internal sealed class PlanetAspectFactExtractor
{
    public IReadOnlyList<PlanetAspectFact> Extract(
        PredictionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Planets
            .Where(planet =>
                planet.HasBeneficAspect ||
                planet.HasMaleficAspect)
            .Select(planet =>
                new PlanetAspectFact
                {
                    Planet = planet.Planet,
                    HasBeneficAspect = planet.HasBeneficAspect,
                    HasMaleficAspect = planet.HasMaleficAspect,
                    AspectedBy = planet.AspectedBy,

                    // Evidence key format:
                    // planet:{PlanetName}:aspect
                    EvidenceKey =
                        $"planet:{planet.Planet}:aspect"
                })
            .ToArray();
    }
}