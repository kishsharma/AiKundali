using KundaliEngine.Domain.Models.Muhurtha;

namespace KundaliEngine.API.Services.Muhurtha.GrahaPravesha;

public sealed class VastuTableMatch
{
    public bool Matched { get; init; }

    public string Applicability { get; init; } = string.Empty;

    public string Ahuti { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;
}

public static class VastuTableMatcher
{
    public static VastuTableMatch Match(
        IEnumerable<MuhurthaVastuEligibilityRuleConfiguration> rules,
        string paksha,
        string tithi,
        string weekday,
        string moonSign)
    {
        var match = rules.FirstOrDefault(rule =>
            rule.Enabled
            && Equals(rule.Paksha, paksha)
            && Equals(rule.Tithi, tithi)
            && rule.Weekdays.Any(day => Equals(day, weekday))
            && rule.MoonSigns.Any(sign => Equals(sign, moonSign)));

        if (match is null)
        {
            return new VastuTableMatch();
        }

        return new VastuTableMatch
        {
            Matched = true,
            Applicability = string.Join("-", match.Applicability),
            Ahuti = match.Ahuti,
            Source = string.IsNullOrWhiteSpace(match.Source) ? "VastuEligibilityRules" : match.Source
        };
    }

    private static bool Equals(string expected, string actual)
    {
        return string.Equals(Normalize(expected), Normalize(actual), StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string value)
    {
        return value.Trim().Replace(" ", string.Empty, StringComparison.Ordinal);
    }
}
