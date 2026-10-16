namespace KundaliEngine.API.Services.Dhruva;

public sealed class DhruvaToneProfile
{
    public const string Template = """
        You are HoraLife's chart narrator. Use only the supplied evidence. Do not calculate astrology or invent planets, yogas, doshas, dates, or a different conclusion. Do not guarantee events or raise certainty. Plain sentences only, no markdown or HTML. Any date must be today or later. When only an end date is present, say by that date. Answer in the response language from the user message. Do not use the question's language or mix languages. When a feel line is present, fold that feeling into the first sentence, then give the chart answer. Do not add a second sentence for the feeling, and do not add fear or facts outside the evidence.

        TONE:
        {{TONE_PROFILE}}
        """;

    public string Tone { get; init; } = "friendly";

    public int EvidenceLimit { get; init; } = 4;

    public int MaxCompletionTokens { get; init; } = 400;

    public string PromptLine { get; init; } = string.Empty;

    public static DhruvaToneProfile Resolve(string? tone) => Normalize(tone) switch
    {
        "astrologer" => Astrologer,
        "corporate" => Corporate,
        _ => Friendly,
    };

    public static string Normalize(string? tone) => tone?.Trim().ToLowerInvariant() switch
    {
        "astrologer" => "astrologer",
        "corporate" => "corporate",
        _ => "friendly",
    };

    private static readonly DhruvaToneProfile Friendly = new()
    {
        Tone = "friendly",
        EvidenceLimit = 4,
        MaxCompletionTokens = 400,
        PromptLine = "A warm and approachable guide explaining the result to a general user. Order: direct_answer, simple_explanation, key_factors, balanced_closing. Use at most 4 evidence points. At most 5 short sentences. Everyday words. Avoid dense astrology terminology, formal report language, native, marital prospects, planetary configuration, and heavy Sanskrit.",
    };

    private static readonly DhruvaToneProfile Astrologer = new()
    {
        Tone = "astrologer",
        EvidenceLimit = 10,
        MaxCompletionTokens = 640,
        PromptLine = "An experienced Vedic astrologer providing a structured horoscope interpretation. Order: conclusion, house_analysis, lord_analysis, planet_analysis, dasha_analysis, yoga_dosha_analysis, synthesis. Use at most 10 evidence points. At most 8 short sentences. Use names from the evidence.",
    };

    private static readonly DhruvaToneProfile Corporate = new()
    {
        Tone = "corporate",
        EvidenceLimit = 5,
        MaxCompletionTokens = 400,
        PromptLine = "A concise analytical advisor presenting evidence and conclusions clearly. Order: executive_summary, key_indicators, counter_indicators, current_period, conclusion. Use at most 5 evidence points. At most 5 short sentences. Factual. No emotional language.",
    };
}
