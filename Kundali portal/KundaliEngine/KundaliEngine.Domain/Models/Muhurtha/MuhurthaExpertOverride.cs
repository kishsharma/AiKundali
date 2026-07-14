namespace KundaliEngine.Domain.Models.Muhurtha;

public class MuhurthaExpertOverride
{
    public string RuleKey { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string ApprovedBy { get; set; } = string.Empty;

    public DateTimeOffset ApprovedAt { get; set; }
}
