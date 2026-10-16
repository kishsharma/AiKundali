using System.Text.RegularExpressions;

namespace KundaliEngine.API.Services.Dhruva;

public static class DhruvaQuestionFeeling
{
    public const string Anxious = "anxious";
    public const string Hopeful = "hopeful";
    public const string Grieving = "grieving";
    public const string Urgent = "urgent";

    private static readonly Rule[] Rules =
    [
        new(
            Grieving,
            ["grief", "grieving", "heartbroken", "heart broken", "divorce", "breakup", "break up", "died", "death", "passed away", "lonely", "hopeless", "depressed", "crying", "sadness", "sad", "udas"],
            ["दुख", "दुखी", "मृत्यु", "मर गया", "तलाक", "अकेला", "निराश", "ದುಃಖ", "ಸಾವು", "ವಿಚ್ಛೇದ", "ಏಕಾಂಗಿ"]),
        new(
            Anxious,
            ["scared", "afraid", "worried", "worries", "worry", "anxious", "anxiety", "nervous", "panic", "stressed", "stressful", "stress", "fear", "tense", "tension", "chinta", "ghabra", "pareshan"],
            ["डर", "घबरा", "चिंता", "चिंतित", "परेशान", "तनाव", "भय", "ಭಯ", "ಚಿಂತೆ", "ಆತಂಕ", "ಗಾಬರಿ"]),
        new(
            Urgent,
            ["urgent", "immediately", "right now", "as soon as", "asap", "emergency"],
            ["तुरंत", "फौरन", "ತುರ್ತು", "ಈಗಲೇ"]),
        new(
            Hopeful,
            ["hopeful", "hoping", "hope", "excited", "looking forward"],
            ["आशा", "उम्मीद", "ಆಸೆ", "ಭರವಸೆ"]),
    ];

    public static string? Classify(string? question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return null;
        }

        foreach (var rule in Rules)
        {
            if (rule.Matches(question))
            {
                return rule.Name;
            }
        }

        return null;
    }

    private sealed class Rule
    {
        private readonly Regex _latin;
        private readonly string[] _script;

        public Rule(string name, string[] latin, string[] script)
        {
            Name = name;
            _script = script;
            var pattern = string.Join("|", latin.Select(Regex.Escape).OrderByDescending(phrase => phrase.Length));
            _latin = new Regex($@"\b(?:{pattern})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        }

        public string Name { get; }

        public bool Matches(string text) =>
            _latin.IsMatch(text) || _script.Any(word => text.Contains(word, StringComparison.Ordinal));
    }
}
