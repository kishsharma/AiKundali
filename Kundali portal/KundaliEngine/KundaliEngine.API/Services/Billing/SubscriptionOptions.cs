namespace KundaliEngine.API.Services.Billing;

public sealed class SubscriptionOptions
{
    public string Currency { get; set; } = "INR";

    public List<SubscriptionPlanOption> Plans { get; set; } = [];
}

public sealed class SubscriptionPlanOption
{
    public string Code { get; set; } = "Free";

    public string Name { get; set; } = "Free";

    public string Tagline { get; set; } = string.Empty;

    public int MonthlyAmountPaise { get; set; }

    public int YearlyAmountPaise { get; set; }

    public int KundaliSaveLimit { get; set; } = 5;

    public int MatchSaveLimit { get; set; } = 2;

    public string? RazorpayMonthlyPlanId { get; set; }

    public string? RazorpayYearlyPlanId { get; set; }
}

public sealed class RazorpayOptions
{
    public string KeyId { get; set; } = string.Empty;

    public string KeySecret { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;
}
