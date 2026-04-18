using System;
using System.Collections.Generic;
using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models
{
    public class KundaliChart
    {
        public BirthDetails BirthDetails { get; set; } = new BirthDetails();
        public DateTime GeneratedAtUtc { get; set; }
        public RashiZodiacSign AscendantSign { get; set; }
        public int AscendantDegree { get; set; }
        public int AscendantMinute { get; set; }
        public List<PlanetPosition> PlanetPositions { get; set; } = new List<PlanetPosition>();
        public List<House> Houses { get; set; } = new List<House>();
        public List<Aspect> Aspects { get; set; } = new List<Aspect>();
        public List<DashaPeriod> DashaPeriods { get; set; } = new List<DashaPeriod>();
    }
}
