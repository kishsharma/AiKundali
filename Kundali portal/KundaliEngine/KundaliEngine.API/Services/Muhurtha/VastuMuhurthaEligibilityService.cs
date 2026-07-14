using KundaliEngine.API.Services.Muhurtha.Models;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models.Muhurtha;

namespace KundaliEngine.API.Services.Muhurtha;

public class VastuMuhurthaEligibilityService : IVastuMuhurthaEligibilityService
{
    private readonly MuhurthaMetadataService _metadataService;
    private readonly IEphemerisProvider _ephemerisProvider;
    private readonly PanchangService _panchangService;

    public VastuMuhurthaEligibilityService(
        MuhurthaMetadataService metadataService,
        IEphemerisProvider ephemerisProvider,
        PanchangService panchangService)
    {
        _metadataService = metadataService;
        _ephemerisProvider = ephemerisProvider;
        _panchangService = panchangService;
    }

    public bool ShouldApply(MuhurthaEventType eventType)
    {
        return _metadataService.GetVastuEligibilityRules(eventType).Count > 0;
    }

    public VastuMuhurthaEligibilityResult? Evaluate(MuhurthaRequest request, MuhurthaCandidateSlot candidate)
    {
        var rules = _metadataService.GetVastuEligibilityRules(request.EventType);
        if (rules.Count == 0)
        {
            return null;
        }

        var midpoint = candidate.StartTime + TimeSpan.FromTicks((candidate.EndTime - candidate.StartTime).Ticks / 2);
        var location = new GeoLocation(request.Location.Latitude, request.Location.Longitude);
        var sun = _ephemerisProvider.GetPlanet(Planet.Sun, midpoint.UtcDateTime, location);
        var moon = _ephemerisProvider.GetPlanet(Planet.Moon, midpoint.UtcDateTime, location);
        var panchang = _panchangService.Calculate(midpoint.DateTime, sun.Longitude, moon.Longitude);
        var tithi = ResolveMuhurthaTithi(sun.Longitude, moon.Longitude);
        var weekday = midpoint.DayOfWeek.ToString();
        var moonSign = moon.Sign.ToString();

        var match = rules.FirstOrDefault(rule =>
            Matches(rule.Paksha, panchang.Paksha)
            && Matches(rule.Tithi, tithi)
            && MatchesAny(rule.Weekdays, weekday)
            && MatchesAny(rule.MoonSigns, moonSign));

        if (match is null)
        {
            return null;
        }

        return new VastuMuhurthaEligibilityResult
        {
            Paksha = panchang.Paksha,
            Tithi = tithi,
            Weekday = weekday,
            MoonSign = moonSign,
            Applicability = string.Join("-", match.Applicability),
            Ahuti = match.Ahuti,
            ScoreImpact = match.ScoreImpact,
            Source = string.IsNullOrWhiteSpace(match.Source) ? "VastuEligibilityRules" : match.Source
        };
    }

    private static bool Matches(string expected, string actual)
    {
        return string.Equals(Normalize(expected), Normalize(actual), StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesAny(IEnumerable<string> expectedValues, string actual)
    {
        return expectedValues.Any(expected => Matches(expected, actual));
    }

    private static string Normalize(string value)
    {
        return value.Trim().Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveMuhurthaTithi(double sunLongitude, double moonLongitude)
    {
        var diff = NormalizeLongitude(moonLongitude - sunLongitude);
        var tithiNumber = (int)(diff / 12) + 1;
        var pakshaTithiNumber = ((tithiNumber - 1) % 15) + 1;
        var isShuklaPaksha = tithiNumber <= 15;

        return pakshaTithiNumber switch
        {
            1 => "Prathama",
            2 => "Dvitiya",
            3 => "Tritiya",
            4 => "Chaturthi",
            5 => "Panchami",
            6 => "Shashthi",
            7 => "Saptami",
            8 => "Ashtami",
            9 => "Navami",
            10 => "Dashami",
            11 => "Ekadashi",
            12 => "Dwadashi",
            13 => "Trayodashi",
            14 => "Chaturdashi",
            15 when isShuklaPaksha => "Purnima",
            15 => "Amavasya",
            _ => "Unknown"
        };
    }

    private static double NormalizeLongitude(double angle)
    {
        return (angle % 360 + 360) % 360;
    }
}
