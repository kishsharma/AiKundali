using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models.Muhurtha;

namespace KundaliEngine.API.Services.Muhurtha;

public class MuhurthaRecommendationService : IMuhurthaRecommendationService
{
    private readonly IMuhurthaScoringService _scoringService;

    public MuhurthaRecommendationService(IMuhurthaScoringService scoringService)
    {
        _scoringService = scoringService;
    }

    public IReadOnlyList<MuhurthaRecommendation> RankAndMerge(
        IReadOnlyList<MuhurthaCandidateSlot> candidates)
    {
        return candidates
            .Where(candidate => candidate.Status == MuhurthaCandidateStatus.Valid)
            .Select(BuildRecommendation)
            .OrderBy(recommendation => recommendation.HardRejected)
            .ThenByDescending(recommendation => recommendation.Score)
            .ThenBy(recommendation => recommendation.StartTime)
            .ToList();
    }

    private MuhurthaRecommendation BuildRecommendation(MuhurthaCandidateSlot candidate)
    {
        var scoreBreakdown = _scoringService.Calculate(candidate.RuleResults);
        candidate.Score = scoreBreakdown.TotalScore;
        candidate.HardRejected = scoreBreakdown.HardRejected;
        candidate.Status = scoreBreakdown.HardRejected
            ? MuhurthaCandidateStatus.HardRejected
            : MuhurthaCandidateStatus.Valid;

        return new MuhurthaRecommendation
        {
            StartTime = candidate.StartTime,
            EndTime = candidate.EndTime,
            Score = scoreBreakdown.TotalScore,
            Verdict = GetVerdict(scoreBreakdown),
            HardRejected = scoreBreakdown.HardRejected,
            Strengths = BuildStrengths(candidate.RuleResults),
            Warnings = BuildWarnings(candidate.RuleResults),
            RuleResults = candidate.RuleResults.ToList(),
            ScoreBreakdown = scoreBreakdown
        };
    }

    private static MuhurthaVerdict GetVerdict(MuhurthaScoreBreakdown scoreBreakdown)
    {
        if (scoreBreakdown.HardRejected)
        {
            return MuhurthaVerdict.HardBlocked;
        }

        return scoreBreakdown.TotalScore switch
        {
            >= 85 => MuhurthaVerdict.ExcellentMuhurtha,
            >= 70 => MuhurthaVerdict.HighlyFavourable,
            >= 55 => MuhurthaVerdict.AcceptableWithMinorCautions,
            >= 40 => MuhurthaVerdict.WeakMuhurtha,
            _ => MuhurthaVerdict.NotRecommended
        };
    }

    private static List<string> BuildStrengths(IReadOnlyList<MuhurthaRuleResult> ruleResults)
    {
        var positives = ruleResults
            .Where(result => result.Severity == MuhurthaRuleSeverity.Positive)
            .Select(result => result.Summary)
            .Where(summary => !string.IsNullOrWhiteSpace(summary))
            .Distinct()
            .ToList();

        if (positives.Count == 0)
        {
            positives.Add("No major positive rule has been identified for this slot yet.");
        }

        return positives;
    }

    private static List<string> BuildWarnings(IReadOnlyList<MuhurthaRuleResult> ruleResults)
    {
        return ruleResults
            .Where(result => result.Status is MuhurthaRuleStatus.Warning or MuhurthaRuleStatus.Skipped || result.HardRejected)
            .Select(result => result.Summary)
            .Where(summary => !string.IsNullOrWhiteSpace(summary))
            .Distinct()
            .ToList();
    }
}
