using KundaliEngine.Domain.Enums;
using System;

namespace KundaliEngine.Domain.Models
{
    public class PratyantardashaPeriod
    {
        public Planet MainPlanet { get; set; }
        public Planet SubPlanet { get; set; }
        public Planet InnerPlanet { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
