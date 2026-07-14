using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models.Muhurtha;

public class MuhurthaRuleDefinition
{
    public string RuleKey { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string SanskritName { get; set; } = string.Empty;

    public MuhurthaRuleGroup Group { get; set; }

    public string RuleType { get; set; } = string.Empty;

    public string CalculationMethod { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Recommendation { get; set; } = string.Empty;

    public bool ExpertOverrideAllowed { get; set; }
}
