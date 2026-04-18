using KundaliEngine.Domain.Models;
using System;

// Responsibility: Calculates Ascendant
namespace KundaliEngine.Astrology.Calculations
{
    public class LagnaCalculator
    {
        // Simplified placeholder. Real implementation should compute ascendant based on latitude, longitude and sidereal time.
        public int CalculateLagna(double longitudeDegrees)
        {
            // Map longitude to Rashi index [0..11]
            int rashi = ((int)Math.Floor(longitudeDegrees / 30.0)) % 12;
            if (rashi < 0) rashi += 12;
            return rashi;
        }
    }
}
