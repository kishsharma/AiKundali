using System;
using System.Linq;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.Astrology.Services.Astrology
{
    public class AspectService
    {
        public AspectCalculationResult CalculateGrahaDrishti(List<PlanetPosition> planets)
        {
            if (planets is null)
                throw new ArgumentNullException(nameof(planets));

            var result = new AspectCalculationResult();

            foreach (var planet in planets)
            {
                int currentHouse = (int)planet.House;
                var offsets = GetOffsets(planet.Planet);

                foreach (var offset in offsets)
                {
                    int targetHouse = GetTargetHouse(currentHouse, offset);
                    HouseNumber targetHouseNumber = (HouseNumber)targetHouse;

                    result.HouseAspects.Add(new HouseAspect
                    {
                        FromPlanet = planet.Planet,
                        FromHouse = currentHouse,
                        ToHouse = targetHouse,
                        AspectOffset = offset
                    });

                    var targets = planets.Where(p => p.House == targetHouseNumber && p.Planet != planet.Planet);

                    foreach (var target in targets)
                    {
                        result.PlanetAspects.Add(new Aspect
                        {
                            FromPlanet = planet.Planet,
                            ToPlanet = target.Planet,
                            Type = (AspectType)offset,
                            AspectOffset = offset,
                            TargetHouse = targetHouse
                        });
                    }
                }
            }

            return result;
        }

        public int GetTargetHouse(int currentHouse, int offset)
        {
            int result = (currentHouse + offset - 1) % 12;
            return result == 0 ? 12 : result;
        }

        private static int[] GetOffsets(Planet planet)
        {
            return planet switch
            {
                Planet.Mars => new[] { 4, 7, 8 },
                Planet.Jupiter => new[] { 5, 7, 9 },
                Planet.Saturn => new[] { 3, 7, 10 },
                Planet.Rahu => new[] { 7 },
                Planet.Ketu => new[] { 7 },
                _ => new[] { 7 }
            };
        }
    }
}
