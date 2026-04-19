using System;
using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Astrology.Calculations
{
    public static class ZodiacCalculator
    {
        public static (ZodiacSign Sign, int Degree, int Minute) Calculate(double longitude)
        {
            double normalizedLongitude = NormalizeLongitude(longitude);
            int index = (int)(normalizedLongitude / 30.0);
            ZodiacSign sign = (ZodiacSign)(index + 1);
            double degreeInSign = normalizedLongitude - (index * 30.0);

            int degree = (int)degreeInSign;
            int minute = (int)((degreeInSign - degree) * 60.0);

            return (sign, degree, minute);
        }

        public static ZodiacSign GetSign(double longitude)
        {
            double normalizedLongitude = NormalizeLongitude(longitude);
            int index = (int)(normalizedLongitude / 30.0);
            return (ZodiacSign)(index + 1);
        }

        public static double GetDegreeInSign(double longitude)
        {
            double normalizedLongitude = NormalizeLongitude(longitude);
            int index = (int)(normalizedLongitude / 30.0);
            return normalizedLongitude - (index * 30.0);
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
