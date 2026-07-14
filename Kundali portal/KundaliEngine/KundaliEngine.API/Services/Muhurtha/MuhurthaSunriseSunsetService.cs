using KundaliEngine.Domain.Models.Muhurtha;

namespace KundaliEngine.API.Services.Muhurtha;

public class MuhurthaSunriseSunsetService : IMuhurthaSunriseSunsetService
{
    public (DateTimeOffset Sunrise, DateTimeOffset Sunset) Calculate(
        DateOnly date,
        MuhurthaLocation location,
        TimeSpan offset)
    {
        var zenith = 90.833333;
        var sunrise = CalculateSolarEvent(date, location.Latitude, location.Longitude, offset, zenith, true);
        var sunset = CalculateSolarEvent(date, location.Latitude, location.Longitude, offset, zenith, false);

        return (sunrise, sunset);
    }

    private static DateTimeOffset CalculateSolarEvent(
        DateOnly date,
        double latitude,
        double longitude,
        TimeSpan offset,
        double zenith,
        bool isSunrise)
    {
        var dayOfYear = date.DayOfYear;
        var longitudeHour = longitude / 15.0;
        var approximateTime = isSunrise
            ? dayOfYear + ((6.0 - longitudeHour) / 24.0)
            : dayOfYear + ((18.0 - longitudeHour) / 24.0);

        var sunMeanAnomaly = (0.9856 * approximateTime) - 3.289;
        var trueLongitude = sunMeanAnomaly
            + (1.916 * Math.Sin(ToRadians(sunMeanAnomaly)))
            + (0.020 * Math.Sin(ToRadians(2 * sunMeanAnomaly)))
            + 282.634;
        trueLongitude = NormalizeDegrees(trueLongitude);

        var rightAscension = ToDegrees(Math.Atan(0.91764 * Math.Tan(ToRadians(trueLongitude))));
        rightAscension = NormalizeDegrees(rightAscension);

        var longitudeQuadrant = Math.Floor(trueLongitude / 90.0) * 90.0;
        var rightAscensionQuadrant = Math.Floor(rightAscension / 90.0) * 90.0;
        rightAscension = (rightAscension + longitudeQuadrant - rightAscensionQuadrant) / 15.0;

        var sinDeclination = 0.39782 * Math.Sin(ToRadians(trueLongitude));
        var cosDeclination = Math.Cos(Math.Asin(sinDeclination));
        var cosHourAngle = (Math.Cos(ToRadians(zenith)) - (sinDeclination * Math.Sin(ToRadians(latitude))))
            / (cosDeclination * Math.Cos(ToRadians(latitude)));

        if (cosHourAngle is > 1 or < -1)
        {
            var fallbackHour = isSunrise ? 6 : 18;
            return new DateTimeOffset(date.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(fallbackHour))), offset);
        }

        var hourAngle = isSunrise
            ? 360.0 - ToDegrees(Math.Acos(cosHourAngle))
            : ToDegrees(Math.Acos(cosHourAngle));
        hourAngle /= 15.0;

        var localMeanTime = hourAngle + rightAscension - (0.06571 * approximateTime) - 6.622;
        var utcHour = localMeanTime - longitudeHour;
        var localHour = NormalizeHours(utcHour + offset.TotalHours);

        var localDateTime = date.ToDateTime(TimeOnly.MinValue).AddHours(localHour);
        return new DateTimeOffset(localDateTime, offset);
    }

    private static double NormalizeDegrees(double degrees)
    {
        degrees %= 360.0;
        return degrees < 0 ? degrees + 360.0 : degrees;
    }

    private static double NormalizeHours(double hours)
    {
        hours %= 24.0;
        return hours < 0 ? hours + 24.0 : hours;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

    private static double ToDegrees(double radians) => radians * 180.0 / Math.PI;
}
