namespace KundaliEngine.API.Services.Muhurtha.GrahaPravesha;

public sealed class ChakraEvaluation
{
    public string Chakra { get; init; } = string.Empty;

    public bool Enabled { get; init; }

    public bool Evaluated { get; init; }

    public int Count { get; init; }

    public bool Eligible { get; init; }

    public string Source { get; init; } = string.Empty;

    public string Tradition { get; init; } = string.Empty;

    public string Notes { get; init; } = string.Empty;
}

public interface IChakraRuleEvaluator
{
    string Chakra { get; }

    bool Enabled { get; }

    ChakraEvaluation Evaluate(int sunNakshatraIndex, int moonNakshatraIndex, GrahaPraveshaProfile profile);
}

public sealed class KalashaChakraEvaluator : IChakraRuleEvaluator
{
    public string Chakra => "KALASHA";

    public bool Enabled => true;

    public static int CountInclusive(int sunNakshatraIndex, int moonNakshatraIndex)
    {
        var sun = Normalize(sunNakshatraIndex);
        var moon = Normalize(moonNakshatraIndex);
        return ((moon - sun + 27) % 27) + 1;
    }

    public static bool IsEligible(int count, IEnumerable<GrahaPraveshaCountRange> ranges)
    {
        return ranges.Any(range => count >= range.From && count <= range.To);
    }

    public ChakraEvaluation Evaluate(int sunNakshatraIndex, int moonNakshatraIndex, GrahaPraveshaProfile profile)
    {
        var count = CountInclusive(sunNakshatraIndex, moonNakshatraIndex);
        var ranges = profile.Kalasha.FavorableRanges;
        return new ChakraEvaluation
        {
            Chakra = Chakra,
            Enabled = profile.Kalasha.Enabled,
            Evaluated = profile.Kalasha.Enabled,
            Count = count,
            Eligible = profile.Kalasha.Enabled && IsEligible(count, ranges),
            Source = profile.Kalasha.Source,
            Tradition = profile.Kalasha.Tradition,
            Notes = profile.Kalasha.Notes
        };
    }

    private static int Normalize(int index)
    {
        var value = index % 27;
        return value < 0 ? value + 27 : value;
    }
}

public sealed class VrishaChakraEvaluator : IChakraRuleEvaluator
{
    public string Chakra => "VRISHA";

    public bool Enabled => false;

    public ChakraEvaluation Evaluate(int sunNakshatraIndex, int moonNakshatraIndex, GrahaPraveshaProfile profile)
    {
        return new ChakraEvaluation
        {
            Chakra = Chakra,
            Enabled = false,
            Evaluated = false,
            Count = 0,
            Eligible = false,
            Source = profile.Vrisha.Source,
            Tradition = profile.Vrisha.Tradition,
            Notes = profile.Vrisha.Notes
        };
    }
}
