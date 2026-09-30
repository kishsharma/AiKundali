namespace KundaliEngine.API.Services.Muhurtha.GrahaPravesha;

public sealed class GrahaPraveshaProfile
{
    public string ProfileId { get; set; } = string.Empty;

    public string EventType { get; set; } = "GRAHA_PRAVESHA";

    public string Version { get; set; } = "1.0";

    public string EffectiveFrom { get; set; } = string.Empty;

    public string Tradition { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;

    public GrahaPraveshaScoringProfile Scoring { get; set; } = new();

    public GrahaPraveshaKalashaProfile Kalasha { get; set; } = new();

    public GrahaPraveshaChakraProfile Vrisha { get; set; } = new();

    public GrahaPraveshaLagnaProfile Lagna { get; set; } = new();

    public TimeSpan MinimumWindow { get; set; } = TimeSpan.FromMinutes(5);

    public List<string> CutOutWindowKeys { get; set; } = new();

    public List<string> KnownGaps { get; set; } = new();

    public List<GrahaPraveshaRule> Rules { get; set; } = new();
}

public sealed class GrahaPraveshaScoringProfile
{
    public int BaseScore { get; set; } = 50;

    public int SuitableMinimum { get; set; } = 65;

    public int RecommendedMinimum { get; set; } = 80;
}

public sealed class GrahaPraveshaKalashaProfile
{
    public string Chakra { get; set; } = "KALASHA";

    public bool Enabled { get; set; } = true;

    public List<GrahaPraveshaCountRange> FavorableRanges { get; set; } = new();

    public string Tradition { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}

public sealed class GrahaPraveshaChakraProfile
{
    public string Chakra { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public string Tradition { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}

public sealed class GrahaPraveshaLagnaProfile
{
    public List<string> Allowed { get; set; } = new();

    public List<string> Restricted { get; set; } = new();

    public List<string> Prohibited { get; set; } = new();

    public string Tradition { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}

public sealed class GrahaPraveshaCountRange
{
    public int From { get; set; }

    public int To { get; set; }
}

public sealed class GrahaPraveshaRule
{
    public string RuleId { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public int Priority { get; set; }

    public string HardOrSoft { get; set; } = "SOFT";

    public bool Enabled { get; set; } = true;

    public int ScoreImpact { get; set; }

    public int ScoreImpactWhenUnmatched { get; set; }

    public bool BlocksRecommendedWhenUnmatched { get; set; }

    public GrahaPraveshaRuleCondition Condition { get; set; } = new();

    public string Explanation { get; set; } = string.Empty;

    public string PassExplanation { get; set; } = string.Empty;

    public string UnmatchedExplanation { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string Tradition { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}

public sealed class GrahaPraveshaRuleCondition
{
    public string Type { get; set; } = string.Empty;

    public List<string> Values { get; set; } = new();
}

public sealed record GrahaPraveshaFacts
{
    public string Tithi { get; init; } = string.Empty;

    public string Paksha { get; init; } = string.Empty;

    public string Nakshatra { get; init; } = string.Empty;

    public string Yoga { get; init; } = string.Empty;

    public string Karana { get; init; } = string.Empty;

    public string Vara { get; init; } = string.Empty;

    public string MoonSign { get; init; } = string.Empty;

    public string Lagna { get; init; } = string.Empty;

    public int KalashaCount { get; init; }

    public bool VastuTableMatched { get; init; }

    public string VastuApplicability { get; init; } = string.Empty;

    public string VastuAhuti { get; init; } = string.Empty;

    public IReadOnlyList<string> OverlappingWindows { get; init; } = Array.Empty<string>();
}

public sealed class GrahaPraveshaEvidence
{
    public string RuleId { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Input { get; init; } = string.Empty;

    public string Result { get; init; } = string.Empty;

    public string Severity { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string Source { get; init; } = string.Empty;

    public int ScoreImpact { get; init; }
}

public sealed class GrahaPraveshaRuleOutcome
{
    public string Status { get; init; } = "ACCEPTABLE";

    public int Score { get; init; }

    public bool HardRejected { get; init; }

    public bool KalashaEligible { get; init; }

    public bool BlocksRecommended { get; init; }

    public List<GrahaPraveshaEvidence> Evidence { get; init; } = new();
}
