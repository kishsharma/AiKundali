using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models.Muhurtha;

namespace KundaliEngine.API.Services.Muhurtha;

public class MuhurthaScoringService : IMuhurthaScoringService
{
    public MuhurthaScoreBreakdown Calculate(IReadOnlyList<MuhurthaRuleResult> ruleResults)
    {
        var hardRejected = ruleResults.Any(result => result.HardRejected);
        var score = Math.Clamp(50 + ruleResults.Sum(result => result.ScoreImpact), 0, 100);

        return new MuhurthaScoreBreakdown
        {
            GenericTimeRulesScore = GroupScore(ruleResults, MuhurthaRuleGroup.GenericTimeRules),
            PanchangaRulesScore = GroupScore(ruleResults, MuhurthaRuleGroup.PanchangaRules),
            PersonalHoroscopeRulesScore = GroupScore(ruleResults, MuhurthaRuleGroup.PersonalHoroscopeRules),
            MuhurthaChartRulesScore = GroupScore(ruleResults, MuhurthaRuleGroup.MuhurthaChartRules),
            TotalScore = hardRejected ? 0 : score,
            HardRejected = hardRejected
        };
    }

    private static int GroupScore(
        IReadOnlyList<MuhurthaRuleResult> ruleResults,
        MuhurthaRuleGroup group)
    {
        var groupKey = group.ToString();
        return ruleResults
            .Where(result => result.Metadata.TryGetValue("group", out var value)
                && string.Equals(value, groupKey, StringComparison.OrdinalIgnoreCase))
            .Sum(result => result.ScoreImpact);
    }
}
