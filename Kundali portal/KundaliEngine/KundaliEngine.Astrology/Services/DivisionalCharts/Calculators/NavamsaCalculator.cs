using KundaliEngine.Astrology.Services.DivisionalCharts.Interfaces;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.Astrology.Services.DivisionalCharts.Calculators
{
    public class NavamsaCalculator :IDivisionalChartCalculator
    {
        public List<PlanetPosition> Calculate(List<PlanetPosition> d1Planets)
        {
            var d9Planets = new List<PlanetPosition>();

            foreach (var planet in d1Planets)
            {
                var d9Sign = CalculateNavamsaSign(
                    planet.Sign,
                    planet.Degree);

                var d9Planet = new PlanetPosition
                {
                    Planet = planet.Planet,

                    Longitude = planet.Longitude,

                    Latitude = planet.Latitude,

                    Degree = planet.Degree,

                    Minute = planet.Minute,

                    Nakshatra = planet.Nakshatra,

                    Pada = planet.Pada,

                    IsRetrograde = planet.IsRetrograde,

                    Sign = d9Sign
                };

                d9Planets.Add(d9Planet);
            }

            return d9Planets;
        }

        private ZodiacSign CalculateNavamsaSign(
            ZodiacSign d1Sign,
            double degreeInSign)
        {
            // Each Navamsa = 3°20'
            double navamsaSize = 3.333333333;

            int navamsaIndex =
                (int)(degreeInSign / navamsaSize);

            int baseSignIndex =
                ((int)d1Sign - 1);

            int d9SignIndex =
                (baseSignIndex * 9 + navamsaIndex) % 12;

            return (ZodiacSign)(d9SignIndex + 1);
        }
    }
}
