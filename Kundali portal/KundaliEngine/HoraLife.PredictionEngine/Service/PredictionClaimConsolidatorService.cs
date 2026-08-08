using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Models;

namespace HoraLife.PredictionEngine.Services;

/// <summary>
/// Consolidates life-area source claims into one deterministic conclusion.
/// </summary>
internal sealed class PredictionClaimConsolidatorService
{
    public PredictionClaim? Consolidate(     PredictionRequest request,   IReadOnlyList<PredictionClaim> claims)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(claims);

        if (request.TimeScope != PredictionTimeScope.Current)
        {
            return null;
        }

        var sourceClaims = claims
            .Where(claim =>
                claim.LifeArea == request.LifeArea &&
                claim.Stage != PredictionClaimStage.Consolidated)
            .ToArray();

        if (sourceClaims.Length == 0)
        {
            return null;
        }

        var tone = ResolveTone(sourceClaims);

        if (tone is null)
        {
            return null;
        }

        var timedClaims = sourceClaims
            .Where(claim =>
                claim.Stage == PredictionClaimStage.Dasha &&
                claim.StartDate is not null &&
                claim.EndDate is not null)
            .ToArray();

        DateTimeOffset? startDate = timedClaims.Length == 0
            ? null
            : timedClaims.Max(claim => claim.StartDate!.Value);

        DateTimeOffset? endDate = timedClaims.Length == 0
            ? null
            : timedClaims.Min(claim => claim.EndDate!.Value);

        if (startDate >= endDate)
        {
            startDate = null;
            endDate = null;
        }

        var areaKey = request.LifeArea
            .ToString()
            .ToLowerInvariant();

        return new PredictionClaim
        {
            Key = $"{areaKey}_current_outlook_{ResolveToneKey(tone.Value)}",
            LifeArea = request.LifeArea,
            FocusKey = request.FocusKey,
            Stage = PredictionClaimStage.Consolidated,
            Tone = tone.Value,
            StartDate = startDate,
            EndDate = endDate,

            RuleIds =
            [
                $"{areaKey}.current.consolidation",

                .. sourceClaims
                    .SelectMany(claim => claim.RuleIds)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
            ],

            EvidenceKeys =
            [
                .. sourceClaims.Select(claim =>
                    $"claim:{claim.Stage.ToString().ToLowerInvariant()}:{claim.Key}"),

                .. sourceClaims
                    .SelectMany(claim => claim.EvidenceKeys)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
            ]
        };
    }

    private static PredictionTone? ResolveTone(
        IReadOnlyList<PredictionClaim> claims)
    {
        if (claims.Any(claim =>
                claim.Tone == PredictionTone.Mixed))
        {
            return PredictionTone.Mixed;
        }

        var hasSupportive = claims.Any(claim =>
            claim.Tone == PredictionTone.Supportive);

        var hasChallenging = claims.Any(claim =>
            claim.Tone == PredictionTone.Challenging);

        if (hasSupportive && hasChallenging)
        {
            return PredictionTone.Mixed;
        }

        if (hasSupportive)
        {
            return PredictionTone.Supportive;
        }

        if (hasChallenging)
        {
            return PredictionTone.Challenging;
        }

        return null;
    }

    private static string ResolveToneKey(PredictionTone tone)
    {
        return tone switch
        {
            PredictionTone.Supportive => "support",
            PredictionTone.Challenging => "pressure",
            PredictionTone.Mixed => "mixed",

            _ => throw new InvalidOperationException(
                $"Unsupported consolidation tone: {tone}.")
        };
    }
}