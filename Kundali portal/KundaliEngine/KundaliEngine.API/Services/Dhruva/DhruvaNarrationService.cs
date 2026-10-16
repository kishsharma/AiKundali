using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using KundaliEngine.API.Services.Auth;
using Microsoft.Extensions.Options;

namespace KundaliEngine.API.Services.Dhruva;

public interface IDhruvaNarrationService
{
    Task<DhruvaNarrateResponseDto> NarrateAsync(DhruvaNarrateRequestDto request, CancellationToken cancellationToken);
}

public sealed class DhruvaNarrationService : IDhruvaNarrationService
{
    public const string Disclaimer = "For educational and spiritual guidance only. Not medical, legal, or financial advice. Outcomes are not guaranteed.";
    public const string PromptVersion = "dhruva-tone-v4";

    private static readonly HashSet<string> Modes = new(StringComparer.OrdinalIgnoreCase)
    {
        "janma_story",
        "dasha_focus",
        "compatibility",
        "qa",
    };

    private static readonly Dictionary<string, string> ModeInstructions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["janma_story"] = "Narrate the birth chart from the supplied evidence. Skip anything absent.",
        ["dasha_focus"] = "Explain the current Mahadasha and Antardasha from the supplied evidence, including the period influence words that are present, then the Mahadasha periods that have not ended. Skip anything absent.",
        ["compatibility"] = "Explain the match from the factors in the supplied evidence. Do not invent a match score. Skip anything absent.",
        ["qa"] = "Answer only the question. If it asks when, use timingWindows only: name the nearest window with its dates and influence, then the stronger window when it is present. Do not invent a second window. Do not repeat the same dates. Never mention a date before today. When a window has only an end date, say by that date. Use the supplied period influence words when they are present. Skip anything absent.",
    };

    private readonly HttpClient _httpClient;
    private readonly DhruvaNarrationOptions _options;
    private readonly IWebHostEnvironment _environment;

    public DhruvaNarrationService(
        HttpClient httpClient,
        IOptions<DhruvaNarrationOptions> options,
        IWebHostEnvironment environment)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _environment = environment;
    }

    public static bool IsSupportedMode(string? mode) =>
        !string.IsNullOrWhiteSpace(mode) && Modes.Contains(mode.Trim());

    public async Task<DhruvaNarrateResponseDto> NarrateAsync(DhruvaNarrateRequestDto request, CancellationToken cancellationToken)
    {
        var mode = request.Mode.Trim().ToLowerInvariant();
        var profile = DhruvaToneProfile.Resolve(request.Tone);
        var today = DateTime.UtcNow.Date;
        var facts = DhruvaEvidenceSelector.OmitPastDates(
            DhruvaEvidenceSelector.Trim(
                DhruvaChartFactBuilder.Build(request.Chart, mode, request.Question),
                profile.EvidenceLimit),
            today);
        var locale = ResolveResponseLanguage(null, request.Locale);
        var feeling = profile.Tone == "corporate"
            ? null
            : DhruvaQuestionFeeling.Classify(request.Question);
        var userMessage = BuildUserMessage(mode, locale, request.Question, profile, facts, feeling);
        var payload = new ChatCompletionRequest
        {
            Model = _options.Model,
            Temperature = _options.Temperature,
            MaxCompletionTokens = profile.MaxCompletionTokens,
            ReasoningEffort = UsesReasoningEffort ? _options.ReasoningEffort.Trim() : null,
            Messages =
            [
                new { role = "system", content = ComposeSystemPrompt(profile) },
                new { role = "user", content = userMessage },
            ],
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(ChatCompletionsUri(), payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new DhruvaNarrationUnavailableException("Chart narration is unavailable right now.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var narration = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
            if (string.IsNullOrWhiteSpace(narration))
            {
                throw new DhruvaNarrationUnavailableException("Chart narration is unavailable right now.");
            }

            narration = OmitPastDatesFromReply(ToPlainText(narration), DateTime.UtcNow.Date);
            var disclaimer = DisclaimerFor(locale);
            var (promptTokens, completionTokens, totalTokens) = ReadTokenUsage(document.RootElement);

            return new DhruvaNarrateResponseDto
            {
                Narration = EnsureDisclaimer(narration.Trim(), locale),
                Model = _options.Model,
                PromptVersion = PromptVersion,
                Disclaimer = disclaimer,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                TotalTokens = totalTokens,
            };
        }
        catch (DhruvaNarrationUnavailableException)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new DhruvaNarrationUnavailableException("Chart narration is unavailable right now.", exception);
        }
    }

    public static string ResolveResponseLanguage(string? preferredLanguage, string? requestLocale) =>
        PreferredLanguageHelper.Normalize(preferredLanguage)
        ?? PreferredLanguageHelper.Normalize(requestLocale)
        ?? "en";

    public static string DisclaimerFor(string? locale) =>
        ResolveResponseLanguage(locale, null) switch
        {
            "hi" => "केवल शैक्षिक और आध्यात्मिक मार्गदर्शन के लिए। यह चिकित्सकीय, कानूनी या वित्तीय सलाह नहीं है। परिणामों की गारंटी नहीं है।",
            "kn" => "ಇದು ಕೇವಲ ಶೈಕ್ಷಣಿಕ ಮತ್ತು ಆಧ್ಯಾತ್ಮಿಕ ಮಾರ್ಗದರ್ಶನಕ್ಕೆ ಮಾತ್ರ. ಇದು ವೈದ್ಯಕೀಯ, ಕಾನೂನು ಅಥವಾ ಹಣಕಾಸಿನ ಸಲಹೆಯಲ್ಲ. ಫಲಿತಾಂಶಗಳಿಗೆ ಖಾತರಿ ಇಲ್ಲ.",
            _ => Disclaimer,
        };

    public static string EnsureDisclaimer(string narration, string? locale = null)
    {
        var disclaimer = DisclaimerFor(locale);
        return narration.Contains(disclaimer, StringComparison.Ordinal)
            ? narration
            : $"{narration.TrimEnd()}\n\n{disclaimer}";
    }

    public static string ToPlainText(string? narration)
    {
        if (string.IsNullOrWhiteSpace(narration))
        {
            return narration ?? string.Empty;
        }

        var plain = narration
            .Replace("```", string.Empty, StringComparison.Ordinal)
            .Replace("**", string.Empty, StringComparison.Ordinal)
            .Replace("__", string.Empty, StringComparison.Ordinal);
        plain = System.Text.RegularExpressions.Regex.Replace(plain, "</?[a-z][^>]*>", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        plain = System.Text.RegularExpressions.Regex.Replace(plain, @"`([^`]+)`", "$1");
        plain = System.Text.RegularExpressions.Regex.Replace(plain, @"^#{1,6}\s+", string.Empty, System.Text.RegularExpressions.RegexOptions.Multiline);
        plain = System.Text.RegularExpressions.Regex.Replace(plain, @"\*([^*\n]+)\*", "$1");
        plain = System.Text.RegularExpressions.Regex.Replace(plain, @"^\s*[-*+]\s+", string.Empty, System.Text.RegularExpressions.RegexOptions.Multiline);
        plain = System.Text.RegularExpressions.Regex.Replace(plain, @"\[([^\]]+)\]\([^)]+\)", "$1");
        plain = System.Text.RegularExpressions.Regex.Replace(plain, @"[ \t]+\n", "\n");
        plain = System.Text.RegularExpressions.Regex.Replace(plain, @"\n{3,}", "\n\n");
        return plain.Trim();
    }

    public static string OmitPastDatesFromReply(string? narration, DateTime today)
    {
        if (string.IsNullOrWhiteSpace(narration))
        {
            return narration ?? string.Empty;
        }

        const string months = "January|February|March|April|May|June|July|August|September|October|November|December|Jan|Feb|Mar|Apr|Jun|Jul|Aug|Sep|Sept|Oct|Nov|Dec";
        var pattern = $@"\b\d{{4}}-\d{{2}}-\d{{2}}\b|\b\d{{1,2}}\s+(?:{months})\s+\d{{4}}\b|\b(?:{months})\s+\d{{1,2}},?\s+\d{{4}}\b|\b(?:{months})\s+\d{{4}}\b";
        var normalized = System.Text.RegularExpressions.Regex.Replace(narration, @"[\u00A0\u202F\u2007\u2009\u200A]", " ");
        var cleaned = System.Text.RegularExpressions.Regex.Replace(
            normalized,
            pattern,
            match => IsPastProseDate(match.Value, today.Date) ? string.Empty : match.Value,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\b(?:19|20)\d{2}\b", match =>
            int.TryParse(match.Value, out var year) && year < today.Year ? string.Empty : match.Value);
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"[ \t]{2,}", " ");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\bbetween\s+and\b", "by", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\bfrom\s+to\b", "until", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\b(?:since|from|on)\s+(?=[,.]|$)", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Multiline);
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+([,.;:])", "$1");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\(\s*\)", string.Empty);
        return cleaned.Trim();
    }

    private static bool IsPastProseDate(string text, DateTime today)
    {
        var value = System.Text.RegularExpressions.Regex.Replace(text.Trim(), @"\bSept\b", "Sep", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var culture = CultureInfo.GetCultureInfo("en-US");
        string[] formats = ["yyyy-MM-dd", "d MMMM yyyy", "d MMM yyyy", "MMMM d, yyyy", "MMMM d yyyy", "MMM d, yyyy", "MMM d yyyy", "MMMM yyyy", "MMM yyyy"];
        if (!DateTime.TryParseExact(value, formats, culture, DateTimeStyles.AllowWhiteSpaces, out var parsed))
        {
            return false;
        }

        if (value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length == 2 && !value.Contains('-'))
        {
            return parsed.Year < today.Year || (parsed.Year == today.Year && parsed.Month < today.Month);
        }

        return parsed.Date < today.Date;
    }

    private Uri ChatCompletionsUri()
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        return new Uri($"{baseUrl}/chat/completions");
    }

    private string LoadSystemPrompt()
    {
        var path = _options.SystemPromptPath;
        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(_environment.ContentRootPath, path);
        }

        return File.Exists(path)
            ? File.ReadAllText(path)
            : DhruvaToneProfile.Template;
    }

    private bool UsesReasoningEffort =>
        _options.Model.Contains("gpt-oss", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(_options.ReasoningEffort);

    private string ComposeSystemPrompt(DhruvaToneProfile profile)
    {
        var template = LoadSystemPrompt();
        if (template.Contains("{{TONE_PROFILE}}", StringComparison.Ordinal))
        {
            return template
                .Replace("{{TONE_PROFILE}}", profile.PromptLine, StringComparison.Ordinal)
                .Replace("{{AUDIENCE}}", string.Empty, StringComparison.Ordinal)
                .Replace("{{RESPONSE_STYLE}}", string.Empty, StringComparison.Ordinal);
        }

        return $"""
            {template.TrimEnd()}

            TONE:
            {profile.PromptLine}
            """;
    }

    private static string BuildUserMessage(string mode, string? locale, string? question, DhruvaToneProfile profile, string evidence, string? feeling)
    {
        var instruction = ModeInstructions.TryGetValue(mode, out var text)
            ? text
            : ModeInstructions["qa"];
        var language = ResolveResponseLanguage(null, locale);
        var questionLine = string.IsNullOrWhiteSpace(question)
            ? "Question: Narrate the supplied evidence."
            : $"Question: {question.Trim()}";
        var feelLine = string.IsNullOrWhiteSpace(feeling) ? string.Empty : $"feel:{feeling}\n";
        return $"""
            Response language: {LanguageName(language)}
            {LanguageInstruction(language)}
            Mode: {mode}
            Selected tone: {profile.Tone}
            {questionLine}
            {feelLine}{instruction}

            EVIDENCE:
            {evidence}
            """;
    }

    private sealed class ChatCompletionRequest
    {
        public string Model { get; init; } = string.Empty;

        public double Temperature { get; init; }

        [JsonPropertyName("max_completion_tokens")]
        public int MaxCompletionTokens { get; init; }

        [JsonPropertyName("reasoning_effort")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ReasoningEffort { get; init; }

        public object[] Messages { get; init; } = [];
    }

    private static (int PromptTokens, int CompletionTokens, int TotalTokens) ReadTokenUsage(JsonElement root)
    {
        if (!root.TryGetProperty("usage", out var usage) || usage.ValueKind != JsonValueKind.Object)
        {
            return (0, 0, 0);
        }

        var prompt = ReadToken(usage, "prompt_tokens");
        var completion = ReadToken(usage, "completion_tokens");
        var total = ReadToken(usage, "total_tokens");
        if (total <= 0)
        {
            total = prompt + completion;
        }

        return (prompt, completion, total);
    }

    private static int ReadToken(JsonElement usage, string name)
    {
        return usage.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out var number)
            ? Math.Max(0, number)
            : 0;
    }

    private static string LanguageName(string language) => language switch
    {
        "hi" => "Hindi",
        "kn" => "Kannada",
        _ => "English",
    };

    private static string LanguageInstruction(string language) => language switch
    {
        "hi" => "Write the entire answer in Hindi using Devanagari. Do not answer in the question's language.",
        "kn" => "Write the entire answer in Kannada using Kannada script. Do not answer in the question's language.",
        _ => "Write the entire answer in English. Do not answer in the question's language.",
    };
}
