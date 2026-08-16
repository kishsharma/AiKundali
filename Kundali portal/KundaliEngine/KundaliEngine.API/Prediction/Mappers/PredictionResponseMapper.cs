using HoraLife.PredictionEngine.Models;
using KundaliEngine.API.Prediction.DTOs;

namespace KundaliEngine.API.Prediction.Mappers;

internal static class PredictionResponseMapper
{
    public static PredictionResponseDto Map(
        PredictionResult predictionResult,
        DashaTimeline dashaTimeline)
    {
        return new PredictionResponseDto
        {
            LifeArea = predictionResult.LifeArea.ToString(),
            TimeScope = predictionResult.TimeScope.ToString(),
            ReferenceDate = predictionResult.ReferenceDate,
            FocusKey = predictionResult.FocusKey,

            Synthesis = predictionResult.Synthesis is null
                ? null
                : new PredictionSynthesisResponseDto
                {
                    OverallTone = predictionResult.Synthesis.OverallTone?.ToString(),
                    EffectiveFrom = predictionResult.Synthesis.EffectiveFrom,
                    EffectiveTo = predictionResult.Synthesis.EffectiveTo,
                    HouseConnections = predictionResult.Synthesis.HouseConnections
                        .Select(connection =>
                            new PredictionHouseConnectionResponseDto
                            {
                                SourceHouse = connection.SourceHouse,
                                LordPlanet = connection.LordPlanet,
                                TargetHouse = connection.TargetHouse,
                                SourceSignificationKeys = connection.SourceSignificationKeys,
                                TargetSignificationKeys = connection.TargetSignificationKeys,
                                EvidenceKey = connection.EvidenceKey
                            })
                        .ToList()
                },

            Claims = predictionResult.Claims
                .Select(claim =>
                {
                    var dashaPeriod = ResolveClaimDashaPeriod(claim.EvidenceKeys, dashaTimeline);

                    return new PredictionClaimResponseDto
                    {
                        Key = claim.Key,
                        Tone = claim.Tone.ToString(),
                        Stage = claim.Stage.ToString(),
                        DashaLevel = dashaPeriod?.Level.ToString(),
                        DashaPlanet = dashaPeriod?.Planet,
                        StartDate = claim.StartDate,
                        EndDate = claim.EndDate,
                        RuleIds = claim.RuleIds,
                        EvidenceKeys = claim.EvidenceKeys
                    };
                })
                .ToList(),

            Answer = predictionResult.Answer is null
                ? null
                : new PredictionAnswerResponseDto
                {
                    Outcome = predictionResult.Answer.Outcome,
                    ReasonKeys = predictionResult.Answer.ReasonKeys,
                    EvidenceKeys = predictionResult.Answer.EvidenceKeys,
                    TimingWindows = predictionResult.Answer.TimingWindows
                        .Select(window =>
                            new PredictionTimingWindowResponseDto
                            {
                                StartDate = window.StartDate,
                                EndDate = window.EndDate,
                                Strength = window.Strength,
                                TriggerPlanets = window.TriggerPlanets,
                                EvidenceKeys = window.EvidenceKeys
                            })
                        .ToList()
                }
        };
    }

    private static DashaPeriod? ResolveClaimDashaPeriod(
        IReadOnlyList<string> evidenceKeys,
        DashaTimeline dashaTimeline)
    {
        if (evidenceKeys.Count == 0 || dashaTimeline.Periods.Count == 0)
        {
            return null;
        }

        var periodsById = dashaTimeline.Periods
            .ToDictionary(period => period.PeriodId, StringComparer.OrdinalIgnoreCase);

        foreach (var evidenceKey in evidenceKeys)
        {
            const string prefix = "dasha:";
            if (!evidenceKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var periodId = evidenceKey[prefix.Length..];
            if (periodsById.TryGetValue(periodId, out var period))
            {
                return period;
            }
        }

        return null;
    }
}
