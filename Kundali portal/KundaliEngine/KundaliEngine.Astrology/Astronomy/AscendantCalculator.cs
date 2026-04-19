using System;

namespace KundaliEngine.Astrology.Astronomy
{
    public class AscendantCalculator
    {
        public double Calculate(DateTime utc, double latitude, double longitude)
        {
            double julianDay = JulianDayCalculator.ToJulianDay(utc);
            return SwissEphWrapper.CalculateAscendant(julianDay, latitude, longitude);
        }
    }
}
