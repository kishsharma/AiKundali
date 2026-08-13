namespace KundaliEngine.API.Services.Auth;

public static class PreferredLanguageHelper
{
    public static string? Normalize(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return null;
        }

        var normalized = language.Trim().ToLowerInvariant();
        return normalized is "en" or "hi" or "kn" ? normalized : null;
    }

    public static string NormalizeRequired(string? language)
    {
        return Normalize(language)
            ?? throw new InvalidOperationException("Preferred language must be one of en, hi or kn.");
    }
}
