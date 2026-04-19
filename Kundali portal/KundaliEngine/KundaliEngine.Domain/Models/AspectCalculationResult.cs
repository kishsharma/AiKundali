using System.Collections.Generic;

namespace KundaliEngine.Domain.Models
{
    public class AspectCalculationResult
    {
        public List<Aspect> PlanetAspects { get; set; } = new List<Aspect>();
        public List<HouseAspect> HouseAspects { get; set; } = new List<HouseAspect>();
    }
}
