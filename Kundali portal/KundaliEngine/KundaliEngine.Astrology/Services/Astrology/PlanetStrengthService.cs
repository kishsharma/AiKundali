using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KundaliEngine.Astrology.Services.Astrology
{
    public class PlanetStrengthService
    {
        public PlanetStrength GetStrength(Planet planet, ZodiacSign sign)
        {
            if (planet == Planet.Rahu || planet == Planet.Ketu)
                return PlanetStrength.NotApplicable;

            if (PlanetStrengthRules.Exaltation.TryGetValue(planet, out var exaltedSign)
                && exaltedSign == sign)
            {
                return PlanetStrength.Exalted;
            }

            if (PlanetStrengthRules.Debilitation.TryGetValue(planet, out var debilitatedSign)
                && debilitatedSign == sign)
            {
                return PlanetStrength.Debilitated;
            }

            if (PlanetStrengthRules.OwnSigns.TryGetValue(planet, out var ownSigns)
                && ownSigns.Contains(sign))
            {
                return PlanetStrength.OwnSign;
            }

            return PlanetStrength.Neutral;
        }

        public string GetDetailedStrength(Planet planet, ZodiacSign sign, double degree)
        {
            var basic = GetStrength(planet, sign);

            if (basic == PlanetStrength.NotApplicable)
                return basic.ToString();

            if (basic == PlanetStrength.Exalted &&
                PlanetStrengthRules.ExaltationDegrees.TryGetValue(planet, out var peak))
            {
                double diff = Math.Abs(degree - peak);

                if (diff <= 5)
                    return "Exalted (Strong)";
                else
                    return "Exalted (Weak)";
            }

            return basic.ToString();
        }

        public bool IsCombust(Planet planet, double planetLongitude, double sunLongitude)
        {
            if (planet == Planet.Sun || planet == Planet.Rahu || planet == Planet.Ketu)
                return false;

            double diff = Math.Abs(planetLongitude - sunLongitude);

            var thresholds = new Dictionary<Planet, double>
            {
                { Planet.Mercury, 14 },
                { Planet.Venus, 10 },
                { Planet.Mars, 17 },
                { Planet.Jupiter, 11 },
                { Planet.Saturn, 15 }
            };

            if (!thresholds.ContainsKey(planet))
                return false;

            return diff <= thresholds[planet];
        }

        public string GetDigbala(Planet planet, HouseNumber house)
        {
            return planet switch
            {
                Planet.Sun or Planet.Mars =>
                    house == HouseNumber.Tenth ? "Strong" : "Weak",

                Planet.Moon or Planet.Venus =>
                    house == HouseNumber.Fourth ? "Strong" : "Weak",

                Planet.Jupiter or Planet.Mercury =>
                    house == HouseNumber.First ? "Strong" : "Weak",

                Planet.Saturn =>
                    house == HouseNumber.Seventh ? "Strong" : "Weak",

                Planet.Rahu or Planet.Ketu =>
                    "NotApplicable",

                _ => "Neutral"
            };
        }
    }
}
