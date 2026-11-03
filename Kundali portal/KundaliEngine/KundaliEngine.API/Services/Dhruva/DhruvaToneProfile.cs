namespace KundaliEngine.API.Services.Dhruva;

public sealed class DhruvaToneProfile
{

    public string Tone { get; init; } = "friendly";

    public int EvidenceLimit { get; init; } = 4;

    public int MaxCompletionTokens { get; init; } = 400;

    public string PromptLine { get; init; } = string.Empty;

    public static string Normalize(string? tone) => tone?.Trim().ToLowerInvariant() switch
    {
        "astrologer" => "astrologer",
        "corporate" => "corporate",
        _ => "friendly",
    };
}
