using System.Runtime.InteropServices;
using System.Text;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.Domain.Models.Muhurtha;

namespace KundaliEngine.API.Services.Panchang;

public class PanchangEclipseService : IPanchangEclipseService
{
    private const int SeFlgSwieph = 2;
    private const int SeEclTotal = 4;
    private const int SeEclAnnular = 8;
    private const int SeEclPartial = 16;
    private const int SeEclAnnularTotal = 32;
    private const int SeEclPenumbral = 64;
    private static readonly TimeSpan SolarSutakDuration = TimeSpan.FromHours(12);
    private static readonly TimeSpan LunarSutakDuration = TimeSpan.FromHours(9);

    public IReadOnlyList<PanchangEclipseDto> GetYearlyEclipses(int year, double latitude, double longitude, string timezone)
    {
        var timezoneInfo = ResolveTimezone(timezone);
        var location = new MuhurthaLocation
        {
            Latitude = latitude,
            Longitude = longitude,
            Timezone = timezone
        };

        var yearStartLocal = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var yearEndLocal = new DateTime(year + 1, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var yearStartUtc = ConvertLocalToUtc(yearStartLocal, timezoneInfo);
        var yearEndUtc = ConvertLocalToUtc(yearEndLocal, timezoneInfo);
        var eclipses = new List<PanchangEclipseDto>();

        eclipses.AddRange(FindSolarEclipses(year, yearStartUtc, yearEndUtc, timezoneInfo, location));
        eclipses.AddRange(FindLunarEclipses(year, yearStartUtc, yearEndUtc, timezoneInfo, location));

        return eclipses
            .OrderBy(eclipse => eclipse.MaximumTime)
            .ToList();
    }

    private IEnumerable<PanchangEclipseDto> FindSolarEclipses(
        int year,
        DateTime yearStartUtc,
        DateTime yearEndUtc,
        TimeZoneInfo timezoneInfo,
        MuhurthaLocation location)
    {
        var cursor = yearStartUtc;
        while (cursor < yearEndUtc)
        {
            var result = FindSolarEclipse(cursor, location);
            if (result is null)
            {
                yield break;
            }

            var maximumUtc = FromJulian(result.Times[0]);
            if (maximumUtc >= yearEndUtc)
            {
                yield break;
            }

            var startUtc = FirstNonZero(result.Times[1], result.Times[0]);
            var endUtc = FirstNonZero(result.Times[4], result.Times[0]);
            if (endUtc < yearStartUtc)
            {
                cursor = maximumUtc.AddDays(1);
                continue;
            }

            yield return BuildDto(
                year,
                "solar",
                ResolveSolarType(result.Flags),
                startUtc,
                maximumUtc,
                endUtc,
                startUtc - SolarSutakDuration,
                endUtc,
                timezoneInfo);

            cursor = AdvanceCursor(cursor, endUtc, maximumUtc);
        }
    }

    private IEnumerable<PanchangEclipseDto> FindLunarEclipses(
        int year,
        DateTime yearStartUtc,
        DateTime yearEndUtc,
        TimeZoneInfo timezoneInfo,
        MuhurthaLocation location)
    {
        var cursor = yearStartUtc;
        while (cursor < yearEndUtc)
        {
            var result = FindLunarEclipse(cursor, location);
            if (result is null)
            {
                yield break;
            }

            var maximumUtc = FromJulian(result.Times[0]);
            if (maximumUtc >= yearEndUtc)
            {
                yield break;
            }

            var startUtc = FirstNonZero(result.Times[2], result.Times[6], result.Times[0]);
            var endUtc = FirstNonZero(result.Times[3], result.Times[7], result.Times[0]);
            if (endUtc < yearStartUtc)
            {
                cursor = maximumUtc.AddDays(1);
                continue;
            }

            var type = ResolveLunarType(result.Flags);
            var sutakStart = string.Equals(type, "Penumbral", StringComparison.OrdinalIgnoreCase)
                ? (DateTime?)null
                : startUtc - LunarSutakDuration;
            var sutakEnd = string.Equals(type, "Penumbral", StringComparison.OrdinalIgnoreCase)
                ? (DateTime?)null
                : endUtc;

            yield return BuildDto(
                year,
                "lunar",
                type,
                startUtc,
                maximumUtc,
                endUtc,
                sutakStart,
                sutakEnd,
                timezoneInfo);

            cursor = AdvanceCursor(cursor, endUtc, maximumUtc);
        }
    }

    private static PanchangEclipseDto BuildDto(
        int year,
        string kind,
        string type,
        DateTime startUtc,
        DateTime maximumUtc,
        DateTime endUtc,
        DateTime? sutakStartUtc,
        DateTime? sutakEndUtc,
        TimeZoneInfo timezoneInfo)
    {
        var localMaximum = TimeZoneInfo.ConvertTimeFromUtc(maximumUtc, timezoneInfo);
        return new PanchangEclipseDto
        {
            Id = $"{kind}-{year}-{localMaximum:yyyyMMddHHmm}",
            Kind = kind,
            Type = type,
            Date = DateOnly.FromDateTime(localMaximum),
            StartTime = new DateTimeOffset(TimeZoneInfo.ConvertTimeFromUtc(startUtc, timezoneInfo), timezoneInfo.GetUtcOffset(startUtc)),
            MaximumTime = new DateTimeOffset(localMaximum, timezoneInfo.GetUtcOffset(maximumUtc)),
            EndTime = new DateTimeOffset(TimeZoneInfo.ConvertTimeFromUtc(endUtc, timezoneInfo), timezoneInfo.GetUtcOffset(endUtc)),
            SutakStartTime = sutakStartUtc is null ? null : new DateTimeOffset(TimeZoneInfo.ConvertTimeFromUtc(sutakStartUtc.Value, timezoneInfo), timezoneInfo.GetUtcOffset(sutakStartUtc.Value)),
            SutakEndTime = sutakEndUtc is null ? null : new DateTimeOffset(TimeZoneInfo.ConvertTimeFromUtc(sutakEndUtc.Value, timezoneInfo), timezoneInfo.GetUtcOffset(sutakEndUtc.Value)),
            IsVisibleFromLocation = true
        };
    }

    private static LocalEclipseResult? FindSolarEclipse(DateTime searchStartUtc, MuhurthaLocation location)
    {
        var times = new double[10];
        var attr = new double[20];
        var geopos = new[] { location.Longitude, location.Latitude, 0d };
        var serr = new StringBuilder(256);
        var flags = swe_sol_eclipse_when_loc(ToJulian(searchStartUtc), SeFlgSwieph, geopos, times, attr, 0, serr);
        return flags <= 0 ? null : new LocalEclipseResult(flags, times);
    }

    private static LocalEclipseResult? FindLunarEclipse(DateTime searchStartUtc, MuhurthaLocation location)
    {
        var times = new double[10];
        var attr = new double[20];
        var geopos = new[] { location.Longitude, location.Latitude, 0d };
        var serr = new StringBuilder(256);
        var flags = swe_lun_eclipse_when_loc(ToJulian(searchStartUtc), SeFlgSwieph, geopos, times, attr, 0, serr);
        return flags <= 0 ? null : new LocalEclipseResult(flags, times);
    }

    private static TimeZoneInfo ResolveTimezone(string timezone)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezone);
        }
        catch
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        }
    }

    private static DateTime ConvertLocalToUtc(DateTime localTime, TimeZoneInfo timezoneInfo)
    {
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified), timezoneInfo);
    }

    private static DateTime AdvanceCursor(DateTime currentCursor, DateTime endUtc, DateTime maximumUtc)
    {
        var next = endUtc > currentCursor ? endUtc.AddMinutes(1) : maximumUtc.AddDays(1);
        return next <= currentCursor ? currentCursor.AddDays(1) : next;
    }

    private static DateTime FirstNonZero(params double[] julianDates)
    {
        foreach (var julianDate in julianDates)
        {
            if (julianDate > 0)
            {
                return FromJulian(julianDate);
            }
        }

        return DateTime.MinValue;
    }

    private static double ToJulian(DateTime utc)
    {
        var normalizedUtc = utc.Kind == DateTimeKind.Utc ? utc : utc.ToUniversalTime();
        return normalizedUtc.ToOADate() + 2415018.5;
    }

    private static DateTime FromJulian(double julianDate)
    {
        var ticks = checked((long)Math.Round((julianDate - 1721425.5) * TimeSpan.TicksPerDay));
        return new DateTime(ticks, DateTimeKind.Utc);
    }

    private static string ResolveSolarType(int flags)
    {
        if ((flags & SeEclAnnularTotal) != 0)
        {
            return "Annular Total";
        }

        if ((flags & SeEclTotal) != 0)
        {
            return "Total";
        }

        if ((flags & SeEclAnnular) != 0)
        {
            return "Annular";
        }

        return "Partial";
    }

    private static string ResolveLunarType(int flags)
    {
        if ((flags & SeEclTotal) != 0)
        {
            return "Total";
        }

        if ((flags & SeEclPartial) != 0)
        {
            return "Partial";
        }

        return (flags & SeEclPenumbral) != 0 ? "Penumbral" : "Unknown";
    }

    [DllImport("Native\\swedll64.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int swe_sol_eclipse_when_loc(
        double tjdStart,
        int ifl,
        double[] geopos,
        double[] tret,
        double[] attr,
        int backward,
        StringBuilder serr);

    [DllImport("Native\\swedll64.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern int swe_lun_eclipse_when_loc(
        double tjdStart,
        int ifl,
        double[] geopos,
        double[] tret,
        double[] attr,
        int backward,
        StringBuilder serr);

    private sealed record LocalEclipseResult(int Flags, double[] Times);
}
