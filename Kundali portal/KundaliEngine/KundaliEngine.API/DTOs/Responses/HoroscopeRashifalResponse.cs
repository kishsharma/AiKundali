using KundaliEngine.API.Services.Horoscope;

namespace KundaliEngine.API.DTOs.Responses;

public sealed class HoroscopeRashifalResponse
{
    public HoroscopeHorizon Horizon { get; set; }

    public DateOnly From { get; set; }

    public DateOnly To { get; set; }

    public List<HoroscopeRashifalItemDto> Items { get; set; } = new();

    public string Disclaimer { get; set; } =
        "General Moon-sign rashifal. Identical for every person sharing the same Chandra rashi. Traditional Jyotish judgment, not medical, legal, or financial advice.";
}

public sealed class HoroscopeRashifalItemDto
{
    public string Rashi { get; set; } = string.Empty;

    public string EnglishName { get; set; } = string.Empty;

    public string Tone { get; set; } = "Mixed";

    public int Score { get; set; }

    public string Summary { get; set; } = string.Empty;

    public List<HoroscopeRashifalAreaDto> Areas { get; set; } = new();

    public List<string> GocharaHighlights { get; set; } = new();
}

public sealed class HoroscopeRashifalAreaDto
{
    public string Key { get; set; } = string.Empty;

    public string Tone { get; set; } = "Mixed";

    public string Line { get; set; } = string.Empty;
}
