using KundaliEngine.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KundaliEngine.Astrology.Calculations
{
    public static class CombustionCalculator
    {
        public static bool IsCombust(Planet planet, double planetLon, double sunLon)
        {
            double diff = Math.Abs(planetLon - sunLon);
            diff = Math.Min(diff, 360 - diff);

            double limit = planet switch
            {
                Planet.Mercury => 14,
                Planet.Venus => 10,
                Planet.Mars => 17,
                Planet.Jupiter => 11,
                Planet.Saturn => 15,
                _ => 0
            };

            return limit > 0 && diff <= limit;
        }
    }
}
