using KundaliEngine.API.Services.Muhurtha;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Dasha;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.Domain.Models.Muhurtha;

namespace KundaliEngine.API.Services.Horoscope;

public sealed class HoroscopeHorizonExtrasBuilder
{
    private static readonly Planet[] HoraSequence =
    [
        Planet.Sun, Planet.Venus, Planet.Mercury, Planet.Moon, Planet.Saturn, Planet.Jupiter, Planet.Mars
    ];

    private readonly IMuhurthaSunriseSunsetService _sunriseSunsetService;
    private readonly ChartService _chartService;

    public HoroscopeHorizonExtrasBuilder(
        IMuhurthaSunriseSunsetService sunriseSunsetService,
        ChartService chartService)
    {
        _sunriseSunsetService = sunriseSunsetService;
        _chartService = chartService;
    }

    public HoroscopeHorizonExtrasDto Build(
        HoroscopeHorizon horizon,
        DateOnly from,
        DateOnly to,
        DateTime analysisLocal,
        TimeSpan offset,
        double latitude,
        double longitude,
        string? masa,
        DashaCalculationResult dasha)
    {
        var extras = new HoroscopeHorizonExtrasDto
        {
            Masa = string.IsNullOrWhiteSpace(masa) ? null : masa
        };

        var location = new MuhurthaLocation
        {
            Name = "horoscope",
            Latitude = latitude,
            Longitude = longitude,
            Timezone = $"UTC{(offset < TimeSpan.Zero ? "-" : "+")}{offset:hh\\:mm}"
        };

        try
        {
            var (sunrise, sunset) = _sunriseSunsetService.Calculate(from, location, offset);
            extras.SunriseLocal = sunrise.ToOffset(offset).ToString("HH:mm");
            extras.SunsetLocal = sunset.ToOffset(offset).ToString("HH:mm");

            if (horizon == HoroscopeHorizon.Daily)
            {
                var peak = ResolvePeakHoraLord(sunrise.UtcDateTime, sunset.UtcDateTime, analysisLocal.DayOfWeek);
                extras.PeakHoraLord = peak.ToString();
                extras.Facts.Add($"Sunrise {extras.SunriseLocal}, sunset {extras.SunsetLocal}; peak day Hora lord {peak}.");
            }
        }
        catch
        {
            // Sunrise service may fail for edge coordinates; keep other extras.
        }

        if (horizon == HoroscopeHorizon.Weekly)
        {
            extras.DashaChangeovers = BuildDashaChangeovers(dasha, from, to);
            extras.MoonPath = BuildMoonPath(from, to, offset, latitude, longitude);
            if (extras.DashaChangeovers.Count > 0)
            {
                extras.Facts.AddRange(extras.DashaChangeovers.Take(3));
            }

            if (extras.MoonPath.Count > 0)
            {
                extras.Facts.Add($"Moon path this week: {string.Join(" → ", extras.MoonPath)}.");
            }
        }

        if (horizon == HoroscopeHorizon.Monthly && !string.IsNullOrWhiteSpace(masa))
        {
            extras.Facts.Add($"Lunar masa frame: {masa}.");
        }

        return extras;
    }

    private List<string> BuildMoonPath(DateOnly from, DateOnly to, TimeSpan offset, double latitude, double longitude)
    {
        var path = new List<string>();
        var mid = from.AddDays(Math.Max(0, (to.DayNumber - from.DayNumber) / 2));
        foreach (var day in new[] { from, mid, to }.Distinct())
        {
            var local = day.ToDateTime(new TimeOnly(12, 0));
            var utc = new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), offset).UtcDateTime;
            var moon = _chartService.GenerateChart(utc, latitude, longitude)
                .FirstOrDefault(p => p.Planet == Planet.Moon);
            if (moon != null)
            {
                path.Add(moon.Sign.ToString());
            }
        }

        return path;
    }

    private static List<string> BuildDashaChangeovers(DashaCalculationResult dasha, DateOnly from, DateOnly to)
    {
        var facts = new List<string>();
        var fromUtc = from.ToDateTime(TimeOnly.MinValue);
        var toUtc = to.ToDateTime(new TimeOnly(23, 59));

        if (dasha.CurrentAntardasha.StartDate >= fromUtc && dasha.CurrentAntardasha.StartDate <= toUtc)
        {
            facts.Add($"{dasha.CurrentAntardasha.SubPlanet} Antardasha begins {dasha.CurrentAntardasha.StartDate:yyyy-MM-dd}.");
        }

        if (dasha.CurrentAntardasha.EndDate >= fromUtc && dasha.CurrentAntardasha.EndDate <= toUtc)
        {
            facts.Add($"{dasha.CurrentAntardasha.SubPlanet} Antardasha ends {dasha.CurrentAntardasha.EndDate:yyyy-MM-dd}.");
        }

        if (dasha.CurrentPratyantardasha != null
            && dasha.CurrentPratyantardasha.InnerPlanet != default
            && dasha.CurrentPratyantardasha.StartDate >= fromUtc
            && dasha.CurrentPratyantardasha.StartDate <= toUtc)
        {
            facts.Add($"{dasha.CurrentPratyantardasha.InnerPlanet} Pratyantardasha begins {dasha.CurrentPratyantardasha.StartDate:yyyy-MM-dd}.");
        }

        return facts;
    }

    private static Planet ResolvePeakHoraLord(DateTime sunriseUtc, DateTime sunsetUtc, DayOfWeek weekday)
    {
        var dayLength = Math.Max((sunsetUtc - sunriseUtc).TotalHours, 1);
        var horaLength = dayLength / 12.0;
        var peakIndex = 6; // midday-ish day hora
        var startLord = WeekdayLord(weekday);
        var startIndex = Array.IndexOf(HoraSequence, startLord);
        if (startIndex < 0)
        {
            startIndex = 0;
        }

        return HoraSequence[(startIndex + peakIndex) % HoraSequence.Length];
    }

    private static Planet WeekdayLord(DayOfWeek day) => day switch
    {
        DayOfWeek.Sunday => Planet.Sun,
        DayOfWeek.Monday => Planet.Moon,
        DayOfWeek.Tuesday => Planet.Mars,
        DayOfWeek.Wednesday => Planet.Mercury,
        DayOfWeek.Thursday => Planet.Jupiter,
        DayOfWeek.Friday => Planet.Venus,
        _ => Planet.Saturn
    };
}

public sealed class HoroscopeHorizonExtrasDto
{
    public List<string> Facts { get; set; } = new();
    public string? SunriseLocal { get; set; }
    public string? SunsetLocal { get; set; }
    public string? PeakHoraLord { get; set; }
    public string? Masa { get; set; }
    public List<string> DashaChangeovers { get; set; } = new();
    public List<string> MoonPath { get; set; } = new();
}
