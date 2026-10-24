using System.Text.RegularExpressions;

namespace KundaliEngine.API.Services.Auth;

public static class RegistrationEmailRules
{
    /// <summary>Astrologer demo account. It is allowed even when the address would fail format checks.</summary>
    public const string AstrologerDemoEmail = "sharma@admin.com";

    private static readonly Regex EmailFormat = new(
        @"^[a-z0-9](?:[a-z0-9._%+\-]{0,62}[a-z0-9])?@[a-z0-9](?:[a-z0-9\-]{0,61}[a-z0-9])?(?:\.[a-z0-9](?:[a-z0-9\-]{0,61}[a-z0-9])?)+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static bool IsAllowed(string? email)
    {
        var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Length == 0)
        {
            return false;
        }

        return normalized == AstrologerDemoEmail || EmailFormat.IsMatch(normalized);
    }
}
