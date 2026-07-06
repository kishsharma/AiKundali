using KundaliEngine.Domain.Enums;

namespace KundaliEngine.API.Helper_functions
{
    public static class HelperData
    {

        public static string GetSignOwner(ZodiacSign sign)
        {
            return sign switch
            {
                ZodiacSign.Aries => Planet.Mars.ToString(),
                ZodiacSign.Taurus => Planet.Venus.ToString(),
                ZodiacSign.Gemini => Planet.Mercury.ToString(),
                ZodiacSign.Cancer => Planet.Moon.ToString(),
                ZodiacSign.Leo => Planet.Sun.ToString(),
                ZodiacSign.Virgo => Planet.Mercury.ToString(),
                ZodiacSign.Libra => Planet.Venus.ToString(),
                ZodiacSign.Scorpio => Planet.Mars.ToString(),
                ZodiacSign.Sagittarius => Planet.Jupiter.ToString(),
                ZodiacSign.Capricorn => Planet.Saturn.ToString(),
                ZodiacSign.Aquarius => Planet.Saturn.ToString(),
                ZodiacSign.Pisces => Planet.Jupiter.ToString(),
                _ => string.Empty
            };
        }

        public static bool IsNaturalBenefic(Planet planet)
        {
            return planet is Planet.Moon or Planet.Venus or Planet.Jupiter;
        }

        public static bool IsNaturalMaleficOrShadow(Planet planet)
        {
            return planet is Planet.Sun or Planet.Mars or Planet.Saturn or Planet.Rahu or Planet.Ketu;
        }
    }
}
