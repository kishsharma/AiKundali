using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models.Muhurtha;

public class MuhurthaRuleConfiguration
{
    public string RuleKey { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public MuhurthaRuleSeverity Severity { get; set; } = MuhurthaRuleSeverity.Neutral;

    public int ScoreImpact { get; set; }

    public bool HardReject { get; set; }

    public List<MuhurthaEventType> ApplicableEventTypes { get; set; } = new();

    public List<MuhurthaEventType> ExcludedEventTypes { get; set; } = new();

    public List<MuhurthaRegionalProfileType> RegionalProfiles { get; set; } = new();
}
