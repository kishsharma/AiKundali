namespace KundaliEngine.API.Services.Muhurtha.GrahaPravesha;

public static class GrahaPraveshaTime
{
    public static TimeZoneInfo Resolve(string timezone)
    {
        foreach (var candidate in Candidates(timezone))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(candidate);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
    }

    public static TimeSpan GetOffset(string timezone, DateOnly date)
    {
        var zone = Resolve(timezone);
        var noon = date.ToDateTime(new TimeOnly(12, 0));
        return zone.GetUtcOffset(DateTime.SpecifyKind(noon, DateTimeKind.Unspecified));
    }

    private static IEnumerable<string> Candidates(string timezone)
    {
        if (!string.IsNullOrWhiteSpace(timezone))
        {
            yield return timezone.Trim();
        }

        if (string.Equals(timezone, "Asia/Kolkata", StringComparison.OrdinalIgnoreCase))
        {
            yield return "India Standard Time";
        }

        if (string.Equals(timezone, "Europe/London", StringComparison.OrdinalIgnoreCase))
        {
            yield return "GMT Standard Time";
        }

        if (string.Equals(timezone, "America/New_York", StringComparison.OrdinalIgnoreCase))
        {
            yield return "Eastern Standard Time";
        }
    }
}
