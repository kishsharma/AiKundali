using System;
using System.Collections.Generic;
using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models
{
    public class KundaliChart
    {
        public BirthDetails BirthDetails { get; set; } = new BirthDetails();
        public DateTime GeneratedAtUtc { get; set; }
        public ZodiacSign AscendantSign { get; set; }
        public string AscendantSignAndNumber { get; set; }
        public int AscendantDegree { get; set; }
        public int AscendantMinute { get; set; }
        public List<PlanetPosition> PlanetPositions { get; set; } = new List<PlanetPosition>();
        public List<HouseDto> Houses { get; set; } = new List<HouseDto>();
        public List<Aspect> Aspects { get; set; } = new List<Aspect>();
        public List<DashaPeriod> DashaPeriods { get; set; } = new List<DashaPeriod>();
        public PanchangDetails Panchang { get; set; } = new PanchangDetails();
    }
}
