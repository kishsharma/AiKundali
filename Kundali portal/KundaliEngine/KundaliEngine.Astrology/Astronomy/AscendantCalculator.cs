using System;

namespace KundaliEngine.Astrology.Astronomy
{
    public class AscendantCalculator
    {
        private readonly IEphemerisProvider _ephemerisProvider;

        public AscendantCalculator()
            : this(new SwissEphemerisProvider())
        {
        }

        public AscendantCalculator(IEphemerisProvider ephemerisProvider)
        {
            _ephemerisProvider = ephemerisProvider ?? throw new ArgumentNullException(nameof(ephemerisProvider));
        }

        public double Calculate(DateTime utc, double latitude, double longitude)
        {
            return _ephemerisProvider.GetAscendant(utc, new GeoLocation(latitude, longitude)).Longitude;
        }
    }
}
