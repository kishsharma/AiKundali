using KundaliEngine.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KundaliEngine.Domain.Rules
{
    public static class PlanetStrengthRules
    {
            public static readonly Dictionary<Planet, ZodiacSign> Exaltation = new()
        {
            { Planet.Sun, ZodiacSign.Aries },
            { Planet.Moon, ZodiacSign.Taurus },
            { Planet.Mars, ZodiacSign.Capricorn },
            { Planet.Mercury, ZodiacSign.Virgo },
            { Planet.Jupiter, ZodiacSign.Cancer },
            { Planet.Venus, ZodiacSign.Pisces },
            { Planet.Saturn, ZodiacSign.Libra }
        };

            public static readonly Dictionary<Planet, ZodiacSign> Debilitation = new()
        {
            { Planet.Sun, ZodiacSign.Libra },
            { Planet.Moon, ZodiacSign.Scorpio },
            { Planet.Mars, ZodiacSign.Cancer },
            { Planet.Mercury, ZodiacSign.Pisces },
            { Planet.Jupiter, ZodiacSign.Capricorn },
            { Planet.Venus, ZodiacSign.Virgo },
            { Planet.Saturn, ZodiacSign.Aries }
        };

            public static readonly Dictionary<Planet, List<ZodiacSign>> OwnSigns = new()
        {
            { Planet.Sun, new() { ZodiacSign.Leo } },
            { Planet.Moon, new() { ZodiacSign.Cancer } },
            { Planet.Mars, new() { ZodiacSign.Aries, ZodiacSign.Scorpio } },
            { Planet.Mercury, new() { ZodiacSign.Gemini, ZodiacSign.Virgo } },
            { Planet.Jupiter, new() { ZodiacSign.Sagittarius, ZodiacSign.Pisces } },
            { Planet.Venus, new() { ZodiacSign.Taurus, ZodiacSign.Libra } },
            { Planet.Saturn, new() { ZodiacSign.Capricorn, ZodiacSign.Aquarius } }
        };

        public static readonly Dictionary<Planet, double> ExaltationDegrees = new()
        {
            { Planet.Sun, 10 },
            { Planet.Moon, 3 },
            { Planet.Mars, 28 },
            { Planet.Mercury, 15 },
            { Planet.Jupiter, 5 },
            { Planet.Venus, 27 },
            { Planet.Saturn, 20 }
        };
    }
}
