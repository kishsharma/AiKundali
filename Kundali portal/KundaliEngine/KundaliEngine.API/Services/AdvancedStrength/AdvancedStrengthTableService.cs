using KundaliEngine.API.DTOs.Responses;
using ApiHouseDto = KundaliEngine.API.DTOs.Responses.HouseDto;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Scoring;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.API.Services.AdvancedStrength
{
    public class AdvancedStrengthTableService
    {
        private static readonly IReadOnlyDictionary<Planet, int> NaturalStrengthOrder =
            new Dictionary<Planet, int>
            {
                [Planet.Sun] = 60,
                [Planet.Moon] = 51,
                [Planet.Venus] = 43,
                [Planet.Jupiter] = 34,
                [Planet.Mercury] = 26,
                [Planet.Mars] = 17,
                [Planet.Saturn] = 9,
                [Planet.Rahu] = 20,
                [Planet.Ketu] = 20
            };

        private readonly PlanetStrengthService _planetStrengthService;
        private readonly PlanetScoreService _planetScoreService;

        public AdvancedStrengthTableService(
            PlanetStrengthService planetStrengthService,
            PlanetScoreService planetScoreService)
        {
            _planetStrengthService = planetStrengthService;
            _planetScoreService = planetScoreService;
        }

        public AdvancedStrengthTableDto Build(
            IReadOnlyList<PlanetPosition> planets,
            IReadOnlyList<ApiHouseDto> houses,
            AspectCalculationResult aspectResult)
        {
            return new AdvancedStrengthTableDto
            {
                MethodNote = "Shadbala-ready strength overview and Ashtakavarga-ready house support. Current values reuse implemented dignity, Digbala, combustion, retrogression, aspect and house-score calculations; exact six-fold Shadbala and BAV/SAV bindu math can replace the estimated columns when implemented.",
                PlanetaryStrengths = planets.Select(planet => BuildPlanetRow(planet, aspectResult)).ToList(),
                HouseSupport = houses.Select(BuildHouseRow).ToList()
            };
        }

        private PlanetaryStrengthRowDto BuildPlanetRow(
            PlanetPosition planet,
            AspectCalculationResult aspectResult)
        {
            var planetScore = _planetScoreService.Calculate(planet);
            var aspectingPlanets = aspectResult.PlanetAspects
                .Where(aspect => aspect.ToPlanet == planet.Planet)
                .Select(aspect => aspect.FromPlanet)
                .Distinct()
                .ToList();

            var beneficAspects = aspectingPlanets.Count(IsNaturalBenefic);
            var maleficAspects = aspectingPlanets.Count(IsNaturalMaleficOrShadow);
            var drikBala = BuildDrikBala(beneficAspects, maleficAspects);

            return new PlanetaryStrengthRowDto
            {
                Planet = planet.Planet.ToString(),
                Sign = planet.Sign.ToString(),
                House = (int)planet.House,
                TotalScore = planetScore.Score,
                StrengthLevel = ScoreToLevel(planetScore.Score),
                SthanaBala = planet.DetailedStrength,
                DigBala = _planetStrengthService.GetDigbala(planet.Planet, planet.House),
                KalaBala = planet.IsCombust ? "Combust pressure" : "Unafflicted by combustion",
                CheshtaBala = planet.IsRetrograde ? "Retrograde emphasis" : "Direct motion",
                NaisargikaBala = $"{NaturalStrengthOrder.GetValueOrDefault(planet.Planet, 20)}/60",
                DrikBala = drikBala,
                Interpretation = BuildPlanetInterpretation(planet, planetScore.Score, drikBala)
            };
        }

        private static HouseSupportRowDto BuildHouseRow(ApiHouseDto house)
        {
            var score = Math.Clamp(house.Score, 0, 100);
            var supportLevel = ScoreToLevel(score);

            return new HouseSupportRowDto
            {
                House = (int)house.HouseNumber,
                Sign = house.Sign.ToString(),
                HouseName = house.HouseName,
                Score = score,
                EstimatedBavPoints = Math.Clamp((int)Math.Round(score / 12.5), 0, 8),
                EstimatedSavPoints = Math.Clamp(18 + (int)Math.Round(score * 0.25), 18, 43),
                SupportLevel = supportLevel,
                LifeAreas = string.Join(", ", house.LifeAreas.Take(3)),
                TransitUsefulness = BuildTransitUsefulness(score, house.HouseName)
            };
        }

        private static string BuildPlanetInterpretation(
            PlanetPosition planet,
            int score,
            string drikBala)
        {
            var delivery = score switch
            {
                >= 80 => "can deliver results strongly",
                >= 65 => "can deliver results with good support",
                >= 50 => "delivers mixed but workable results",
                >= 35 => "needs care before depending on its results",
                _ => "is sensitive and should be judged with caution"
            };

            var condition = new List<string>();
            if (planet.IsCombust)
            {
                condition.Add("combustion");
            }

            if (planet.IsRetrograde)
            {
                condition.Add("retrograde motion");
            }

            var conditionText = condition.Count == 0
                ? string.Empty
                : $" with {string.Join(" and ", condition)}";

            return $"{planet.Planet} {delivery} from house {(int)planet.House}{conditionText}; Drik Bala is {drikBala.ToLowerInvariant()}.";
        }

        private static string BuildTransitUsefulness(int score, string houseName)
        {
            return score switch
            {
                >= 80 => $"Very supportive for transits activating {houseName}.",
                >= 65 => $"Generally supportive for {houseName} results.",
                >= 50 => $"Usable with mixed outcomes for {houseName}.",
                >= 35 => $"Requires patience when {houseName} is activated.",
                _ => $"Sensitive for transits; avoid over-reliance on {houseName} timing."
            };
        }

        private static string BuildDrikBala(int beneficAspects, int maleficAspects)
        {
            if (beneficAspects > maleficAspects)
            {
                return "Supported";
            }

            if (maleficAspects > beneficAspects)
            {
                return "Pressured";
            }

            return beneficAspects == 0 ? "Neutral" : "Mixed";
        }

        private static string ScoreToLevel(int score)
        {
            return score switch
            {
                >= 80 => "Very Strong",
                >= 65 => "Strong",
                >= 50 => "Moderate",
                >= 35 => "Weak",
                _ => "Very Weak"
            };
        }

        private static bool IsNaturalBenefic(Planet planet)
        {
            return planet is Planet.Jupiter or Planet.Venus or Planet.Mercury or Planet.Moon;
        }

        private static bool IsNaturalMaleficOrShadow(Planet planet)
        {
            return planet is Planet.Sun or Planet.Mars or Planet.Saturn or Planet.Rahu or Planet.Ketu;
        }
    }
}


