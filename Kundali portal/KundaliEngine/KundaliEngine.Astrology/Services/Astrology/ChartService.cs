using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KundaliEngine.Astrology.Services.Astrology
{
    public class ChartService
    {
        private readonly PlanetPositionCalculator _calculator;
        private readonly HouseService _houseService;
        private readonly AspectService _aspectService;
        private readonly PlanetStrengthService _strengthService;

        public ChartService()
        {
            _calculator = new PlanetPositionCalculator();
            _houseService = new HouseService();
            _aspectService = new AspectService();
            _strengthService = new PlanetStrengthService();
        }

        public List<PlanetPosition> GenerateChart(DateTime birthUtc, double lat, double lon)
        {
            Planet[] planets =
            {
            Planet.Sun,
            Planet.Moon,
            Planet.Mars,
            Planet.Mercury,
            Planet.Jupiter,
            Planet.Venus,
            Planet.Saturn,
            Planet.Rahu,
            Planet.Ketu
           };

            var planetPositions = new List<PlanetPosition>();

            foreach (var planet in planets)
            {
                var result = _calculator.Calculate(planet, birthUtc);
                planetPositions.Add(result);
            }

            // Ascendant
            var ascCalculator = new AscendantCalculator();
            double asc = ascCalculator.Calculate(birthUtc, lat, lon);

            // Houses
            _houseService.AssignWholeSignHouses(asc, planetPositions);

            // 🔥 STEP 5 GOES HERE 🔥

            var sun = planetPositions.First(p => p.Planet == Planet.Sun);

            foreach (var planet in planetPositions)
            {
                planet.DetailedStrength = _strengthService.GetDetailedStrength(
                    planet.Planet,
                    planet.Sign,
                    planet.Degree
                );

                planet.IsCombust = _strengthService.IsCombust(
                    planet.Planet,
                    planet.Longitude,
                    sun.Longitude
                );
            }

            return planetPositions;
        }
    }
}
