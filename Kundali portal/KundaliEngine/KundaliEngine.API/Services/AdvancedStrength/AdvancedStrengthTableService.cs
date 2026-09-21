using KundaliEngine.API.DTOs.Responses;
using ApiHouseDto = KundaliEngine.API.DTOs.Responses.HouseDto;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Scoring;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.Domain.Rules;

namespace KundaliEngine.API.Services.AdvancedStrength
{
    public class AdvancedStrengthTableService
    {
        private static readonly IReadOnlyDictionary<Planet, double> NaisargikaVirupas =
            new Dictionary<Planet, double>
            {
                [Planet.Sun] = 60,
                [Planet.Moon] = 51.43,
                [Planet.Venus] = 42.86,
                [Planet.Jupiter] = 34.29,
                [Planet.Mercury] = 25.71,
                [Planet.Mars] = 17.14,
                [Planet.Saturn] = 8.57
            };

        private static readonly IReadOnlyDictionary<Planet, double> RequiredShadbalaVirupas =
            new Dictionary<Planet, double>
            {
                [Planet.Sun] = 390,
                [Planet.Moon] = 360,
                [Planet.Mars] = 300,
                [Planet.Mercury] = 420,
                [Planet.Jupiter] = 390,
                [Planet.Venus] = 330,
                [Planet.Saturn] = 300
            };

        private static readonly IReadOnlyDictionary<Planet, HouseNumber> DigBalaHouses =
            new Dictionary<Planet, HouseNumber>
            {
                [Planet.Sun] = HouseNumber.Tenth,
                [Planet.Mars] = HouseNumber.Tenth,
                [Planet.Moon] = HouseNumber.Fourth,
                [Planet.Venus] = HouseNumber.Fourth,
                [Planet.Jupiter] = HouseNumber.First,
                [Planet.Mercury] = HouseNumber.First,
                [Planet.Saturn] = HouseNumber.Seventh
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
            var sunLongitude = planets.FirstOrDefault(planet => planet.Planet == Planet.Sun)?.Longitude ?? 0;
            var moonLongitude = planets.FirstOrDefault(planet => planet.Planet == Planet.Moon)?.Longitude ?? 0;

            return new AdvancedStrengthTableDto
            {
                MethodNote = "Shadbala uses six-fold strength in virupas from dignity, direction, time, motion, natural force and aspects. Ashtakavarga house points remain estimated from house scores.",
                PlanetaryStrengths = planets.Select(planet => BuildPlanetRow(planet, aspectResult)).ToList(),
                Shadbala = planets
                    .Where(planet => planet.Planet is not Planet.Rahu and not Planet.Ketu)
                    .Select(planet => BuildShadbalaRow(planet, aspectResult, sunLongitude, moonLongitude))
                    .ToList(),
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
                NaisargikaBala = $"{NaisargikaVirupas.GetValueOrDefault(planet.Planet, 20):0}/60",
                DrikBala = drikBala,
                Interpretation = BuildPlanetInterpretation(planet, planetScore.Score, drikBala)
            };
        }

        private ShadbalaRowDto BuildShadbalaRow(
            PlanetPosition planet,
            AspectCalculationResult aspectResult,
            double sunLongitude,
            double moonLongitude)
        {
            var sthana = RoundVirupa(CalculateSthanaBala(planet));
            var dig = RoundVirupa(CalculateDigBala(planet));
            var kala = RoundVirupa(CalculateKalaBala(planet, sunLongitude, moonLongitude));
            var cheshta = RoundVirupa(CalculateCheshtaBala(planet));
            var naisargika = RoundVirupa(NaisargikaVirupas.GetValueOrDefault(planet.Planet, 0));
            var drik = RoundVirupa(CalculateDrikBala(planet, aspectResult));
            var total = RoundVirupa(sthana + dig + kala + cheshta + naisargika + drik);
            var required = RequiredShadbalaVirupas.GetValueOrDefault(planet.Planet, 360);
            var percent = required <= 0 ? 0 : (int)Math.Clamp(Math.Round(total / required * 100), 0, 200);
            var level = ScoreToLevel(Math.Clamp(percent, 0, 100));

            return new ShadbalaRowDto
            {
                Planet = planet.Planet.ToString(),
                Sign = planet.Sign.ToString(),
                House = (int)planet.House,
                SthanaBala = sthana,
                DigBala = dig,
                KalaBala = kala,
                CheshtaBala = cheshta,
                NaisargikaBala = naisargika,
                DrikBala = drik,
                Total = total,
                Required = required,
                Percent = percent,
                StrengthLevel = level,
                Interpretation = $"{planet.Planet} holds {percent}% of required Shadbala ({total:0.0}/{required:0} virupas) from house {(int)planet.House}."
            };
        }

        private double CalculateSthanaBala(PlanetPosition planet)
        {
            var uccha = CalculateUcchaBala(planet);
            var kendradi = planet.House switch
            {
                HouseNumber.First or HouseNumber.Fourth or HouseNumber.Seventh or HouseNumber.Tenth => 60,
                HouseNumber.Second or HouseNumber.Fifth or HouseNumber.Eighth or HouseNumber.Eleventh => 30,
                _ => 15
            };
            var ojha = HasOjhaYugmaBala(planet) ? 15 : 0;
            var dignity = _planetStrengthService.GetStrength(planet.Planet, planet.Sign) switch
            {
                PlanetStrength.OwnSign => 30,
                PlanetStrength.Neutral => 15,
                PlanetStrength.Debilitated => 0,
                PlanetStrength.Exalted => 0,
                _ => 10
            };

            return Math.Clamp(uccha + kendradi + ojha + dignity, 0, 180);
        }

        private static double CalculateUcchaBala(PlanetPosition planet)
        {
            if (!PlanetStrengthRules.Exaltation.TryGetValue(planet.Planet, out var exaltedSign)
                || !PlanetStrengthRules.ExaltationDegrees.TryGetValue(planet.Planet, out var exaltedDegree))
            {
                return 30;
            }

            var exaltationLongitude = ((int)exaltedSign - 1) * 30 + exaltedDegree;
            var separation = LongitudinalSeparation(planet.Longitude, exaltationLongitude);
            return Math.Clamp(60 * (1 - separation / 180), 0, 60);
        }

        private static bool HasOjhaYugmaBala(PlanetPosition planet)
        {
            var isOddSign = (int)planet.Sign % 2 == 1;
            return planet.Planet switch
            {
                Planet.Sun or Planet.Mars or Planet.Jupiter => isOddSign,
                Planet.Moon or Planet.Venus => !isOddSign,
                Planet.Mercury => true,
                Planet.Saturn => isOddSign,
                _ => false
            };
        }

        private static double CalculateDigBala(PlanetPosition planet)
        {
            if (!DigBalaHouses.TryGetValue(planet.Planet, out var preferredHouse))
            {
                return 0;
            }

            var current = (int)planet.House;
            var preferred = (int)preferredHouse;
            var distance = Math.Min(Math.Abs(current - preferred), 12 - Math.Abs(current - preferred));
            return Math.Clamp(60 * (1 - distance / 6.0), 0, 60);
        }

        private static double CalculateKalaBala(PlanetPosition planet, double sunLongitude, double moonLongitude)
        {
            var elongation = LongitudinalSeparation(moonLongitude, sunLongitude);
            var shuklaRatio = Math.Clamp(elongation / 180, 0, 1);
            var isBenefic = planet.Planet is Planet.Moon or Planet.Mercury or Planet.Venus or Planet.Jupiter;
            var paksha = 60 * (isBenefic ? shuklaRatio : 1 - shuklaRatio);
            var unafflicted = planet.IsCombust ? 8 : 40;
            var diurnal = 20;
            return Math.Clamp(paksha + unafflicted + diurnal, 0, 180);
        }

        private static double CalculateCheshtaBala(PlanetPosition planet)
        {
            var motion = planet.IsRetrograde ? 55 : 30;
            return Math.Clamp(motion - (planet.IsCombust ? 10 : 0), 0, 60);
        }

        private static double CalculateDrikBala(PlanetPosition planet, AspectCalculationResult aspectResult)
        {
            var aspectingPlanets = aspectResult.PlanetAspects
                .Where(aspect => aspect.ToPlanet == planet.Planet)
                .Select(aspect => aspect.FromPlanet)
                .Distinct()
                .ToList();

            var score = aspectingPlanets.Sum(aspecting =>
            {
                if (IsNaturalBenefic(aspecting))
                {
                    return 15;
                }

                if (IsNaturalMaleficOrShadow(aspecting))
                {
                    return -15;
                }

                return 0;
            });

            return Math.Clamp(score, -60, 60);
        }

        private static double LongitudinalSeparation(double first, double second)
        {
            var diff = Math.Abs(first - second) % 360;
            return diff > 180 ? 360 - diff : diff;
        }

        private static double RoundVirupa(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);

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


