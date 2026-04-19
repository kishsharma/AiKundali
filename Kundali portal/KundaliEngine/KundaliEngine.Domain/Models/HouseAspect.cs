using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models
{
    public class HouseAspect
    {
        public Planet FromPlanet { get; set; }
        public int FromHouse { get; set; }
        public int ToHouse { get; set; }
        public int AspectOffset { get; set; }
    }
}
