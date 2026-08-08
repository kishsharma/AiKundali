using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Models;
using HoraLife.PredictionEngine.Service.Career;

namespace HoraLife.PredictionEngine.Services.Career;

/// <summary>
/// Generates deterministic Career claims for selected Dasha periods.
/// </summary>
/// <remarks>
/// A Dasha claim is produced only when:
/// - the selected period's lord exists in DashaLordPlanets; and
/// - the approved tone resolver finds directional evidence.
///
/// The claim inherits exact timing from the selected Dasha period.
/// </remarks>
internal sealed class CareerDashaClaimGeneratorService
{
    private readonly CareerDashaToneResolverService _toneResolver = new();

    public IReadOnlyList<PredictionClaim> Generate(
        PredictionRequest request,
        PredictionEvidenceSet evidence)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(evidence);

        if (request.LifeArea != PredictionLifeArea.Career)
        {
            throw new ArgumentException(
                "This service supports only Career predictions.",
                nameof(request));
        }

        var planetsByName = evidence.DashaLordPlanets
            .GroupBy(
                planet => planet.Planet,
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.OrdinalIgnoreCase);

        var claims = new List<PredictionClaim>();

        foreach (var period in evidence.DashaPeriods)
        {
            if (!planetsByName.TryGetValue(
                    period.Planet,
                    out var planet))
            {
                continue;
            }

            var tone = _toneResolver.Resolve(planet);

            if (tone is null)
            {
                continue;
            }

            claims.Add(new PredictionClaim
            {
                Key = ResolveClaimKey(tone.Value),
                LifeArea = PredictionLifeArea.Career,
                FocusKey = request.FocusKey,
                Stage = PredictionClaimStage.Dasha,
                Tone = tone.Value,
                StartDate = period.StartDate,
                EndDate = period.EndDate,
                RuleIds = ResolveRuleIds(planet),
                EvidenceKeys = ResolveEvidenceKeys(period, planet)
            });
        }

        return claims;
    }

    private static string ResolveClaimKey(PredictionTone tone)
    {
        return tone switch
        {
            PredictionTone.Supportive =>
                "career_dasha_support",

            PredictionTone.Challenging =>
                "career_dasha_pressure",

            PredictionTone.Mixed =>
                "career_dasha_mixed",

            _ => throw new InvalidOperationException(
                $"Unsupported Career Dasha tone: {tone}.")
        };
    }

    private static IReadOnlyList<string> ResolveRuleIds(
        PlanetPredictionData planet)
    {
        var ruleIds = new List<string>();

        if (planet.HasBeneficAspect)
        {
            ruleIds.Add(
                "career.dasha_lord.benefic_aspect");
        }

        if (planet.HasMaleficAspect)
        {
            ruleIds.Add(
                "career.dasha_lord.malefic_aspect");
        }

        return ruleIds;
    }

    private static IReadOnlyList<string> ResolveEvidenceKeys(
        DashaPeriod period,
        PlanetPredictionData planet)
    {
        var evidenceKeys = new List<string>
        {
            $"dasha:{period.PeriodId}",
            $"dasha-level:{period.Level.ToString().ToLowerInvariant()}",
            $"planet:{planet.Planet}"
        };

        if (planet.HasBeneficAspect)
        {
            evidenceKeys.Add(
                $"planet:{planet.Planet}:benefic_aspect");
        }

        if (planet.HasMaleficAspect)
        {
            evidenceKeys.Add(
                $"planet:{planet.Planet}:malefic_aspect");
        }

        return evidenceKeys;
    }
}