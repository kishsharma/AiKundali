using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models
{
    public class Aspect
    {
        public Planet FromPlanet { get; set; }
        public Planet ToPlanet { get; set; }
        public AspectType Type { get; set; }
        public double OrbDegrees { get; set; }
    }
}
