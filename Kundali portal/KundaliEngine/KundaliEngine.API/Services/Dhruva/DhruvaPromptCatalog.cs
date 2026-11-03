namespace KundaliEngine.API.Services.Dhruva;

public sealed class DhruvaPromptCatalog
{
    private readonly IReadOnlyDictionary<string, DhruvaToneDefinition> _tones;
    private readonly IReadOnlyDictionary<string, string> _modes;

    private DhruvaPromptCatalog(
        string systemPrompt,
        IReadOnlyDictionary<string, DhruvaToneDefinition> tones,
        IReadOnlyDictionary<string, string> modes)
    {
        SystemPrompt = systemPrompt;
        _tones = tones;
        _modes = modes;
    }

    public string SystemPrompt { get; }

    public static DhruvaPromptCatalog Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The configured Dhruva prompt file was not found.", path);
        }

        var sections = ParseSections(File.ReadAllLines(path));
        var systemPrompt = RequiredSection(sections, "system");
        var tones = new Dictionary<string, DhruvaToneDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var tone in new[] { "friendly", "astrologer", "corporate" })
        {
            tones[tone] = ParseTone(tone, RequiredSection(sections, $"tone:{tone}"));
        }

        var modes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var mode in new[] { "janma_story", "dasha_focus", "compatibility", "qa" })
        {
            modes[mode] = RequiredSection(sections, $"mode:{mode}");
        }

        return new DhruvaPromptCatalog(systemPrompt, tones, modes);
    }

    public DhruvaToneProfile ResolveTone(string? tone)
    {
        var normalized = DhruvaToneProfile.Normalize(tone);
        var definition = _tones[normalized];
        return new DhruvaToneProfile
        {
            Tone = normalized,
            EvidenceLimit = definition.EvidenceLimit,
            MaxCompletionTokens = definition.MaxCompletionTokens,
            PromptLine = definition.PromptLine,
        };
    }

    public string GetModeInstruction(string mode) =>
        _modes.TryGetValue(mode, out var instruction)
            ? instruction
            : _modes["qa"];

    private static DhruvaToneDefinition ParseTone(string tone, string content)
    {
        var lines = content.Split('\n');
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var promptLines = new List<string>();
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("evidence_limit=", StringComparison.OrdinalIgnoreCase)
                || trimmed.StartsWith("max_completion_tokens=", StringComparison.OrdinalIgnoreCase))
            {
                var separator = trimmed.IndexOf('=');
                values[trimmed[..separator]] = trimmed[(separator + 1)..].Trim();
            }
            else if (!string.IsNullOrWhiteSpace(trimmed))
            {
                promptLines.Add(trimmed);
            }
        }

        if (!values.TryGetValue("evidence_limit", out var evidenceText)
            || !int.TryParse(evidenceText, out var evidenceLimit)
            || evidenceLimit < 1)
        {
            throw new InvalidDataException($"Tone '{tone}' has an invalid evidence_limit.");
        }

        if (!values.TryGetValue("max_completion_tokens", out var tokensText)
            || !int.TryParse(tokensText, out var maxCompletionTokens)
            || maxCompletionTokens < 1)
        {
            throw new InvalidDataException($"Tone '{tone}' has an invalid max_completion_tokens.");
        }

        var prompt = string.Join(Environment.NewLine, promptLines).Trim();
        if (prompt.Length == 0)
        {
            throw new InvalidDataException($"Tone '{tone}' has no prompt text.");
        }

        return new DhruvaToneDefinition(evidenceLimit, maxCompletionTokens, prompt);
    }

    private static Dictionary<string, string> ParseSections(IEnumerable<string> lines)
    {
        var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? currentName = null;
        var currentLines = new List<string>();

        void SaveCurrent()
        {
            if (currentName is not null)
            {
                sections[currentName] = string.Join(Environment.NewLine, currentLines).Trim();
            }
        }

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.StartsWith('[') && line.EndsWith(']') && !line.StartsWith("[/", StringComparison.Ordinal))
            {
                SaveCurrent();
                currentName = line[1..^1].Trim();
                currentLines = new List<string>();
                continue;
            }

            if (line.StartsWith("[/", StringComparison.Ordinal) && line.EndsWith(']'))
            {
                SaveCurrent();
                currentName = null;
                currentLines = new List<string>();
                continue;
            }

            if (currentName is not null)
            {
                currentLines.Add(rawLine);
            }
        }

        SaveCurrent();
        return sections;
    }

    private static string RequiredSection(IReadOnlyDictionary<string, string> sections, string name)
    {
        if (!sections.TryGetValue(name, out var content) || string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidDataException($"The Dhruva prompt file is missing the '{name}' section.");
        }

        return content;
    }

    private sealed record DhruvaToneDefinition(
        int EvidenceLimit,
        int MaxCompletionTokens,
        string PromptLine);
}
