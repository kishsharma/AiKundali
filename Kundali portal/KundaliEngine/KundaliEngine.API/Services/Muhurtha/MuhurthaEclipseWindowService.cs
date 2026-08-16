using System.Runtime.InteropServices;
using System.Text;
using KundaliEngine.API.Services.Muhurtha.Models;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models.Muhurtha;

namespace KundaliEngine.API.Services.Muhurtha;

public class MuhurthaEclipseWindowService : IMuhurthaEclipseWindowService
{
    private const int SeFlgSwieph = 2;
    private const int SeEclTotal = 4;
    private const int SeEclAnnular = 8;
    private const int SeEclPartial = 16;
    private const int SeEclAnnularTotal = 32;
    private const int SeEclPenumbral = 64;
    private static readonly TimeSpan SolarSutakDuration = TimeSpan.FromHours(12);
    private static readonly TimeSpan LunarSutakDuration = TimeSpan.FromHours(9);

    public IReadOnlyList<MuhurthaTimeWindow> BuildWindows(MuhurthaDayContext dayContext)
    {
        var windows = new List<MuhurthaTimeWindow>();
        var searchStartUtc = (dayContext.DayStart - SolarSutakDuration).UtcDateTime;
        var searchEndUtc = (dayContext.DayEnd + SolarSutakDuration).UtcDateTime;

        if (TryFindLocalSolarEclipse(searchStartUtc, searchEndUtc, dayContext.Location, dayContext.DayStart.Offset, out var solarEclipse))
        {
            windows.Add(BuildEclipseWindow(
                "solar_eclipse",
                "Solar Eclipse",
                solarEclipse,
                MuhurthaRuleSeverity.HardBlock,
                -40,
                "The slot overlaps a locally visible solar eclipse.",
                "Avoid auspicious Muhurtha during the solar eclipse window."));

            windows.Add(BuildEclipseWindow(
                "solar_eclipse_sutak",
                "Solar Eclipse Sutak",
                solarEclipse with { StartUtc = solarEclipse.StartUtc - SolarSutakDuration },
                MuhurthaRuleSeverity.HardBlock,
                -28,
                "The slot overlaps the Sutak period of a locally visible solar eclipse.",
                "Prefer a slot outside the solar eclipse Sutak period."));
        }

        if (TryFindLocalLunarEclipse(searchStartUtc, searchEndUtc, dayContext.Location, dayContext.DayStart.Offset, out var lunarEclipse))
        {
            windows.Add(BuildEclipseWindow(
                "lunar_eclipse",
                "Lunar Eclipse",
                lunarEclipse,
                MuhurthaRuleSeverity.HardBlock,
                -34,
                "The slot overlaps a locally visible lunar eclipse.",
                "Avoid auspicious Muhurtha during the lunar eclipse window."));

            windows.Add(BuildEclipseWindow(
                "lunar_eclipse_sutak",
                "Lunar Eclipse Sutak",
                lunarEclipse with { StartUtc = lunarEclipse.StartUtc - LunarSutakDuration },
                MuhurthaRuleSeverity.HardBlock,
                -24,
                "The slot overlaps the Sutak period of a locally visible lunar eclipse.",
                "Prefer a slot outside the lunar eclipse Sutak period."));
        }

        return windows;
    }

    private static MuhurthaTimeWindow BuildEclipseWindow(
        string ruleKey,
        string displayName,
        EclipseWindow eclipse,
        MuhurthaRuleSeverity severity,
        int scoreImpact,
        string summary,
        string recommendation)
    {
        var window = new MuhurthaTimeWindow
        {
            RuleKey = ruleKey,
            DisplayName = displayName,
            StartTime = new DateTimeOffset(eclipse.StartUtc, TimeSpan.Zero).ToOffset(eclipse.Offset),
            EndTime = new DateTimeOffset(eclipse.EndUtc, TimeSpan.Zero).ToOffset(eclipse.Offset),
            DefaultSeverity = severity,
            DefaultScoreImpact = scoreImpact,
            Summary = summary,
            Recommendation = recommendation
        };

        window.Metadata["eclipseType"] = eclipse.Type;
        window.Metadata["maximumTime"] = new DateTimeOffset(eclipse.MaximumUtc, TimeSpan.Zero).ToOffset(eclipse.Offset).ToString("yyyy-MM-dd HH:mm");
        return window;
    }

    private static bool TryFindLocalSolarEclipse(
        DateTime searchStartUtc,
        DateTime searchEndUtc,
        MuhurthaLocation location,
        TimeSpan offset,
        out EclipseWindow eclipse)
    {
        eclipse = default!;
        var result = FindSolarEclipse(searchStartUtc, location);
        if (result is null)
        {
            return false;
        }

        if ((result.Flags & (SeEclTotal | SeEclAnnular | SeEclPartial | SeEclAnnularTotal)) == 0)
        {
            return false;
        }

        var startUtc = FirstNonZero(result.Times[1], result.Times[0]);
        var endUtc = FirstNonZero(result.Times[4], result.Times[0]);
        if (endUtc < searchStartUtc || startUtc > searchEndUtc)
        {
            return false;
        }

        eclipse = new EclipseWindow(
            ResolveSolarType(result.Flags),
            startUtc,
            endUtc,
            FromJulian(result.Times[0]),
            offset);
        return true;
    }

    private static bool TryFindLocalLunarEclipse(
        DateTime searchStartUtc,
        DateTime searchEndUtc,
        MuhurthaLocation location,
        TimeSpan offset,
        out EclipseWindow eclipse)
    {
        eclipse = default!;
        var result = FindLunarEclipse(searchStartUtc, location);
        if (result is null)
        {
            return false;
        }

        if ((result.Flags & (SeEclTotal | SeEclPartial)) == 0)
        {
            return false;
        }

        var startUtc = FirstNonZero(result.Times[2], result.Times[6], result.Times[0]);
        var endUtc = FirstNonZero(result.Times[3], result.Times[7], result.Times[0]);
        if (endUtc < searchStartUtc || startUtc > searchEndUtc)
        {
            return false;
        }

        eclipse = new EclipseWindow(
            ResolveLunarType(result.Flags),
            startUtc,
            endUtc,
            FromJulian(result.Times[0]),
            offset);
        return true;
    }

    private static LocalEclipseResult? FindSolarEclipse(DateTime searchStartUtc, MuhurthaLocation location)
    {
        var times = new double[10];
        var attr = new double[20];
        var geopos = new[] { location.Longitude, location.Latitude, 0d };
        var serr = new StringBuilder(256);
        var flags = swe_sol_eclipse_when_loc(ToJulian(searchStartUtc), SeFlgSwieph, geopos, times, attr, 0, serr);
        return flags <= 0 ? null : new LocalEclipseResult(flags, times, attr);
    }

    private static LocalEclipseResult? FindLunarEclipse(DateTime searchStartUtc, MuhurthaLocation location)
    {
        var times = new double[10];
        var attr = new double[20];
        var geopos = new[] { location.Longitude, location.Latitude, 0d };
        var serr = new StringBuilder(256);
        var flags = swe_lun_eclipse_when_loc(ToJulian(searchStartUtc), SeFlgSwieph, geopos, times, attr, 0, serr);
        return flags <= 0 ? null : new LocalEclipseResult(flags, times, attr);
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
            return "AnnularTotal";
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

    private sealed record LocalEclipseResult(int Flags, double[] Times, double[] Attributes);

    private sealed record EclipseWindow(
        string Type,
        DateTime StartUtc,
        DateTime EndUtc,
        DateTime MaximumUtc,
        TimeSpan Offset);
}
