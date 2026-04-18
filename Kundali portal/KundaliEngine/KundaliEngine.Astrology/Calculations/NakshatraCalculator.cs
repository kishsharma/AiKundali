using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Astrology.Calculations
{
    public static class NakshatraCalculator
    {
        private const double NakshatraSpanDegrees = 360.0 / 27.0;
        private const double PadaSpanDegrees = NakshatraSpanDegrees / 4.0;

        public static (Nakshatra Nakshatra, int Pada) Calculate(double longitude)
        {
            double normalizedLongitude = NormalizeLongitude(longitude);
            int nakshatraIndex = (int)(normalizedLongitude / NakshatraSpanDegrees);
            double degreesWithinNakshatra = normalizedLongitude % NakshatraSpanDegrees;
            int pada = (int)(degreesWithinNakshatra / PadaSpanDegrees) + 1;

            if (pada > 4)
                pada = 4;

            return ((Nakshatra)nakshatraIndex, pada);
        }

        private static double NormalizeLongitude(double longitude)
        {
            longitude %= 360.0;
            if (longitude < 0)
                longitude += 360.0;

            return longitude;
        }
    }
}
