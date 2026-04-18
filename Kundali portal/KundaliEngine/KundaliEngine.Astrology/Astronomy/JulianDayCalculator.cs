using System;

// Responsibility: Converts datetime- JD
namespace KundaliEngine.Astrology.Astronomy
{
    public static class JulianDayCalculator
    {
        public static double ToJulianDay(DateTime dateTimeUtc)
        {
            int year = dateTimeUtc.Year;
            int month = dateTimeUtc.Month;
            double day = dateTimeUtc.Day +
                         dateTimeUtc.Hour / 24.0 +
                         dateTimeUtc.Minute / 1440.0 +
                         dateTimeUtc.Second / 86400.0;

            if (month <= 2)
            {
                year -= 1;
                month += 12;
            }

            int A = year / 100;
            int B = 2 - A + (A / 4);

            double jd = Math.Floor(365.25 * (year + 4716))
                        + Math.Floor(30.6001 * (month + 1))
                        + day + B - 1524.5;

            return jd;
        }
    }
}
