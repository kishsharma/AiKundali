using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.API.Services.Horoscope;

public static class HoroscopePanchangaScorer
{
    private static readonly string[] TaraNames =
    [
        "Janma", "Sampat", "Vipat", "Kshema", "Pratyak", "Sadhana", "Nidhana", "Mitra", "Paramitral"
    ];

    public static HoroscopePanchangaScore Score(
        PanchangDetails panchanga,
        Nakshatra natalMoonNakshatra,
        HoroscopeHorizonWeights weights)
    {
        var facts = new List<string>();
        var support = 0;
        var pressure = 0;

        var currentNak = ParseNakshatra(panchanga.Nakshatra);
        var taraNumber = currentNak == null
            ? 1
            : ((int)currentNak - (int)natalMoonNakshatra + 27) % 27 % 9 + 1;
        var taraName = TaraNames[Math.Clamp(taraNumber - 1, 0, TaraNames.Length - 1)];
        var taraTone = taraNumber switch
        {
            2 or 4 or 6 or 8 or 9 => "Supportive",
            1 or 3 or 5 or 7 => "Challenging",
            _ => "Mixed"
        };

        if (taraTone == "Supportive")
        {
            support += 18;
            facts.Add($"Nakshatra tara from janma Moon is {taraName} ({taraNumber}) — supportive strength.");
        }
        else if (taraTone == "Challenging")
        {
            pressure += 16;
            facts.Add($"Nakshatra tara from janma Moon is {taraName} ({taraNumber}) — caution on force.");
        }

        if (HoroscopeFactorMatrix.ChallengingYogas.Contains(panchanga.Yoga))
        {
            pressure += 12;
            facts.Add($"Yoga {panchanga.Yoga} is classically inauspicious for cohesion.");
        }
        else
        {
            support += 6;
            facts.Add($"Yoga {panchanga.Yoga} frames the day's binding quality.");
        }

        if (HoroscopeFactorMatrix.ChallengingKaranas.Contains(panchanga.Karana))
        {
            pressure += 10;
            facts.Add($"Karana {panchanga.Karana} cautions concrete outcomes.");
        }
        else
        {
            support += 5;
            facts.Add($"Karana {panchanga.Karana} supports practical completion.");
        }

        facts.Add($"Tithi {panchanga.Paksha} {panchanga.Tithi} ({panchanga.Vaar}).");

        var delta = support - pressure;
        var tone = delta >= 10 ? "Supportive" : delta <= -10 ? "Challenging" : "Mixed";
        var score = Math.Clamp(50 + delta, 0, 100);

        // Soft weight application for horizon (panchanga matters more on daily).
        score = Math.Clamp((int)Math.Round(50 + (score - 50) * (0.6 + weights.Panchanga)), 0, 100);

        return new HoroscopePanchangaScore
        {
            Tithi = panchanga.Tithi,
            Paksha = panchanga.Paksha,
            Nakshatra = panchanga.Nakshatra,
            Pada = panchanga.Pada,
            Yoga = panchanga.Yoga,
            Karana = panchanga.Karana,
            Vaar = panchanga.Vaar,
            Maas = panchanga.Maas,
            TaraNumber = taraNumber,
            TaraName = taraName,
            TaraTone = taraTone,
            Score = score,
            Tone = tone,
            Support = support,
            Pressure = pressure,
            Facts = facts
        };
    }

    private static Nakshatra? ParseNakshatra(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return Enum.TryParse<Nakshatra>(name.Replace(" ", string.Empty), true, out var value)
            ? value
            : null;
    }
}

public sealed class HoroscopePanchangaScore
{
    public string Tithi { get; set; } = string.Empty;
    public string Paksha { get; set; } = string.Empty;
    public string Nakshatra { get; set; } = string.Empty;
    public int Pada { get; set; }
    public string Yoga { get; set; } = string.Empty;
    public string Karana { get; set; } = string.Empty;
    public string Vaar { get; set; } = string.Empty;
    public string Maas { get; set; } = string.Empty;
    public int TaraNumber { get; set; }
    public string TaraName { get; set; } = string.Empty;
    public string TaraTone { get; set; } = "Mixed";
    public int Score { get; set; }
    public string Tone { get; set; } = "Mixed";
    public int Support { get; set; }
    public int Pressure { get; set; }
    public List<string> Facts { get; set; } = new();
}
