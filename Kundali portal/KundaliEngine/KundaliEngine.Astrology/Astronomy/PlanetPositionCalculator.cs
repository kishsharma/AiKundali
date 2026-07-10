using System;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.Astrology.Astronomy
{
    public class PlanetPositionCalculator
    {
        private readonly IEphemerisProvider _ephemerisProvider;

        public PlanetPositionCalculator()
            : this(new SwissEphemerisProvider())
        {
        }

        public PlanetPositionCalculator(IEphemerisProvider ephemerisProvider)
        {
            _ephemerisProvider = ephemerisProvider ?? throw new ArgumentNullException(nameof(ephemerisProvider));
        }

        public PlanetPosition Calculate(Planet planet, DateTime utc)
        {
            return _ephemerisProvider.GetPlanet(planet, utc, new GeoLocation(0, 0));
        }

        public PlanetPosition Calculate(string planetName, DateTime utc)
        {
            Planet planet = Enum.Parse<Planet>(planetName, ignoreCase: true);
            return Calculate(planet, utc);
        }
    }
}
