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
        private readonly IEphemerisProvider _ephemerisProvider;
        private readonly HouseService _houseService;
        private readonly AspectService _aspectService;
        private readonly PlanetStrengthService _strengthService;

        public ChartService(IEphemerisProvider ephemerisProvider)
        {
            _ephemerisProvider = ephemerisProvider ?? throw new ArgumentNullException(nameof(ephemerisProvider));
            _houseService = new HouseService();
            _aspectService = new AspectService();
            _strengthService = new PlanetStrengthService();
        }

        public ChartService()
            : this(new SwissEphemerisProvider())
        {
        }

        public List<PlanetPosition> GenerateChart(DateTime birthUtc, double lat, double lon)
        {
            var location = new GeoLocation(lat, lon);
            var planetPositions = _ephemerisProvider.GetAllPlanets(birthUtc, location).ToList();

            // Ascendant
            double asc = _ephemerisProvider.GetAscendant(birthUtc, location).Longitude;

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
