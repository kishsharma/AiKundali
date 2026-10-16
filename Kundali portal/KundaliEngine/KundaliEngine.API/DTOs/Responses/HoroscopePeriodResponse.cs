using KundaliEngine.API.Services.Horoscope;

namespace KundaliEngine.API.DTOs.Responses;

public sealed class HoroscopePeriodResponse
{
    public HoroscopeHorizon Horizon { get; set; }

    public DateOnly From { get; set; }

    public DateOnly To { get; set; }

    public string OverallTone { get; set; } = "Mixed";

    public int OverallScore { get; set; }

    public List<string> SummaryFacts { get; set; } = new();

    public List<HoroscopeAreaDto> Areas { get; set; } = new();

    public HoroscopePanchangaDto? Panchanga { get; set; }

    public List<HoroscopeGocharaHitDto> GocharaHits { get; set; } = new();

    public HoroscopeDashaContextDto? DashaContext { get; set; }

    public List<HoroscopeActiveFormationDto> ActiveFormations { get; set; } = new();

    public HoroscopeHorizonExtrasDto? HorizonExtras { get; set; }

    public HoroscopeTithiPraveshaStatusDto TithiPravesha { get; set; } = new();

    public List<string> Limitations { get; set; } = new();

    public string Disclaimer { get; set; } =
        "Traditional Jyotish judgment based on Parashara principles. Not medical, legal, or financial advice.";
}

public sealed class HoroscopeActiveFormationDto
{
    public string Type { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Tone { get; set; } = "Mixed";

    public string Fact { get; set; } = string.Empty;
}

public sealed class HoroscopeAreaDto
{
    public string Key { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Tone { get; set; } = "Mixed";

    public int Score { get; set; }

    /// <summary>Plain-language outlook for the person (shown in UI).</summary>
    public string Prediction { get; set; } = string.Empty;

    /// <summary>Technical Jyotish reasons (info-icon tooltip only).</summary>
    public List<string> Facts { get; set; } = new();

    public List<string> Do { get; set; } = new();

    public List<string> Caution { get; set; } = new();

    public List<string> Avoid { get; set; } = new();
}

public sealed class HoroscopePanchangaDto
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

    public List<string> Facts { get; set; } = new();
}

public sealed class HoroscopeGocharaHitDto
{
    public string Planet { get; set; } = string.Empty;

    public string Reference { get; set; } = string.Empty;

    public int House { get; set; }

    public string Sign { get; set; } = string.Empty;

    public string Tone { get; set; } = "Mixed";

    public string Fact { get; set; } = string.Empty;
}

public sealed class HoroscopeDashaContextDto
{
    public string Mahadasha { get; set; } = string.Empty;

    public string Antardasha { get; set; } = string.Empty;

    public string? Pratyantardasha { get; set; }

    public DateTime MahadashaStartUtc { get; set; }

    public DateTime MahadashaEndUtc { get; set; }

    public DateTime AntardashaStartUtc { get; set; }

    public DateTime AntardashaEndUtc { get; set; }

    public List<string> Facts { get; set; } = new();
}

public sealed class HoroscopeTithiPraveshaStatusDto
{
    public bool Enabled { get; set; }

    public string Status { get; set; } = "NotEnabled";

    public string Message { get; set; } =
        "Annual and daily Tithi Pravesha charts are reserved for phase 2. v1 judgments stay within natal + Vimshottari + gochara + panchanga.";

    public List<string> HierarchicalRules { get; set; } = new()
    {
        "A lower chart cannot give what the upper chart denies.",
        "Years: natal chart + Vimshottari MD/AD.",
        "Week/month: prefer annual Tithi Pravesha once enabled; until then use slow gochara + MD/AD.",
        "Day: prefer daily Tithi Pravesha + Hora lord once enabled; until then use panchanga + fast gochara + soft PD."
    };
}
