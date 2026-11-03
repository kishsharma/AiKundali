using System.Globalization;

namespace KundaliEngine.API.Services;

public static class PractitionerAppointmentTimeResolver
{
    private static readonly string[] LocalFormats = ["yyyy-MM-dd'T'HH:mm", "yyyy-MM-dd'T'HH:mm:ss"];

    public static bool TryResolve(string startsAtLocal, string endsAtLocal, string timeZoneId, out DateTime startsAtUtc, out DateTime endsAtUtc, out string? error)
    {
        startsAtUtc = default;
        endsAtUtc = default;
        error = null;
        if (string.IsNullOrWhiteSpace(timeZoneId)) { error = "Time zone is required."; return false; }
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId.Trim(), out var zone)) { error = "Choose a valid time zone."; return false; }
        if (!DateTime.TryParseExact(startsAtLocal, LocalFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var startLocal)
            || !DateTime.TryParseExact(endsAtLocal, LocalFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var endLocal))
        { error = "Enter a valid start and end time."; return false; }
        startLocal = DateTime.SpecifyKind(startLocal, DateTimeKind.Unspecified);
        endLocal = DateTime.SpecifyKind(endLocal, DateTimeKind.Unspecified);
        if (endLocal <= startLocal) { error = "End time must be after start time."; return false; }
        if (zone.IsInvalidTime(startLocal) || zone.IsInvalidTime(endLocal))
        { error = "This time does not exist because of a daylight-saving time change."; return false; }
        startsAtUtc = TimeZoneInfo.ConvertTimeToUtc(startLocal, zone);
        endsAtUtc = TimeZoneInfo.ConvertTimeToUtc(endLocal, zone);
        if (endsAtUtc <= startsAtUtc) { error = "The selected time window is invalid in this time zone."; return false; }
        return true;
    }
}
