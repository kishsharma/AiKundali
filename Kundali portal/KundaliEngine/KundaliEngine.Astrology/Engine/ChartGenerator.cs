using System;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Calculations;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.Astrology.Engine
{
    public class ChartGenerator
    {
        private readonly PlanetPositionCalculator _planetCalc;
        private readonly LagnaCalculator _lagnaCalc;

        public ChartGenerator()
        {
            _planetCalc = new PlanetPositionCalculator();
            _lagnaCalc = new LagnaCalculator();
        }

        public KundaliChart GenerateChart(BirthDetails birthDetails)
        {
            var chart = new KundaliChart
            {
                BirthDetails = birthDetails,
                GeneratedAtUtc = DateTime.UtcNow
            };

            Planet[] planets =
            {
                Planet.Sun,
                Planet.Moon,
                Planet.Mercury,
                Planet.Venus,
                Planet.Mars,
                Planet.Jupiter,
                Planet.Saturn,
                Planet.Rahu,
                Planet.Ketu
            };

            foreach (var planet in planets)
            {
                chart.PlanetPositions.Add(_planetCalc.Calculate(planet, DateTime.UtcNow));
            }

            int ascendantIndex = _lagnaCalc.CalculateLagna(0.0);
            chart.AscendantSign = (RashiZodiacSign)ascendantIndex;

            return chart;
        }
    }
}
