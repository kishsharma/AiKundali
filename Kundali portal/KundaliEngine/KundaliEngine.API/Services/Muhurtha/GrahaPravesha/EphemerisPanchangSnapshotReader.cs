using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Calculations;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.Domain.Enums;

namespace KundaliEngine.API.Services.Muhurtha.GrahaPravesha;

public sealed class PanchangSnapshot
{
    public string Tithi { get; init; } = string.Empty;

    public string Paksha { get; init; } = string.Empty;

    public string Nakshatra { get; init; } = string.Empty;

    public string Yoga { get; init; } = string.Empty;

    public string Karana { get; init; } = string.Empty;

    public string MoonSign { get; init; } = string.Empty;

    public string Lagna { get; init; } = string.Empty;

    public int SunNakshatraIndex { get; init; }

    public int MoonNakshatraIndex { get; init; }

    public string Key => string.Join('|', Tithi, Paksha, Nakshatra, Yoga, Karana, MoonSign, Lagna);
}

public interface IPanchangSnapshotReader
{
    PanchangSnapshot Read(DateTimeOffset instant, double latitude, double longitude);
}

public sealed class EphemerisPanchangSnapshotReader : IPanchangSnapshotReader
{
    private readonly IEphemerisProvider _ephemerisProvider;
    private readonly PanchangService _panchangService;

    public EphemerisPanchangSnapshotReader(IEphemerisProvider ephemerisProvider, PanchangService panchangService)
    {
        _ephemerisProvider = ephemerisProvider;
        _panchangService = panchangService;
    }

    public PanchangSnapshot Read(DateTimeOffset instant, double latitude, double longitude)
    {
        var location = new GeoLocation(latitude, longitude);
        var utc = DateTime.SpecifyKind(instant.UtcDateTime, DateTimeKind.Utc);
        var sun = _ephemerisProvider.GetPlanet(Planet.Sun, utc, location);
        var moon = _ephemerisProvider.GetPlanet(Planet.Moon, utc, location);
        var panchang = _panchangService.Calculate(instant.DateTime, sun.Longitude, moon.Longitude);
        var ascendant = _ephemerisProvider.GetAscendant(utc, location);
        var sunNakshatra = NakshatraCalculator.Calculate(sun.Longitude).Nakshatra;
        var moonNakshatra = NakshatraCalculator.Calculate(moon.Longitude).Nakshatra;

        return new PanchangSnapshot
        {
            Tithi = panchang.Tithi,
            Paksha = panchang.Paksha,
            Nakshatra = panchang.Nakshatra,
            Yoga = panchang.Yoga,
            Karana = panchang.Karana,
            MoonSign = ZodiacCalculator.GetSign(moon.Longitude).ToString(),
            Lagna = ZodiacCalculator.GetSign(ascendant.Longitude).ToString(),
            SunNakshatraIndex = (int)sunNakshatra,
            MoonNakshatraIndex = (int)moonNakshatra
        };
    }
}
