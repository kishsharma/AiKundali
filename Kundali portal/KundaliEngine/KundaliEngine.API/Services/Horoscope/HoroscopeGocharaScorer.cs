using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.API.Services.Horoscope;

public static class HoroscopeGocharaScorer
{
    private static readonly HashSet<Planet> SlowPlanets =
    [
        Planet.Saturn, Planet.Jupiter, Planet.Rahu, Planet.Ketu
    ];

    private static readonly HashSet<Planet> FastPlanets =
    [
        Planet.Moon, Planet.Mercury, Planet.Venus, Planet.Mars, Planet.Sun
    ];

    public static HoroscopeGocharaScore Score(
        IReadOnlyList<PlanetPosition> transitFromLagna,
        IReadOnlyList<PlanetPosition> transitFromMoon,
        Planet mahadasha,
        Planet antardasha,
        HoroscopeHorizonWeights weights)
    {
        var hits = new List<HoroscopeGocharaHit>();
        var support = 0;
        var pressure = 0;

        void Consider(PlanetPosition position, string reference)
        {
            if (position == null)
            {
                return;
            }

            var isSlow = SlowPlanets.Contains(position.Planet);
            var isFast = FastPlanets.Contains(position.Planet);
            if (weights.PreferSlowGochara && !weights.PreferFastGochara && isFast && !isSlow)
            {
                return;
            }

            if (weights.PreferFastGochara && !weights.PreferSlowGochara && isSlow)
            {
                // Still keep Saturn/Jupiter lightly on daily.
                if (position.Planet is not (Planet.Saturn or Planet.Jupiter))
                {
                    return;
                }
            }

            var house = (int)position.House;
            var tone = "Mixed";
            var fact = $"{position.Planet} transits house {house} from {reference} in {position.Sign}.";

            if (position.Planet == Planet.Jupiter && HoroscopeFactorMatrix.JupiterSupportHouses.Contains(house))
            {
                tone = "Supportive";
                support += reference == "Moon" ? 14 : 12;
                fact = $"Jupiter gochara in house {house} from {reference} — classical support.";
            }
            else if (position.Planet == Planet.Saturn && HoroscopeFactorMatrix.SaturnPressureHouses.Contains(house))
            {
                tone = "Challenging";
                pressure += reference == "Moon" ? 16 : 14;
                fact = house is 1 or 12 or 8
                    ? $"Saturn gochara in house {house} from {reference} — Sade-Sati / pressure zone."
                    : $"Saturn gochara in house {house} from {reference} — discipline and delay themes.";
            }
            else if (position.Planet is Planet.Rahu or Planet.Ketu && house is 1 or 7 or 8 or 12)
            {
                tone = "Challenging";
                pressure += 10;
                fact = $"{position.Planet} axis pressure on house {house} from {reference}.";
            }
            else if (position.Planet == Planet.Mars && house is 1 or 2 or 4 or 7 or 8 or 12)
            {
                tone = "Challenging";
                pressure += weights.PreferFastGochara ? 10 : 6;
                fact = $"Mars gochara in house {house} from {reference} — heat and haste.";
            }
            else if (position.Planet == Planet.Venus && house is 1 or 2 or 4 or 5 or 7 or 11)
            {
                tone = "Supportive";
                support += weights.PreferFastGochara ? 8 : 5;
                fact = $"Venus gochara in house {house} from {reference} — ease and affinity.";
            }

            if (position.Planet == mahadasha || position.Planet == antardasha)
            {
                support += 4;
                fact += $" Aligns with running {(position.Planet == mahadasha ? "Mahadasha" : "Antardasha")} lord.";
            }

            if (position.IsRetrograde)
            {
                fact += " (retrograde).";
            }

            hits.Add(new HoroscopeGocharaHit
            {
                Planet = position.Planet.ToString(),
                Reference = reference,
                House = house,
                Sign = position.Sign.ToString(),
                Tone = tone,
                Fact = fact
            });
        }

        foreach (var planet in transitFromLagna)
        {
            Consider(planet, "Lagna");
        }

        foreach (var planet in transitFromMoon)
        {
            Consider(planet, "Moon");
        }

        // Deduplicate noisy Moon-daily doubles: keep strongest tone per planet+reference+house
        hits = hits
            .GroupBy(h => $"{h.Planet}|{h.Reference}|{h.House}")
            .Select(g => g.OrderByDescending(h => ToneRank(h.Tone)).First())
            .OrderByDescending(h => ToneRank(h.Tone))
            .ThenBy(h => h.Planet)
            .Take(14)
            .ToList();

        var delta = support - pressure;
        var toneOverall = delta >= 12 ? "Supportive" : delta <= -12 ? "Challenging" : "Mixed";
        var score = Math.Clamp(50 + delta, 0, 100);

        return new HoroscopeGocharaScore
        {
            Hits = hits,
            Score = score,
            Tone = toneOverall,
            Support = support,
            Pressure = pressure,
            Facts = hits.Where(h => h.Tone != "Mixed").Select(h => h.Fact).Take(8).ToList()
        };
    }

    private static int ToneRank(string tone) => tone switch
    {
        "Challenging" => 3,
        "Supportive" => 2,
        _ => 1
    };
}

public sealed class HoroscopeGocharaHit
{
    public string Planet { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public int House { get; set; }
    public string Sign { get; set; } = string.Empty;
    public string Tone { get; set; } = "Mixed";
    public string Fact { get; set; } = string.Empty;
}

public sealed class HoroscopeGocharaScore
{
    public List<HoroscopeGocharaHit> Hits { get; set; } = new();
    public int Score { get; set; }
    public string Tone { get; set; } = "Mixed";
    public int Support { get; set; }
    public int Pressure { get; set; }
    public List<string> Facts { get; set; } = new();
}
