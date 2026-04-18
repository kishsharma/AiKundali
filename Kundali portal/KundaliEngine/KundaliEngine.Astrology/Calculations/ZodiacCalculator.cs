using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Astrology.Calculations
{
    public static class ZodiacCalculator
    {
        public static (RashiZodiacSign Sign, int Degree, int Minute) Calculate(double longitude)
        {
            double normalizedLongitude = NormalizeLongitude(longitude);
            int signIndex = (int)(normalizedLongitude / 30.0);
            double degreeInSign = normalizedLongitude % 30.0;

            int degrees = (int)degreeInSign;
            int minutes = (int)((degreeInSign - degrees) * 60);

            return ((RashiZodiacSign)signIndex, degrees, minutes);
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
