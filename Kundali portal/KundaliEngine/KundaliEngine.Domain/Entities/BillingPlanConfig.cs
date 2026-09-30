namespace KundaliEngine.Domain.Entities;

public class BillingPlanConfig
{
    public string Code { get; set; } = "Free";

    public string Name { get; set; } = "Free";

    public string Tagline { get; set; } = string.Empty;

    public int MonthlyAmountPaise { get; set; }

    public int YearlyAmountPaise { get; set; }

    public int KundaliSaveLimit { get; set; }

    public int MatchSaveLimit { get; set; }

    public bool AiChatEnabled { get; set; }

    public int? LifeQuestionLimit { get; set; }

    public bool CheckoutOpen { get; set; }

    public string? RazorpayMonthlyPlanId { get; set; }

    public string? RazorpayYearlyPlanId { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
