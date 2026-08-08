using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Models;

namespace HoraLife.PredictionEngine.Service.Career;

/// <summary>
/// Resolves the directional Career influence of a selected Dasha lord
/// from its existing normalized planet evidence.
/// </summary>
/// <remarks>
/// CURRENT DASHA-LORD RULES
///
/// 1. Benefic aspect only:
///    Resolves as Supportive.
///
/// 2. Malefic aspect only:
///    Resolves as Challenging.
///
/// 3. Both benefic and malefic aspects:
///    Resolves as Mixed.
///
/// 4. No relevant aspect:
///    Returns null because directional evidence is insufficient.
///
/// These are HoraLife deterministic engine rules.
/// They are not direct classical scripture references.
/// </remarks>
internal sealed class CareerDashaToneResolverService
{
    public PredictionTone? Resolve(PlanetPredictionData planet)
    {
        ArgumentNullException.ThrowIfNull(planet);

        if (planet.HasBeneficAspect &&
            planet.HasMaleficAspect)
        {
            return PredictionTone.Mixed;
        }

        if (planet.HasBeneficAspect)
        {
            return PredictionTone.Supportive;
        }

        if (planet.HasMaleficAspect)
        {
            return PredictionTone.Challenging;
        }

        return null;
    }
}