using KundaliEngine.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KundaliEngine.Domain.Rules
{
    public static class DashaRules
    {
        public static readonly List<(Planet Planet, int Years)> VimshottariOrder = new()
        {
            (Planet.Ketu, 7),
            (Planet.Venus, 20),
            (Planet.Sun, 6),
            (Planet.Moon, 10),
            (Planet.Mars, 7),
            (Planet.Rahu, 18),
            (Planet.Jupiter, 16),
            (Planet.Saturn, 19),
            (Planet.Mercury, 17)
        };
    }

    public static class NakshatraDashaMapper
    {
        public static Planet GetDashaLord(int nakshatraIndex)
        {
            return DashaRules.VimshottariOrder[nakshatraIndex % 9].Planet;
        }
    }
}
