using KundaliEngine.Domain.Models.Muhurtha;

namespace KundaliEngine.API.Services.Muhurtha;

public interface IMuhurthaScoringService
{
    MuhurthaScoreBreakdown Calculate(IReadOnlyList<MuhurthaRuleResult> ruleResults);
}
