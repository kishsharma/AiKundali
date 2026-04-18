using System;

// Responsibility: Degree ? Rashi
namespace KundaliEngine.Astrology.Calculations
{
    public class RashiCalculator
    {
        public int GetRashiFromLongitude(double longitudeDegrees)
        {
            int rashi = ((int)Math.Floor(longitudeDegrees / 30.0)) % 12;
            if (rashi < 0) rashi += 12;
            return rashi;
        }
    }
}
