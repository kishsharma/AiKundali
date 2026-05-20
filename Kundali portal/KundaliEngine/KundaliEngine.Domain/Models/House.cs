using System.Collections.Generic;
using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models
{
    public class House
    {
        public HouseNumber HouseNumber { get; set; }
        public ZodiacSign Sign { get; set; }
        public List<Planet> Occupants { get; set; } = new List<Planet>();
    }
}
