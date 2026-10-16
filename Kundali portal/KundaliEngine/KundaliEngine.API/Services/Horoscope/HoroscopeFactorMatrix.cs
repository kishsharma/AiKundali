namespace KundaliEngine.API.Services.Horoscope;

/// <summary>
/// Parashara-aligned factor weights for personal daily / weekly / monthly horoscope (v1, no TP).
/// Priority when factors conflict: lagna/planets &gt; tithi+nakshatra bala &gt; karana/vara/yoga.
/// </summary>
public static class HoroscopeFactorMatrix
{
    public const double NatalContextWeight = 0.15;
    public const double DashaWeight = 0.35;
    public const double GocharaWeight = 0.35;
    public const double PanchangaWeight = 0.15;

    public static HoroscopeHorizonWeights ForHorizon(HoroscopeHorizon horizon) => horizon switch
    {
        HoroscopeHorizon.Daily => new HoroscopeHorizonWeights(
            Natal: 0.10,
            Dasha: 0.25,
            Gochara: 0.35,
            Panchanga: 0.30,
            PreferSlowGochara: false,
            PreferFastGochara: true,
            UsePratyantarSoft: true),
        HoroscopeHorizon.Weekly => new HoroscopeHorizonWeights(
            Natal: 0.12,
            Dasha: 0.33,
            Gochara: 0.40,
            Panchanga: 0.15,
            PreferSlowGochara: true,
            PreferFastGochara: true,
            UsePratyantarSoft: false),
        HoroscopeHorizon.Monthly => new HoroscopeHorizonWeights(
            Natal: 0.15,
            Dasha: 0.40,
            Gochara: 0.40,
            Panchanga: 0.05,
            PreferSlowGochara: true,
            PreferFastGochara: false,
            UsePratyantarSoft: false),
        _ => new HoroscopeHorizonWeights(0.15, 0.35, 0.35, 0.15, true, true, false)
    };

    public static readonly IReadOnlyList<HoroscopeAreaDefinition> Areas =
    [
        new("health", "Health & vitality", [1, 6, 8], ["Sun", "Moon", "Mars"]),
        new("money", "Money & resources", [2, 11], ["Jupiter", "Venus", "Mercury"]),
        new("home", "Home & peace", [4], ["Moon", "Venus", "Mercury"]),
        new("mind", "Mind & intellect", [5, 9], ["Mercury", "Jupiter", "Moon"]),
        new("relations", "Relations", [7], ["Venus", "Jupiter", "Moon"]),
        new("career", "Career & work", [10, 6], ["Sun", "Saturn", "Mercury", "Mars"])
    ];

    /// <summary>Classical gochara houses often read as supportive from Moon/Lagna for Jupiter.</summary>
    public static readonly HashSet<int> JupiterSupportHouses = [2, 5, 7, 9, 11];

    /// <summary>Saturn pressure houses commonly tracked in gochara (incl. Sade-Sati / Ashtama).</summary>
    public static readonly HashSet<int> SaturnPressureHouses = [1, 4, 7, 8, 10, 12];

    public static readonly HashSet<string> BeneficialYogas = new(StringComparer.OrdinalIgnoreCase)
    {
        "Siddha", "Sadhya", "Shubha", "Shukla", "Brahma", "Indra", "Vaidhriti" // Vaidhriti kept listed but scored separately
    };

    public static readonly HashSet<string> ChallengingYogas = new(StringComparer.OrdinalIgnoreCase)
    {
        "Vyatipata", "Vaidhriti", "Vyaghata", "Parigha", "Vishkambha", "Atiganda", "Shula", "Ganda"
    };

    public static readonly HashSet<string> ChallengingKaranas = new(StringComparer.OrdinalIgnoreCase)
    {
        "Vishti", "Bhadra"
    };
}

public enum HoroscopeHorizon
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2
}

public sealed record HoroscopeHorizonWeights(
    double Natal,
    double Dasha,
    double Gochara,
    double Panchanga,
    bool PreferSlowGochara,
    bool PreferFastGochara,
    bool UsePratyantarSoft);

public sealed record HoroscopeAreaDefinition(
    string Key,
    string Title,
    IReadOnlyList<int> Houses,
    IReadOnlyList<string> Karakas);
