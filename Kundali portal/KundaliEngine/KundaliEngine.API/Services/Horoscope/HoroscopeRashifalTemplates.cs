using KundaliEngine.Domain.Enums;

namespace KundaliEngine.API.Services.Horoscope;

/// <summary>
/// Deterministic newspaper-style lines keyed by planet + house-from-Moon + tone.
/// Same inputs always produce the same text for every user.
/// </summary>
public static class HoroscopeRashifalTemplates
{
    public static readonly IReadOnlyDictionary<ZodiacSign, string> EnglishNames =
        new Dictionary<ZodiacSign, string>
        {
            [ZodiacSign.Aries] = "Aries",
            [ZodiacSign.Taurus] = "Taurus",
            [ZodiacSign.Gemini] = "Gemini",
            [ZodiacSign.Cancer] = "Cancer",
            [ZodiacSign.Leo] = "Leo",
            [ZodiacSign.Virgo] = "Virgo",
            [ZodiacSign.Libra] = "Libra",
            [ZodiacSign.Scorpio] = "Scorpio",
            [ZodiacSign.Sagittarius] = "Sagittarius",
            [ZodiacSign.Capricorn] = "Capricorn",
            [ZodiacSign.Aquarius] = "Aquarius",
            [ZodiacSign.Pisces] = "Pisces"
        };

    public static string LineFor(string planet, int house, string tone)
    {
        var key = $"{planet}|{house}|{tone}";
        if (Catalog.TryGetValue(key, out var line))
        {
            return line;
        }

        return tone switch
        {
            "Supportive" => $"{planet} in the {Ordinal(house)} from Moon supports steady progress.",
            "Challenging" => $"{planet} in the {Ordinal(house)} from Moon asks for patience and care.",
            _ => $"{planet} in the {Ordinal(house)} from Moon keeps results mixed."
        };
    }

    public static string AreaLine(string areaKey, string tone) => (areaKey, tone) switch
    {
        ("career", "Supportive") => "Work visibility improves; advance planned tasks.",
        ("career", "Challenging") => "Career needs patience; avoid abrupt job moves.",
        ("money", "Supportive") => "Cash flow and resources look steadier.",
        ("money", "Challenging") => "Spend carefully; delay speculative bets.",
        ("health", "Supportive") => "Vitality holds if routines stay regular.",
        ("health", "Challenging") => "Protect rest and avoid overstrain.",
        ("relations", "Supportive") => "Partnerships respond to warmth and clarity.",
        ("relations", "Challenging") => "Keep conversations measured; avoid ego clashes.",
        ("mind", "Supportive") => "Focus and study come more easily.",
        ("mind", "Challenging") => "Quiet the mind; postpone heavy decisions.",
        ("home", "Supportive") => "Home and comfort themes ease.",
        ("home", "Challenging") => "Domestic stress needs gentle handling.",
        (_, "Supportive") => "Conditions favour constructive effort.",
        (_, "Challenging") => "Proceed slowly and conserve energy.",
        _ => "Results stay mixed; stay practical."
    };

    public static string BuildSummary(string rashi, string tone, IReadOnlyList<string> highlights)
    {
        var lead = tone switch
        {
            "Supportive" => $"For {rashi} Moon natives, the period leans supportive.",
            "Challenging" => $"For {rashi} Moon natives, the period asks caution.",
            _ => $"For {rashi} Moon natives, the period stays mixed."
        };

        var detail = highlights.Count > 0
            ? string.Join(" ", highlights.Take(2))
            : "Slow planets set the broader tone from Chandra lagna.";
        return $"{lead} {detail}";
    }

    private static string Ordinal(int house) => house switch
    {
        1 => "1st",
        2 => "2nd",
        3 => "3rd",
        _ => $"{house}th"
    };

    private static readonly Dictionary<string, string> Catalog = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Jupiter|2|Supportive"] = "Jupiter in the 2nd from Moon favours speech, savings, and family support.",
        ["Jupiter|5|Supportive"] = "Jupiter in the 5th from Moon lifts intellect, creativity, and goodwill.",
        ["Jupiter|7|Supportive"] = "Jupiter in the 7th from Moon blesses partnerships and agreements.",
        ["Jupiter|9|Supportive"] = "Jupiter in the 9th from Moon opens luck, mentors, and longer journeys.",
        ["Jupiter|11|Supportive"] = "Jupiter in the 11th from Moon strengthens gains and networks.",
        ["Saturn|1|Challenging"] = "Saturn on the Moon sign (Sade-Sati zone) asks discipline and emotional steadiness.",
        ["Saturn|8|Challenging"] = "Saturn in the 8th from Moon (Ashtama) brings delays and deeper review.",
        ["Saturn|12|Challenging"] = "Saturn in the 12th from Moon emphasises solitude, expenses, and rest.",
        ["Saturn|4|Challenging"] = "Saturn in the 4th from Moon pressures home peace and inner ease.",
        ["Saturn|7|Challenging"] = "Saturn in the 7th from Moon tests partnerships with duty and distance.",
        ["Saturn|10|Challenging"] = "Saturn in the 10th from Moon slows career progress but builds endurance.",
        ["Mars|1|Challenging"] = "Mars on the Moon sign raises heat, haste, and short tempers.",
        ["Mars|8|Challenging"] = "Mars in the 8th from Moon warns against risk and accidents.",
        ["Rahu|1|Challenging"] = "Rahu on the Moon sign stirs restlessness and sudden shifts.",
        ["Ketu|1|Challenging"] = "Ketu on the Moon sign thins emotional certainty; keep routines.",
        ["Venus|7|Supportive"] = "Venus in the 7th from Moon softens relations and aesthetic luck.",
        ["Venus|4|Supportive"] = "Venus in the 4th from Moon comforts home and heart.",
        ["Mercury|3|Supportive"] = "Mercury in the 3rd from Moon aids writing, deals, and short travel.",
        ["Sun|10|Supportive"] = "Sun in the 10th from Moon highlights authority and recognition."
    };
}
