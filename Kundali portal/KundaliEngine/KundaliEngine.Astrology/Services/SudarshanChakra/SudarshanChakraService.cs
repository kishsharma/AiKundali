using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.Astrology.Services.SudarshanChakra
{
    public class SudarshanChakraService
    {
        private readonly AspectService _aspectService;

        public SudarshanChakraService(AspectService aspectService)
        {
            _aspectService = aspectService;
        }

        public SudarshanChakraResult Calculate(List<PlanetPosition> planets, ZodiacSign ascendantSign)
        {
            if (planets == null || planets.Count == 0)
            {
                throw new ArgumentException("Planet positions are required.", nameof(planets));
            }

            var moon = planets.FirstOrDefault(planet => planet.Planet == Planet.Moon)
                ?? throw new ArgumentException("Moon position is required.", nameof(planets));
            var sun = planets.FirstOrDefault(planet => planet.Planet == Planet.Sun)
                ?? throw new ArgumentException("Sun position is required.", nameof(planets));

            var wheels = new List<SudarshanChakraWheel>
            {
                BuildWheel("lagna", "Lagna Chakra", "Ascendant", ascendantSign, "Body, life direction and visible events", planets),
                BuildWheel("chandra", "Chandra Chakra", "Moon", moon.Sign, "Mind, emotions, public experience and timing sensitivity", planets),
                BuildWheel("surya", "Surya Chakra", "Sun", sun.Sign, "Soul purpose, authority, vitality and father/government themes", planets)
            };

            var emphasis = BuildHouseEmphasis(wheels);
            var interpretations = BuildInterpretations(planets, emphasis);

            return new SudarshanChakraResult
            {
                Purpose = "Sudarshan Chakra combines Lagna, Moon and Sun reference charts to confirm whether a life area is supported repeatedly from body, mind and soul perspectives.",
                Usage = "Use it as a confirmation layer after the main Rashi chart. Repeated emphasis across Lagna, Chandra and Surya houses makes a prediction stronger; conflict between the three makes the result mixed and timing-dependent.",
                CalculationMethod = "The same planetary longitudes are kept. Houses are recalculated three times: from the Ascendant sign, from the Moon sign and from the Sun sign. Planet clusters, aspects and repeated house activation are then summarized.",
                OverallScore = CalculateOverallScore(interpretations, emphasis),
                Wheels = wheels,
                HouseEmphasis = emphasis,
                Interpretations = interpretations,
                OverallPrediction = BuildOverallPrediction(interpretations, emphasis)
            };
        }

        private SudarshanChakraWheel BuildWheel(
            string key,
            string name,
            string reference,
            ZodiacSign referenceSign,
            string focus,
            List<PlanetPosition> sourcePlanets)
        {
            var wheelPlanets = sourcePlanets
                .Select(planet => CloneWithHouse(planet, CalculateHouseFromReference(planet.Sign, referenceSign)))
                .ToList();
            var aspectResult = _aspectService.CalculateGrahaDrishti(wheelPlanets);
            var houses = new List<SudarshanChakraHouse>();

            for (var house = 1; house <= 12; house++)
            {
                var sign = GetSignFromHouse(referenceSign, house);
                houses.Add(new SudarshanChakraHouse
                {
                    HouseNumber = house,
                    Sign = sign,
                    Theme = GetHouseTheme(house),
                    Planets = wheelPlanets
                        .Where(planet => (int)planet.House == house)
                        .Select(MapPlacement)
                        .ToList(),
                    AspectedBy = aspectResult.HouseAspects
                        .Where(aspect => aspect.ToHouse == house)
                        .Select(aspect => aspect.FromPlanet.ToString())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList()
                });
            }

            return new SudarshanChakraWheel
            {
                Key = key,
                Name = name,
                Reference = reference,
                ReferenceSign = referenceSign,
                Focus = focus,
                Houses = houses
            };
        }

        private static List<SudarshanHouseEmphasis> BuildHouseEmphasis(List<SudarshanChakraWheel> wheels)
        {
            var lagna = wheels.First(wheel => wheel.Key == "lagna");
            var moon = wheels.First(wheel => wheel.Key == "chandra");
            var sun = wheels.First(wheel => wheel.Key == "surya");

            return Enumerable.Range(1, 12)
                .Select(house =>
                {
                    var lagnaCount = GetPredictivePlanetCount(lagna, house);
                    var moonCount = GetPredictivePlanetCount(moon, house);
                    var sunCount = GetPredictivePlanetCount(sun, house);
                    var activeReferences = CountActiveReferences(lagnaCount, moonCount, sunCount);
                    var total = lagnaCount + moonCount + sunCount;

                    return new SudarshanHouseEmphasis
                    {
                        HouseNumber = house,
                        Theme = GetHouseTheme(house),
                        LagnaPlanetCount = lagnaCount,
                        MoonPlanetCount = moonCount,
                        SunPlanetCount = sunCount,
                        TotalPlanetCount = total,
                        IsRepeatedAcrossReferences = activeReferences >= 2,
                        Prediction = BuildHousePrediction(house, total, activeReferences)
                    };
                })
                .OrderByDescending(item => item.IsRepeatedAcrossReferences)
                .ThenByDescending(item => item.TotalPlanetCount)
                .ThenBy(item => item.HouseNumber)
                .ToList();
        }

        private static List<SudarshanChakraInterpretation> BuildInterpretations(
            List<PlanetPosition> planets,
            List<SudarshanHouseEmphasis> emphasis)
        {
            var interpretations = new List<SudarshanChakraInterpretation>
            {
                BuildAreaInterpretation(
                    "Kendra Support",
                    emphasis,
                    new[] { 1, 4, 7, 10 },
                    "Core stability, relationships, home and career are reinforced when Kendra houses repeat."),
                BuildAreaInterpretation(
                    "Trikona Fortune",
                    emphasis,
                    new[] { 1, 5, 9 },
                    "Dharma, intelligence, grace and long-term opportunity improve when Trikona houses repeat."),
                BuildAreaInterpretation(
                    "Upachaya Growth",
                    emphasis,
                    new[] { 3, 6, 10, 11 },
                    "Effort, service, profession and gains grow stronger through persistence when Upachaya houses repeat."),
                BuildAreaInterpretation(
                    "Dusthana Pressure",
                    emphasis,
                    new[] { 6, 8, 12 },
                    "Health, debts, sudden changes and expenses need careful handling when Dusthana houses repeat.",
                    isPressureScore: true)
            };

            var beneficCount = planets.Count(IsNaturalBenefic);
            var challengingCount = planets.Count(planet => IsNaturalMaleficOrShadow(planet) || planet.IsCombust);
            interpretations.Add(new SudarshanChakraInterpretation
            {
                Area = "Planet Condition",
                Score = Math.Clamp(50 + (beneficCount * 6) - (challengingCount * 5), 0, 100),
                Tone = beneficCount >= challengingCount ? "Supportive" : "Mixed",
                Factors = new List<string>
                {
                    $"{beneficCount} natural benefic placements considered",
                    $"{challengingCount} malefic, shadow or combust placements considered"
                },
                Prediction = beneficCount >= challengingCount
                    ? "The Chakra has enough benefic support to convert repeated house emphasis into constructive outcomes."
                    : "The Chakra needs careful timing and remedies because repeated themes may arrive with pressure before results stabilize."
            });

            return interpretations;
        }

        private static SudarshanChakraInterpretation BuildAreaInterpretation(
            string area,
            List<SudarshanHouseEmphasis> emphasis,
            int[] houses,
            string prediction,
            bool isPressureScore = false)
        {
            var selected = emphasis.Where(item => houses.Contains(item.HouseNumber)).ToList();
            var repeated = selected.Count(item => item.IsRepeatedAcrossReferences);
            var score = Math.Clamp(30 + selected.Sum(item => item.TotalPlanetCount * 5) + repeated * 8, 0, 100);

            return new SudarshanChakraInterpretation
            {
                Area = area,
                Score = score,
                Tone = isPressureScore
                    ? score >= 70 ? "High Pressure" : score >= 50 ? "Moderate Pressure" : "Low Pressure"
                    : score >= 70 ? "Strong" : score >= 50 ? "Moderate" : "Low",
                Factors = selected
                    .Where(item => item.TotalPlanetCount > 0)
                    .Select(item => $"{item.Theme}: {item.TotalPlanetCount} combined placements")
                    .ToList(),
                Prediction = prediction
            };
        }

        private static int CalculateOverallScore(
            List<SudarshanChakraInterpretation> interpretations,
            List<SudarshanHouseEmphasis> emphasis)
        {
            if (interpretations.Count == 0)
            {
                return 0;
            }

            var supportiveScores = interpretations
                .Where(item => !string.Equals(item.Area, "Dusthana Pressure", StringComparison.OrdinalIgnoreCase))
                .Select(item => item.Score)
                .ToList();
            var interpretationAverage = supportiveScores.Count == 0
                ? interpretations.Average(item => item.Score)
                : supportiveScores.Average();
            var dusthanaPressure = interpretations
                .FirstOrDefault(item => string.Equals(item.Area, "Dusthana Pressure", StringComparison.OrdinalIgnoreCase))
                ?.Score ?? 0;
            var repeatedBonus = Math.Min(15, emphasis.Count(item => item.IsRepeatedAcrossReferences) * 3);
            var pressurePenalty = Math.Max(0, dusthanaPressure - 50) / 3;
            return Math.Clamp((int)Math.Round(interpretationAverage + repeatedBonus - pressurePenalty), 0, 100);
        }
        private static string BuildOverallPrediction(
            List<SudarshanChakraInterpretation> interpretations,
            List<SudarshanHouseEmphasis> emphasis)
        {
            var strongestArea = interpretations
                .OrderByDescending(item => item.Score)
                .FirstOrDefault();
            var topHouse = emphasis
                .Where(item => item.TotalPlanetCount > 0)
                .OrderByDescending(item => item.IsRepeatedAcrossReferences)
                .ThenByDescending(item => item.TotalPlanetCount)
                .FirstOrDefault();

            if (strongestArea == null || topHouse == null)
            {
                return "Sudarshan Chakra is available, but no concentrated house emphasis was found.";
            }

            return $"{strongestArea.Area} is the leading pattern. The {topHouse.HouseNumber} house ({topHouse.Theme}) receives the clearest repeated emphasis, so predictions connected to this area carry higher confidence.";
        }

        private static PlanetPosition CloneWithHouse(PlanetPosition source, HouseNumber house)
        {
            return new PlanetPosition
            {
                Planet = source.Planet,
                PlanetName = source.PlanetName,
                Longitude = source.Longitude,
                Latitude = source.Latitude,
                Sign = source.Sign,
                DegreeInSign = source.DegreeInSign,
                Degree = source.Degree,
                Minute = source.Minute,
                Nakshatra = source.Nakshatra,
                Pada = source.Pada,
                House = house,
                IsRetrograde = source.IsRetrograde,
                DetailedStrength = source.DetailedStrength,
                IsCombust = source.IsCombust
            };
        }

        private static SudarshanPlanetPlacement MapPlacement(PlanetPosition planet)
        {
            return new SudarshanPlanetPlacement
            {
                Planet = planet.Planet,
                Sign = planet.Sign,
                House = (int)planet.House,
                Longitude = planet.Longitude,
                DegreeInSign = planet.DegreeInSign,
                IsRetrograde = planet.IsRetrograde,
                IsCombust = planet.IsCombust,
                DetailedStrength = planet.DetailedStrength
            };
        }

        private static HouseNumber CalculateHouseFromReference(ZodiacSign sign, ZodiacSign referenceSign)
        {
            var house = (((int)sign - (int)referenceSign + 12) % 12) + 1;
            return (HouseNumber)house;
        }

        private static ZodiacSign GetSignFromHouse(ZodiacSign referenceSign, int house)
        {
            var signIndex = (((int)referenceSign - 1) + (house - 1)) % 12;
            return (ZodiacSign)(signIndex + 1);
        }

        private static int GetPredictivePlanetCount(SudarshanChakraWheel wheel, int house)
        {
            return wheel.Houses
                .First(item => item.HouseNumber == house)
                .Planets
                .Count(planet => IsPredictivePlacement(wheel, planet));
        }

        private static bool IsPredictivePlacement(SudarshanChakraWheel wheel, SudarshanPlanetPlacement planet)
        {
            return !(
                planet.House == 1 &&
                ((string.Equals(wheel.Key, "chandra", StringComparison.OrdinalIgnoreCase) && planet.Planet == Planet.Moon) ||
                 (string.Equals(wheel.Key, "surya", StringComparison.OrdinalIgnoreCase) && planet.Planet == Planet.Sun)));
        }

        private static int CountActiveReferences(params int[] counts)
        {
            return counts.Count(count => count > 0);
        }

        private static string BuildHousePrediction(int house, int total, int activeReferences)
        {
            if (total == 0)
            {
                return $"No direct planet cluster is present for {GetHouseTheme(house)}; judge this area mainly through lordship and aspects.";
            }

            var confidence = activeReferences >= 2 ? "high confidence" : "moderate confidence";
            return $"{GetHouseTheme(house)} is activated with {confidence} because {total} combined planet placements appear across {activeReferences} reference chart(s).";
        }

        private static string GetHouseTheme(int house)
        {
            return house switch
            {
                1 => "Self, vitality and personality",
                2 => "Wealth, family and speech",
                3 => "Courage, communication and siblings",
                4 => "Home, mother and inner peace",
                5 => "Education, creativity and children",
                6 => "Health, debts and competition",
                7 => "Marriage, partnership and agreements",
                8 => "Longevity, transformation and hidden matters",
                9 => "Fortune, dharma and guidance",
                10 => "Career, karma and status",
                11 => "Gains, networks and fulfillment",
                12 => "Expenses, sleep and spiritual release",
                _ => string.Empty
            };
        }

        private static bool IsNaturalBenefic(PlanetPosition planet)
        {
            return planet.Planet is Planet.Jupiter or Planet.Venus or Planet.Mercury;
        }

        private static bool IsNaturalMaleficOrShadow(PlanetPosition planet)
        {
            return planet.Planet is Planet.Sun or Planet.Mars or Planet.Saturn or Planet.Rahu or Planet.Ketu;
        }
    }
}



