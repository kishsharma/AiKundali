using System;

// Responsibility: Assign house numbers
namespace KundaliEngine.Astrology.Calculations
{
    public class HouseCalculator
    {
        // Placeholder: returns house number (1..12) for a given planet longitude and ascendant.
        public int GetHouse(double planetLongitudeDegrees, int ascendantRashi)
        {
            // Convert ascendant rashi to degrees
            double ascendantDegrees = ascendantRashi * 30.0;
            double diff = planetLongitudeDegrees - ascendantDegrees;
            diff = (diff % 360 + 360) % 360;
            int house = (int)Math.Floor(diff / 30.0) + 1;
            return house;
        }
    }
}
