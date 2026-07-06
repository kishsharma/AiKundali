using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.Domain.Rules;
using KundaliHouse = KundaliEngine.Domain.Models.HouseDto;

namespace KundaliEngine.Astrology.Services.KundaliYoga
{
    public class YogaEvaluationContext
    {
        private readonly Dictionary<Planet, PlanetPosition> _planets;
        private readonly Dictionary<HouseNumber, KundaliHouse> _houses;
        private readonly AspectCalculationResult _aspects;
        private readonly PlanetStrengthService _planetStrengthService;

        public YogaEvaluationContext(
            IEnumerable<PlanetPosition> planets,
            IEnumerable<KundaliHouse> houses,
            AspectService aspectService,
            PlanetStrengthService planetStrengthService)
        {
            if (planets is null)
            {
                throw new ArgumentNullException(nameof(planets));
            }

            if (houses is null)
            {
                throw new ArgumentNullException(nameof(houses));
            }

            _planetStrengthService = planetStrengthService ?? throw new ArgumentNullException(nameof(planetStrengthService));

            var planetList = planets.ToList();
            _planets = planetList
                .GroupBy(position => position.Planet)
                .ToDictionary(group => group.Key, group => group.First());

            _houses = houses
                .GroupBy(house => house.HouseNumber)
                .ToDictionary(group => group.Key, group => group.First());

            _aspects = (aspectService ?? throw new ArgumentNullException(nameof(aspectService)))
                .CalculateGrahaDrishti(planetList);
        }

        public PlanetPosition? GetPlanet(Planet planet)
        {
            return _planets.TryGetValue(planet, out var position) ? position : null;
        }

        public KundaliHouse? GetHouse(HouseNumber house)
        {
            return _houses.TryGetValue(house, out var result) ? result : null;
        }

        public KundaliHouse? GetHouse(int house)
        {
            return Enum.IsDefined(typeof(HouseNumber), house)
                ? GetHouse((HouseNumber)house)
                : null;
        }

        public bool IsConjunction(Planet first, Planet second)
        {
            var firstPosition = GetPlanet(first);
            var secondPosition = GetPlanet(second);

            return firstPosition != null
                && secondPosition != null
                && firstPosition.House == secondPosition.House;
        }

        public bool IsAspect(Planet fromPlanet, Planet toPlanet)
        {
            return _aspects.PlanetAspects.Any(aspect =>
                aspect.FromPlanet == fromPlanet && aspect.ToPlanet == toPlanet);
        }

        public bool IsOwnSign(Planet planet)
        {
            var position = GetPlanet(planet);
            return position != null
                && PlanetStrengthRules.OwnSigns.TryGetValue(planet, out var ownSigns)
                && ownSigns.Contains(position.Sign);
        }

        public bool IsExalted(Planet planet)
        {
            var position = GetPlanet(planet);
            return position != null
                && PlanetStrengthRules.Exaltation.TryGetValue(planet, out var sign)
                && position.Sign == sign;
        }

        public bool IsDebilitated(Planet planet)
        {
            var position = GetPlanet(planet);
            return position != null
                && PlanetStrengthRules.Debilitation.TryGetValue(planet, out var sign)
                && position.Sign == sign;
        }

        public bool IsKendra(int house)
        {
            return house is 1 or 4 or 7 or 10;
        }

        public bool IsTrikona(int house)
        {
            return house is 1 or 5 or 9;
        }

        public bool IsDusthana(int house)
        {
            return house is 6 or 8 or 12;
        }

        public bool IsUpachaya(int house)
        {
            return house is 3 or 6 or 10 or 11;
        }

        public bool IsPlanetStrong(Planet planet)
        {
            var position = GetPlanet(planet);
            return position != null
                && _planetStrengthService.GetStrength(planet, position.Sign)
                    is PlanetStrength.Exalted or PlanetStrength.OwnSign;
        }

        public bool IsKendraFromMoon(Planet planet)
        {
            var moon = GetPlanet(Planet.Moon);
            var target = GetPlanet(planet);

            return moon != null
                && target != null
                && IsKendra(DistanceBetweenHouses(moon.House, target.House));
        }

        public Planet? HouseLord(HouseNumber house)
        {
            var houseInfo = GetHouse(house);
            return houseInfo == null ? null : SignLord(houseInfo.Sign);
        }

        public Planet? SignLordOf(ZodiacSign sign)
        {
            return SignLord(sign);
        }

        public IReadOnlyList<PlanetPosition> GetPlanetsInHouse(HouseNumber house)
        {
            return _planets.Values
                .Where(position => position.House == house)
                .ToList();
        }

        public IReadOnlyList<PlanetPosition> GetAllPlanets()
        {
            return _planets.Values.ToList();
        }

        public bool PlanetInHouse(Planet planet, HouseNumber house)
        {
            return GetPlanet(planet)?.House == house;
        }

        public bool PlanetInSign(Planet planet, ZodiacSign sign)
        {
            return GetPlanet(planet)?.Sign == sign;
        }

        public int DistanceBetweenHouses(HouseNumber fromHouse, HouseNumber toHouse)
        {
            return DistanceBetweenHouses((int)fromHouse, (int)toHouse);
        }

        public int DistanceBetweenHouses(int fromHouse, int toHouse)
        {
            return GetInclusiveDistance(fromHouse, toHouse);
        }

        public int DistanceBetweenSigns(ZodiacSign fromSign, ZodiacSign toSign)
        {
            return DistanceBetweenSigns((int)fromSign, (int)toSign);
        }

        public int DistanceBetweenSigns(int fromSign, int toSign)
        {
            return GetInclusiveDistance(fromSign, toSign);
        }

        public HouseNumber HouseFrom(HouseNumber fromHouse, int inclusiveDistance)
        {
            int target = ((int)fromHouse + inclusiveDistance - 2) % 12 + 1;
            return (HouseNumber)target;
        }

        public bool IsNaturalBenefic(Planet planet)
        {
            return planet is Planet.Moon or Planet.Mercury or Planet.Venus or Planet.Jupiter;
        }

        public bool IsNaturalMaleficOrShadow(Planet planet)
        {
            return planet is Planet.Sun or Planet.Mars or Planet.Saturn or Planet.Rahu or Planet.Ketu;
        }

        public bool IsQualifyingMoonSidePlanet(Planet planet)
        {
            return planet is not Planet.Sun and not Planet.Rahu and not Planet.Ketu;
        }

        public string CalculateYogaStrength(IEnumerable<Planet> involvedPlanets)
        {
            int strong = 0;
            int weak = 0;

            foreach (var planet in involvedPlanets.Distinct())
            {
                var position = GetPlanet(planet);

                if (position == null)
                {
                    continue;
                }

                var strength = _planetStrengthService.GetStrength(planet, position.Sign);

                if (strength is PlanetStrength.Exalted or PlanetStrength.OwnSign)
                {
                    strong++;
                }
                else if (strength == PlanetStrength.Debilitated || position.IsCombust)
                {
                    weak++;
                }
            }

            if (strong > weak && strong > 0)
            {
                return "Strong";
            }

            if (weak > strong)
            {
                return "Weak";
            }

            return "Neutral";
        }

        public YogaRuleMatch CreateMatch(
            string key,
            IEnumerable<Planet> involvedPlanets,
            IEnumerable<HouseNumber> involvedHouses,
            string reason)
        {
            var planets = involvedPlanets.Distinct().ToList();

            return new YogaRuleMatch
            {
                Key = key,
                Strength = CalculateYogaStrength(planets),
                InvolvedPlanets = planets.Select(planet => planet.ToString()).ToList(),
                InvolvedHouses = involvedHouses
                    .Distinct()
                    .Select(house => (int)house)
                    .OrderBy(house => house)
                    .ToList(),
                Reason = reason
            };
        }

        private static int GetInclusiveDistance(int from, int to)
        {
            return ((to - from + 12) % 12) + 1;
        }

        private static Planet? SignLord(ZodiacSign sign)
        {
            foreach (var rule in PlanetStrengthRules.OwnSigns)
            {
                if (rule.Value.Contains(sign))
                {
                    return rule.Key;
                }
            }

            return null;
        }
    }
}
