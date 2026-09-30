namespace KundaliEngine.Domain.Subscriptions;

public static class PlanCodes
{
    public const string Free = "Free";
    public const string Premium = "Premium";
    public const string PremiumPlus = "PremiumPlus";

    public static readonly string[] All = [Free, Premium, PremiumPlus];

    public static string Normalize(string? planCode)
    {
        if (string.IsNullOrWhiteSpace(planCode))
        {
            return Free;
        }

        var trimmed = planCode.Trim();
        if (trimmed.Equals(PremiumPlus, StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("premium plus", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("premium_plus", StringComparison.OrdinalIgnoreCase))
        {
            return PremiumPlus;
        }

        if (trimmed.Equals(Premium, StringComparison.OrdinalIgnoreCase))
        {
            return Premium;
        }

        if (trimmed.Equals(Free, StringComparison.OrdinalIgnoreCase))
        {
            return Free;
        }

        return Free;
    }

    public static bool IsPaid(string? planCode)
    {
        var code = Normalize(planCode);
        return code is Premium or PremiumPlus;
    }

    public static bool IsPlus(string? planCode)
    {
        return Normalize(planCode) == PremiumPlus;
    }

    public static bool TryParse(string? planCode, out string normalized)
    {
        if (string.IsNullOrWhiteSpace(planCode))
        {
            normalized = Free;
            return false;
        }

        var trimmed = planCode.Trim();
        if (trimmed.Equals(PremiumPlus, StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("premium plus", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("premium_plus", StringComparison.OrdinalIgnoreCase))
        {
            normalized = PremiumPlus;
            return true;
        }

        if (trimmed.Equals(Premium, StringComparison.OrdinalIgnoreCase))
        {
            normalized = Premium;
            return true;
        }

        if (trimmed.Equals(Free, StringComparison.OrdinalIgnoreCase))
        {
            normalized = Free;
            return true;
        }

        normalized = Free;
        return false;
    }
}
