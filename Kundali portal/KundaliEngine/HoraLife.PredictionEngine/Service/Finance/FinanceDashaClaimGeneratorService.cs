using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Models;

namespace HoraLife.PredictionEngine.Services.Finance;

internal sealed class FinanceDashaClaimGeneratorService
{
    public IReadOnlyList<PredictionClaim> Generate(
        PredictionRequest request,
        PredictionEvidenceSet evidence)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(evidence);

        if (request.LifeArea != PredictionLifeArea.Finance)
        {
            throw new ArgumentException(
                "This service supports only Finance predictions.",
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

            var tone = ResolveTone(planet);

            if (tone is null)
            {
                continue;
            }

            claims.Add(new PredictionClaim
            {
                Key = ResolveClaimKey(tone.Value),
                LifeArea = PredictionLifeArea.Finance,
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

    private static PredictionTone? ResolveTone(PlanetPredictionData planet)
    {
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

    private static string ResolveClaimKey(PredictionTone tone)
    {
        return tone switch
        {
            PredictionTone.Supportive =>
                "finance_dasha_support",

            PredictionTone.Challenging =>
                "finance_dasha_pressure",

            PredictionTone.Mixed =>
                "finance_dasha_mixed",

            _ => throw new InvalidOperationException(
                $"Unsupported Finance Dasha tone: {tone}.")
        };
    }

    private static IReadOnlyList<string> ResolveRuleIds(
        PlanetPredictionData planet)
    {
        var ruleIds = new List<string>();

        if (planet.HasBeneficAspect)
        {
            ruleIds.Add(
                "finance.dasha_lord.benefic_aspect");
        }

        if (planet.HasMaleficAspect)
        {
            ruleIds.Add(
                "finance.dasha_lord.malefic_aspect");
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
