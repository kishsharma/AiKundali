using KundaliEngine.Astrology.Services.Scoring;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.KnowledgeBase.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KundaliEngine.Astrology.Services.Compatibility
{
    /// <summary>
    /// Calculates explainable compatibility scores from Kundali chart data only.
    /// Ashtakoota is still returned on the response for API compatibility, but it is
    /// not used in any dimension score.
    /// </summary>
    public class CompatibilityScoreService
    {
        private static readonly HouseNumber[] KendraHouses = { HouseNumber.First, HouseNumber.Fourth, HouseNumber.Seventh, HouseNumber.Tenth };
        private static readonly HouseNumber[] TrikonaHouses = { HouseNumber.First, HouseNumber.Fifth, HouseNumber.Ninth };
        private static readonly HouseNumber[] DharmaTrikonaHouses = { HouseNumber.First, HouseNumber.Fifth, HouseNumber.Ninth };
        private static readonly HouseNumber[] MarsDoshaHouses = { HouseNumber.First, HouseNumber.Fourth, HouseNumber.Seventh, HouseNumber.Eighth, HouseNumber.Twelfth };
        private static readonly Planet[] BeneficPlanets = { Planet.Moon, Planet.Jupiter, Planet.Venus };
        private static readonly Planet[] MaleficOrShadowPlanets = { Planet.Sun, Planet.Mars, Planet.Saturn, Planet.Rahu, Planet.Ketu };

        private readonly NakshatraMetadataService _nakshatraMetadataService;
        private readonly AstrologyKnowledgeService _astrologyKnowledgeService;
        private readonly YoniCompatibilityService _yoniCompatibilityService;
        private readonly PlanetScoreService _planetScoreService;

        public CompatibilityScoreService(
            NakshatraMetadataService nakshatraMetadataService,
            AstrologyKnowledgeService astrologyKnowledgeService,
            YoniCompatibilityService yoniCompatibilityService)
        {
            _nakshatraMetadataService = nakshatraMetadataService;
            _astrologyKnowledgeService = astrologyKnowledgeService;
            _yoniCompatibilityService = yoniCompatibilityService;
            _planetScoreService = new PlanetScoreService(astrologyKnowledgeService);
        }

        public CompatibilityResult CalculateCompatibility(
            CompatibilityProfile personA,
            CompatibilityProfile personB)
        {
            if (personA == null)
                throw new ArgumentNullException(nameof(personA));
            if (personB == null)
                throw new ArgumentNullException(nameof(personB));
            if (personA.Chart == null)
                throw new ArgumentException("PersonA chart is missing", nameof(personA));
            if (personB.Chart == null)
                throw new ArgumentException("PersonB chart is missing", nameof(personB));

            var result = new CompatibilityResult(personA.Name, personB.Name);
            var ashtakoota = CalculateAshtakoota(personA.Chart, personB.Chart);

            var dimensions = new List<CompatibilityDimension>
            {
                CalculateEmotionalHarmony(personA, personB),
                CalculateCommunicationCompatibility(personA, personB),
                CalculateMarriageStability(personA, personB),
                CalculateAttractionRomance(personA, personB),
                CalculateSpiritualCompatibility(personA, personB),
                CalculateGrowthTogether(personA, personB)
            };

            result.Dimensions = dimensions;
            result.OverallCompatibilityScore = (int)Math.Round(dimensions.Average(d => d.Score));
            result.OverallConfidence = (int)Math.Round(dimensions.Average(d => d.Confidence));

            ExtractDominantThemes(result);

            result.Summary = GenerateSummary(result);
            result.PersonAchart = personA.Chart;
            result.PersonBchart = personB.Chart;
            result.Ashtakoota = ashtakoota;
            return result;
        }

        private AshtakootaResult CalculateAshtakoota(KundaliChart boyChart, KundaliChart girlChart)
        {
            var boyMoon = boyChart.PlanetPositions.First(p => p.Planet == Planet.Moon);
            var girlMoon = girlChart.PlanetPositions.First(p => p.Planet == Planet.Moon);
            var boyNak = (int)boyMoon.Nakshatra;
            var girlNak = (int)girlMoon.Nakshatra;
            var boySign = (int)boyMoon.Sign;
            var girlSign = (int)girlMoon.Sign;
            var boyNakMeta = _nakshatraMetadataService.GetNakshatra(boyMoon.Nakshatra.ToString());
            var girlNakMeta = _nakshatraMetadataService.GetNakshatra(girlMoon.Nakshatra.ToString());
            var boyVarna = MetaValue(boyNakMeta?.Varna, Varna(boySign));
            var girlVarna = MetaValue(girlNakMeta?.Varna, Varna(girlSign));
            var boyYoni = MetaValue(boyNakMeta?.Yoni, Yoni(boyNak));
            var girlYoni = MetaValue(girlNakMeta?.Yoni, Yoni(girlNak));
            var boyGana = MetaValue(boyNakMeta?.Gana, Gana(boyNak));
            var girlGana = MetaValue(girlNakMeta?.Gana, Gana(girlNak));
            var boyNadi = MetaValue(boyNakMeta?.Nadi, Nadi(boyNak));
            var girlNadi = MetaValue(girlNakMeta?.Nadi, Nadi(girlNak));
            var scores = new List<KutaScore>
            {
                Score("Varna", VarnaScore(boyVarna, girlVarna), 1, Detail(boyVarna, girlVarna)),
                Score("Vasya", boySign == girlSign ? 2 : 1, 2, Detail(boyMoon.Sign.ToString(), girlMoon.Sign.ToString())),
                Score("Tara", TaraScore(boyNak, girlNak), 3, Detail(boyMoon.Nakshatra.ToString(), girlMoon.Nakshatra.ToString())),
                Score("Yoni", _yoniCompatibilityService.GetScore(boyYoni, girlYoni), 4, Detail(boyYoni, girlYoni)),
                Score("Graha Maitri", GrahaMaitriScore(Lord(boySign), Lord(girlSign)), 5, Detail(Lord(boySign), Lord(girlSign))),
                Score("Gana", GanaScore(boyGana, girlGana), 6, Detail(boyGana, girlGana)),
                Score("Bhakoot", BhakootScore(boySign, girlSign), 7, Detail(boyMoon.Sign.ToString(), girlMoon.Sign.ToString())),
                Score("Nadi", boyNadi == girlNadi ? 0 : 8, 8, Detail(boyNadi, girlNadi))
            };
            return new AshtakootaResult { Scores = scores, TotalScore = scores.Sum(x => x.Score) };
        }

        private static KutaScore Score(string name, double score, double max, string detail) => new() { Name = name, Score = score, MaxScore = max, Detail = detail, Grade = score >= max ? "Excellent" : score >= max * .65 ? "Good" : score > 0 ? "Average" : "Challenging" };
        private static string Detail(string boyValue, string girlValue) => $"Boy: {boyValue} / Girl: {girlValue}";
        private static string MetaValue(string? value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback : value;
        private static double VarnaScore(string boyVarna, string girlVarna) => VarnaRank(boyVarna) >= VarnaRank(girlVarna) ? 1 : 0;
        private static string Varna(int sign) => sign switch { 1 or 5 or 9 => "Kshatriya", 2 or 6 or 10 => "Vaishya", 3 or 7 or 11 => "Shudra", _ => "Brahmin" };
        private static int VarnaRank(string value) => value switch { "Shudra" => 1, "Farmer" => 2, "Vaishya" => 2, "Kshatriya" => 3, _ => 4 };
        private static double TaraScore(int a, int b) => (IsTaraGood(((b - a + 27) % 27) + 1) ? 1.5 : 0) + (IsTaraGood(((a - b + 27) % 27) + 1) ? 1.5 : 0);
        private static bool IsTaraGood(int count) => !new[] { 3, 5, 7 }.Contains(count % 9);
        private static string Yoni(int n) => new[] { "Horse", "Elephant", "Goat", "Serpent", "Serpent", "Dog", "Cat", "Goat", "Cat", "Rat", "Rat", "Cow", "Buffalo", "Tiger", "Buffalo", "Tiger", "Deer", "Deer", "Dog", "Monkey", "Mongoose", "Monkey", "Lion", "Horse", "Lion", "Cow", "Elephant" }[n];
        private static string Lord(int sign) => sign switch { 1 or 8 => "Mars", 2 or 7 => "Venus", 3 or 6 => "Mercury", 4 => "Moon", 5 => "Sun", 9 or 12 => "Jupiter", _ => "Saturn" };

        private double GrahaMaitriScore(string boyLord, string girlLord)
        {
            if (boyLord == girlLord) return 5;

            var boyRelation = PlanetRelation(boyLord, girlLord);
            var girlRelation = PlanetRelation(girlLord, boyLord);

            if (boyRelation == "Friend" && girlRelation == "Friend") return 5;
            if (IsPair(boyRelation, girlRelation, "Friend", "Neutral")) return 4;
            if (boyRelation == "Neutral" && girlRelation == "Neutral") return 3;
            if (IsPair(boyRelation, girlRelation, "Friend", "Enemy")) return 1;
            if (IsPair(boyRelation, girlRelation, "Neutral", "Enemy")) return 0.5;
            return 0;
        }

        private static bool IsPair(string a, string b, string x, string y) => (a == x && b == y) || (a == y && b == x);

        private string PlanetRelation(string from, string to)
        {
            var planet = _astrologyKnowledgeService.GetPlanet(from);
            if (planet?.Friends.Contains(to) == true) return "Friend";
            if (planet?.Enemies.Contains(to) == true) return "Enemy";
            return "Neutral";
        }

        private static double GanaScore(string ga, string gb)
        {
            if (ga == gb) return 6;
            if ((ga == "Deva" && gb == "Manushya") || (ga == "Manushya" && gb == "Deva")) return 6;
            if ((ga == "Deva" && gb == "Rakshasa") || (ga == "Rakshasa" && gb == "Deva")) return 1;
            return 0;
        }

        private static string Gana(int n) => new[] { "Deva", "Manushya", "Rakshasa", "Manushya", "Deva", "Manushya", "Deva", "Deva", "Rakshasa", "Rakshasa", "Manushya", "Manushya", "Deva", "Rakshasa", "Deva", "Rakshasa", "Deva", "Rakshasa", "Rakshasa", "Manushya", "Manushya", "Deva", "Rakshasa", "Rakshasa", "Manushya", "Manushya", "Deva" }[n];
        private static double BhakootScore(int a, int b) { var diff = Math.Abs(a - b); var pair = Math.Min(diff, 12 - diff); return pair is 1 or 4 or 5 ? 0 : 7; }
        private static string Nadi(int n) => (n % 3) switch { 0 => "Adi", 1 => "Madhya", _ => "Antya" };

        private CompatibilityDimension CalculateEmotionalHarmony(CompatibilityProfile personA, CompatibilityProfile personB)
        {
            var components = new List<ScoreComponentInput>
            {
                PlanetPairStrength(personA.Chart, personB.Chart, Planet.Moon, "Moon placement and strength"),
                PlanetAfflictionComponent(personA.Chart, personB.Chart, Planet.Moon, "Moon affliction/protection"),
                HouseComponent(personA.Chart, personB.Chart, HouseNumber.Fourth, "4th house emotional foundation"),
                HouseLordComponent(personA.Chart, personB.Chart, HouseNumber.Fourth, "4th lord emotional support"),
                PlanetAspectNatureComponent(personA.Chart, personB.Chart, Planet.Moon, "Benefic/malefic aspects on Moon"),
                CrossPlanetInfluenceComponent(personA.Chart, personB.Chart, Planet.Venus, Planet.Jupiter, "Venus-Jupiter influence")
            };

            return BuildDimension("Emotional Harmony", components, personA.Chart, personB.Chart);
        }

        private CompatibilityDimension CalculateCommunicationCompatibility(CompatibilityProfile personA, CompatibilityProfile personB)
        {
            var components = new List<ScoreComponentInput>
            {
                PlanetPairStrength(personA.Chart, personB.Chart, Planet.Mercury, "Mercury placement and strength"),
                PlanetAfflictionComponent(personA.Chart, personB.Chart, Planet.Mercury, "Mercury affliction/protection"),
                HouseComponent(personA.Chart, personB.Chart, HouseNumber.Second, "2nd house speech and family values"),
                HouseLordComponent(personA.Chart, personB.Chart, HouseNumber.Second, "2nd lord speech support"),
                HouseComponent(personA.Chart, personB.Chart, HouseNumber.Third, "3rd house expression and listening"),
                HouseLordComponent(personA.Chart, personB.Chart, HouseNumber.Third, "3rd lord communication support"),
                CrossPlanetInfluenceComponent(personA.Chart, personB.Chart, Planet.Mercury, Planet.Moon, "Mercury-Moon relationship"),
                CrossPlanetInfluenceComponent(personA.Chart, personB.Chart, Planet.Mercury, Planet.Venus, "Mercury-Venus relationship")
            };

            return BuildDimension("Communication Compatibility", components, personA.Chart, personB.Chart);
        }

        private CompatibilityDimension CalculateMarriageStability(CompatibilityProfile personA, CompatibilityProfile personB)
        {
            var components = new List<ScoreComponentInput>
            {
                HouseComponent(personA.Chart, personB.Chart, HouseNumber.Seventh, "7th house marriage foundation"),
                HouseLordComponent(personA.Chart, personB.Chart, HouseNumber.Seventh, "7th lord partnership strength"),
                PlanetPairStrength(personA.Chart, personB.Chart, Planet.Venus, "Venus relationship karaka"),
                PlanetPairStrength(personA.Chart, personB.Chart, Planet.Jupiter, "Jupiter protection and wisdom"),
                PlanetPairStrength(personA.Chart, personB.Chart, Planet.Saturn, "Saturn commitment and endurance"),
                HouseComponent(personA.Chart, personB.Chart, HouseNumber.Second, "2nd house family continuity"),
                HouseComponent(personA.Chart, personB.Chart, HouseNumber.Eighth, "8th house longevity and shared vulnerability"),
                NodeInfluenceComponent(personA.Chart, personB.Chart, new[] { HouseNumber.Seventh, HouseNumber.Second, HouseNumber.Eighth }, "Rahu/Ketu influence on marriage houses"),
                MarsDoshaComponent(personA.Chart, personB.Chart, "Mars Dosha marriage pressure"),
                MarriageAspectComponent(personA.Chart, personB.Chart, "Benefic/malefic aspects on marriage indicators")
            };

            return BuildDimension("Marriage Stability", components, personA.Chart, personB.Chart);
        }

        private CompatibilityDimension CalculateAttractionRomance(CompatibilityProfile personA, CompatibilityProfile personB)
        {
            var components = new List<ScoreComponentInput>
            {
                PlanetPairStrength(personA.Chart, personB.Chart, Planet.Venus, "Venus romance and affection"),
                PlanetPairStrength(personA.Chart, personB.Chart, Planet.Mars, "Mars passion and desire"),
                CrossPlanetInfluenceComponent(personA.Chart, personB.Chart, Planet.Venus, Planet.Mars, "Venus-Mars relationship"),
                HouseComponent(personA.Chart, personB.Chart, HouseNumber.Fifth, "5th house romance"),
                HouseComponent(personA.Chart, personB.Chart, HouseNumber.Seventh, "7th house partnership attraction"),
                HouseComponent(personA.Chart, personB.Chart, HouseNumber.Twelfth, "12th house intimacy"),
                CrossPlanetInfluenceComponent(personA.Chart, personB.Chart, Planet.Moon, Planet.Venus, "Moon-Venus emotional affection")
            };

            return BuildDimension("Attraction & Romance", components, personA.Chart, personB.Chart);
        }

        private CompatibilityDimension CalculateSpiritualCompatibility(CompatibilityProfile personA, CompatibilityProfile personB)
        {
            var components = new List<ScoreComponentInput>
            {
                PlanetPairStrength(personA.Chart, personB.Chart, Planet.Jupiter, "Jupiter wisdom and dharma"),
                HouseComponent(personA.Chart, personB.Chart, HouseNumber.Fifth, "5th house mantra and merit"),
                HouseComponent(personA.Chart, personB.Chart, HouseNumber.Ninth, "9th house dharma and guru support"),
                LagnaLordComponent(personA.Chart, personB.Chart, "Lagna lord spiritual direction"),
                PlanetPairStrength(personA.Chart, personB.Chart, Planet.Ketu, "Ketu detachment and moksha impulse"),
                HouseGroupComponent(personA.Chart, personB.Chart, DharmaTrikonaHouses, "Dharma Trikona strength")
            };

            return BuildDimension("Spiritual Compatibility", components, personA.Chart, personB.Chart);
        }

        private CompatibilityDimension CalculateGrowthTogether(CompatibilityProfile personA, CompatibilityProfile personB)
        {
            var components = new List<ScoreComponentInput>
            {
                HouseComponent(personA.Chart, personB.Chart, HouseNumber.First, "1st house resilience"),
                HouseComponent(personA.Chart, personB.Chart, HouseNumber.Fifth, "5th house creativity and planning"),
                HouseComponent(personA.Chart, personB.Chart, HouseNumber.Ninth, "9th house fortune and guidance"),
                HouseComponent(personA.Chart, personB.Chart, HouseNumber.Tenth, "10th house career direction"),
                HouseComponent(personA.Chart, personB.Chart, HouseNumber.Eleventh, "11th house gains and networks"),
                LagnaLordComponent(personA.Chart, personB.Chart, "Lagna lord mutual growth capacity"),
                HouseLordComponent(personA.Chart, personB.Chart, HouseNumber.Tenth, "10th lord execution strength"),
                HouseLordComponent(personA.Chart, personB.Chart, HouseNumber.Eleventh, "11th lord gains support"),
                RajaYogaComponent(personA.Chart, personB.Chart, "Raja Yoga growth signal"),
                DhanaYogaComponent(personA.Chart, personB.Chart, "Dhana Yoga shared prosperity signal"),
                MutualBeneficInfluenceComponent(personA.Chart, personB.Chart, "Mutual benefic influences")
            };

            return BuildDimension("Growth Together", components, personA.Chart, personB.Chart);
        }

        private CompatibilityDimension BuildDimension(string name, List<ScoreComponentInput> components, KundaliChart chartA, KundaliChart chartB)
        {
            var validComponents = components.Where(component => component.Score.HasValue).ToList();
            if (validComponents.Count == 0)
            {
                throw new InvalidOperationException($"{name} cannot be scored because no Kundali inputs were available.");
            }

            var score = WeightedScore(validComponents);
            var dimension = new CompatibilityDimension(name, score, BuildSummary(name, score));
            AddComponentFactors(dimension, validComponents);
            dimension.ScoreBreakdown = BuildScoreBreakdown(validComponents);
            dimension.Confidence = ConfidenceFromData(validComponents, chartA, chartB);
            return dimension;
        }

        private ScoreComponentInput PlanetPairStrength(KundaliChart chartA, KundaliChart chartB, Planet planet, string label)
        {
            var a = GetPlanet(chartA, planet);
            var b = GetPlanet(chartB, planet);
            if (a == null || b == null)
            {
                return ScoreComponentInput.Unavailable(label, $"{planet} missing from one or both charts");
            }

            var aScore = PlanetScore(a);
            var bScore = PlanetScore(b);
            var relation = PlanetRelationshipScore(a, b);
            var score = WeightedScore(new[]
            {
                ScoreComponentInput.Available($"{planet} strength average", AverageScores(aScore, bScore), "Both planet strengths"),
                ScoreComponentInput.Available($"{planet} relationship", relation, $"{FormatPlanet(a)} / {FormatPlanet(b)}")
            });

            return ScoreComponentInput.Available(label, score, $"{planet}: {aScore}/100 and {bScore}/100; relationship {relation}/100");
        }

        private ScoreComponentInput PlanetAfflictionComponent(KundaliChart chartA, KundaliChart chartB, Planet planet, string label)
        {
            var a = PlanetConditionScore(chartA, planet);
            var b = PlanetConditionScore(chartB, planet);
            if (!a.Score.HasValue || !b.Score.HasValue)
            {
                return ScoreComponentInput.Unavailable(label, $"{planet} missing from one or both charts");
            }

            return ScoreComponentInput.Available(label, AverageScores(a.Score.Value, b.Score.Value), $"{a.Detail}; {b.Detail}");
        }

        private ScoreComponentInput PlanetAspectNatureComponent(KundaliChart chartA, KundaliChart chartB, Planet planet, string label)
        {
            var a = AspectNatureScore(chartA, planet);
            var b = AspectNatureScore(chartB, planet);
            if (!a.Score.HasValue || !b.Score.HasValue)
            {
                return ScoreComponentInput.Unavailable(label, $"{planet} missing from one or both charts");
            }

            return ScoreComponentInput.Available(label, AverageScores(a.Score.Value, b.Score.Value), $"{a.Detail}; {b.Detail}");
        }

        private ScoreComponentInput HouseComponent(KundaliChart chartA, KundaliChart chartB, HouseNumber houseNumber, string label)
        {
            var a = GetHouse(chartA, houseNumber);
            var b = GetHouse(chartB, houseNumber);
            if (a == null || b == null)
            {
                return ScoreComponentInput.Unavailable(label, $"{houseNumber} missing from one or both charts");
            }

            return ScoreComponentInput.Available(label, AverageScores(a.Score, b.Score), $"{HouseDetail(chartA, houseNumber)}; {HouseDetail(chartB, houseNumber)}");
        }

        private ScoreComponentInput HouseLordComponent(KundaliChart chartA, KundaliChart chartB, HouseNumber houseNumber, string label)
        {
            var a = HouseLordScore(chartA, houseNumber);
            var b = HouseLordScore(chartB, houseNumber);
            if (!a.Score.HasValue || !b.Score.HasValue)
            {
                return ScoreComponentInput.Unavailable(label, $"{houseNumber} lord missing from one or both charts");
            }

            return ScoreComponentInput.Available(label, AverageScores(a.Score.Value, b.Score.Value), $"{a.Detail}; {b.Detail}");
        }

        private ScoreComponentInput LagnaLordComponent(KundaliChart chartA, KundaliChart chartB, string label)
        {
            var a = HouseLordScore(chartA, HouseNumber.First);
            var b = HouseLordScore(chartB, HouseNumber.First);
            if (!a.Score.HasValue || !b.Score.HasValue)
            {
                return ScoreComponentInput.Unavailable(label, "Lagna lord missing from one or both charts");
            }

            return ScoreComponentInput.Available(label, AverageScores(a.Score.Value, b.Score.Value), $"{a.Detail}; {b.Detail}");
        }

        private ScoreComponentInput HouseGroupComponent(KundaliChart chartA, KundaliChart chartB, IEnumerable<HouseNumber> houses, string label)
        {
            var scores = houses
                .Select(house => HouseComponent(chartA, chartB, house, $"{house}"))
                .Where(component => component.Score.HasValue)
                .ToList();

            if (scores.Count == 0)
            {
                return ScoreComponentInput.Unavailable(label, "No requested houses available");
            }

            return ScoreComponentInput.Available(label, AverageScores(scores.Select(component => component.Score!.Value).ToArray()), string.Join("; ", scores.Select(component => component.Detail)));
        }

        private ScoreComponentInput CrossPlanetInfluenceComponent(KundaliChart chartA, KundaliChart chartB, Planet first, Planet second, string label)
        {
            var firstA = GetPlanet(chartA, first);
            var secondA = GetPlanet(chartA, second);
            var firstB = GetPlanet(chartB, first);
            var secondB = GetPlanet(chartB, second);
            if (firstA == null || secondA == null || firstB == null || secondB == null)
            {
                return ScoreComponentInput.Unavailable(label, $"{first} or {second} missing from one or both charts");
            }

            var aScore = IntraChartPlanetRelationshipScore(chartA, first, second);
            var bScore = IntraChartPlanetRelationshipScore(chartB, first, second);
            var crossScore = AverageScores(PlanetRelationshipScore(firstA, secondB), PlanetRelationshipScore(firstB, secondA));
            var score = AverageScores(aScore.Score, bScore.Score, crossScore);
            return ScoreComponentInput.Available(label, score, $"{aScore.Detail}; {bScore.Detail}; cross-chart relationship {crossScore}/100");
        }

        private ScoreComponentInput NodeInfluenceComponent(KundaliChart chartA, KundaliChart chartB, IEnumerable<HouseNumber> houses, string label)
        {
            var a = NodeInfluenceScore(chartA, houses);
            var b = NodeInfluenceScore(chartB, houses);
            return ScoreComponentInput.Available(label, AverageScores(a.Score, b.Score), $"{a.Detail}; {b.Detail}");
        }

        private ScoreComponentInput MarsDoshaComponent(KundaliChart chartA, KundaliChart chartB, string label)
        {
            var a = MarsDoshaScore(chartA);
            var b = MarsDoshaScore(chartB);
            return ScoreComponentInput.Available(label, AverageScores(a.Score, b.Score), $"{a.Detail}; {b.Detail}");
        }

        private ScoreComponentInput MarriageAspectComponent(KundaliChart chartA, KundaliChart chartB, string label)
        {
            var indicators = new[] { Planet.Venus, Planet.Jupiter, Planet.Saturn };
            var planetScores = indicators
                .Select(planet => PlanetAspectNatureComponent(chartA, chartB, planet, $"{planet} aspects"))
                .Where(component => component.Score.HasValue)
                .Select(component => component.Score!.Value)
                .ToList();

            var seventhA = HouseAspectScore(chartA, HouseNumber.Seventh);
            var seventhB = HouseAspectScore(chartB, HouseNumber.Seventh);
            planetScores.Add(seventhA.Score);
            planetScores.Add(seventhB.Score);

            return ScoreComponentInput.Available(label, AverageScores(planetScores.ToArray()), $"7th house aspects: {seventhA.Detail}; {seventhB.Detail}");
        }

        private ScoreComponentInput RajaYogaComponent(KundaliChart chartA, KundaliChart chartB, string label)
        {
            var a = RajaYogaScore(chartA);
            var b = RajaYogaScore(chartB);
            return ScoreComponentInput.Available(label, AverageScores(a.Score, b.Score), $"{a.Detail}; {b.Detail}");
        }

        private ScoreComponentInput DhanaYogaComponent(KundaliChart chartA, KundaliChart chartB, string label)
        {
            var a = DhanaYogaScore(chartA);
            var b = DhanaYogaScore(chartB);
            return ScoreComponentInput.Available(label, AverageScores(a.Score, b.Score), $"{a.Detail}; {b.Detail}");
        }

        private ScoreComponentInput MutualBeneficInfluenceComponent(KundaliChart chartA, KundaliChart chartB, string label)
        {
            var a = BeneficInfluenceScore(chartA, KendraHouses.Concat(TrikonaHouses).Distinct());
            var b = BeneficInfluenceScore(chartB, KendraHouses.Concat(TrikonaHouses).Distinct());
            return ScoreComponentInput.Available(label, AverageScores(a.Score, b.Score), $"{a.Detail}; {b.Detail}");
        }

        private (int? Score, string Detail) PlanetConditionScore(KundaliChart chart, Planet planet)
        {
            var position = GetPlanet(chart, planet);
            if (position == null)
            {
                return (null, $"{planet} missing");
            }

            var strength = PlanetScore(position);
            var aspectScore = AspectNatureScore(chart, planet);
            var housePressure = IsDusthana(position.House) ? 100 - HouseScore(chart, position.House) : HouseScore(chart, position.House);
            var combustionScore = position.IsCombust ? 100 - strength : strength;
            var retrogradeScore = position.IsRetrograde ? AverageScores(strength, housePressure) : strength;

            var score = WeightedScore(new[]
            {
                ScoreComponentInput.Available("Strength", strength, ""),
                ScoreComponentInput.Available("Aspects", aspectScore.Score ?? strength, ""),
                ScoreComponentInput.Available("House condition", housePressure, ""),
                ScoreComponentInput.Available("Combust/retrograde condition", AverageScores(combustionScore, retrogradeScore), "")
            });

            return (score, $"{planet} condition {score}/100 from strength {strength}/100, aspect condition {aspectScore.Score}/100, house {(int)position.House}");
        }

        private (int? Score, string Detail) AspectNatureScore(KundaliChart chart, Planet target)
        {
            if (GetPlanet(chart, target) == null)
            {
                return (null, $"{target} missing");
            }

            var directInfluences = chart.Aspects
                .Where(aspect => aspect.ToPlanet == target)
                .Select(aspect => aspect.FromPlanet)
                .ToList();

            var conjunctions = GetConjunctPlanets(chart, target);
            var influencingPlanets = directInfluences.Concat(conjunctions).Distinct().ToList();

            if (influencingPlanets.Count == 0)
            {
                var targetScore = PlanetScore(GetPlanet(chart, target));
                return (targetScore, $"{target} has no listed graha drishti/conjunction pressure; using own strength {targetScore}/100");
            }

            var beneficScores = influencingPlanets.Where(IsNaturalBenefic).Select(planet => PlanetScore(GetPlanet(chart, planet))).ToList();
            var maleficScores = influencingPlanets.Where(IsNaturalMaleficOrShadow).Select(planet => PlanetScore(GetPlanet(chart, planet))).ToList();
            var support = beneficScores.Count == 0 ? PlanetScore(GetPlanet(chart, target)) : AverageScores(beneficScores.ToArray());
            var pressure = maleficScores.Count == 0 ? support : 100 - AverageScores(maleficScores.ToArray());
            var score = AverageScores(support, pressure, PlanetScore(GetPlanet(chart, target)));
            var detail = $"{target} receives {beneficScores.Count} benefic and {maleficScores.Count} malefic/shadow influences; aspect condition {score}/100";

            return (score, detail);
        }

        private (int Score, string Detail) HouseAspectScore(KundaliChart chart, HouseNumber house)
        {
            var houseDto = GetHouse(chart, house);
            var score = houseDto?.Score ?? 0;
            var occupants = chart.PlanetPositions.Where(position => position.House == house).Select(position => position.Planet).ToList();
            var aspectingPlanets = chart.Aspects
                .Where(aspect => aspect.TargetHouse == (int)house)
                .Select(aspect => aspect.FromPlanet)
                .Distinct()
                .ToList();
            var planets = occupants.Concat(aspectingPlanets).Distinct().ToList();

            if (planets.Count == 0)
            {
                return (score, $"{house} has house score {score}/100 with no planet/aspect pressure listed");
            }

            var benefic = planets.Where(IsNaturalBenefic).Select(planet => PlanetScore(GetPlanet(chart, planet))).ToList();
            var malefic = planets.Where(IsNaturalMaleficOrShadow).Select(planet => PlanetScore(GetPlanet(chart, planet))).ToList();
            var support = benefic.Count == 0 ? score : AverageScores(score, AverageScores(benefic.ToArray()));
            var pressure = malefic.Count == 0 ? support : AverageScores(score, 100 - AverageScores(malefic.ToArray()));
            var final = AverageScores(support, pressure);

            return (final, $"{house} aspect condition {final}/100 from {benefic.Count} benefic and {malefic.Count} malefic/shadow contacts");
        }

        private (int Score, string Detail) NodeInfluenceScore(KundaliChart chart, IEnumerable<HouseNumber> houses)
        {
            var sensitive = houses.ToHashSet();
            var nodes = new[] { Planet.Rahu, Planet.Ketu }
                .Select(planet => GetPlanet(chart, planet))
                .Where(position => position != null)
                .Select(position => position!)
                .ToList();

            var hits = nodes
                .Where(position => sensitive.Contains(position.House)
                    || chart.Aspects.Any(aspect => aspect.FromPlanet == position.Planet && sensitive.Contains((HouseNumber)aspect.TargetHouse)))
                .ToList();

            if (hits.Count == 0)
            {
                var baseScore = AverageScores(sensitive.Select(house => HouseScore(chart, house)).ToArray());
                return (baseScore, $"No Rahu/Ketu contact on {string.Join(", ", sensitive.Select(h => (int)h))}; related house score {baseScore}/100");
            }

            var nodeStrength = AverageScores(hits.Select(position => PlanetScore(position)).ToArray());
            var houseStrength = AverageScores(sensitive.Select(house => HouseScore(chart, house)).ToArray());
            var score = AverageScores(100 - nodeStrength, houseStrength);
            return (score, $"Rahu/Ketu contact through {string.Join(", ", hits.Select(hit => $"{hit.Planet} house {(int)hit.House}"))}; node pressure score {score}/100");
        }

        private (int Score, string Detail) MarsDoshaScore(KundaliChart chart)
        {
            var mars = GetPlanet(chart, Planet.Mars);
            var moon = GetPlanet(chart, Planet.Moon);
            if (mars == null)
            {
                return (AverageHouseScore(chart, MarsDoshaHouses), "Mars missing; using sensitive marriage-house strength");
            }

            bool fromLagna = MarsDoshaHouses.Contains(mars.House);
            bool fromMoon = moon != null && MarsDoshaHouses.Select(house => (int)house).Contains(HouseFrom(moon.House, mars.House));
            var sensitiveHouseStrength = AverageHouseScore(chart, MarsDoshaHouses);

            if (!fromLagna && !fromMoon)
            {
                return (AverageScores(PlanetScore(mars), sensitiveHouseStrength), $"Mars in house {(int)mars.House}; no Mars Dosha sensitive placement");
            }

            var marsPressure = 100 - PlanetScore(mars);
            var protection = AverageScores(PlanetScore(GetPlanet(chart, Planet.Jupiter)), PlanetScore(GetPlanet(chart, Planet.Venus)), sensitiveHouseStrength);
            var score = AverageScores(marsPressure, protection);
            var sources = new List<string>();
            if (fromLagna) sources.Add($"from Lagna house {(int)mars.House}");
            if (fromMoon && moon != null) sources.Add($"{HouseFrom(moon.House, mars.House)}th from Moon");

            return (score, $"Mars Dosha indicated {string.Join(" and ", sources)}; Mars/protection score {score}/100");
        }

        private (int Score, string Detail) RajaYogaScore(KundaliChart chart)
        {
            var kendraLords = KendraHouses.Select(house => HouseLord(chart, house)).Where(lord => lord.HasValue).Select(lord => lord!.Value).Distinct();
            var trikonaLords = TrikonaHouses.Select(house => HouseLord(chart, house)).Where(lord => lord.HasValue).Select(lord => lord!.Value).Distinct();
            var associations = new List<int>();

            foreach (var kendraLord in kendraLords)
            {
                foreach (var trikonaLord in trikonaLords)
                {
                    if (kendraLord == trikonaLord)
                    {
                        continue;
                    }

                    var association = AssociationScore(chart, kendraLord, trikonaLord);
                    if (association.Score.HasValue)
                    {
                        associations.Add(association.Score.Value);
                    }
                }
            }

            var housePower = AverageScores(KendraHouses.Concat(TrikonaHouses).Distinct().Select(house => HouseScore(chart, house)).ToArray());
            if (associations.Count == 0)
            {
                return (housePower, $"No Kendra-Trikona lord association found; Kendra/Trikona house strength {housePower}/100");
            }

            var score = AverageScores(housePower, associations.Max());
            return (score, $"Kendra-Trikona lord association found; Raja Yoga signal {score}/100");
        }

        private (int Score, string Detail) DhanaYogaScore(KundaliChart chart)
        {
            var wealthHouses = new[] { HouseNumber.Second, HouseNumber.Fifth, HouseNumber.Ninth, HouseNumber.Eleventh };
            var lords = wealthHouses.Select(house => HouseLord(chart, house)).Where(lord => lord.HasValue).Select(lord => lord!.Value).Distinct().ToList();
            var associations = new List<int>();

            for (int i = 0; i < lords.Count; i++)
            {
                for (int j = i + 1; j < lords.Count; j++)
                {
                    var association = AssociationScore(chart, lords[i], lords[j]);
                    if (association.Score.HasValue)
                    {
                        associations.Add(association.Score.Value);
                    }
                }
            }

            var wealthPower = AverageScores(wealthHouses.Select(house => HouseScore(chart, house)).ToArray());
            if (associations.Count == 0)
            {
                return (wealthPower, $"No wealth-lord association found; Dhana houses average {wealthPower}/100");
            }

            var score = AverageScores(wealthPower, associations.Max());
            return (score, $"Wealth-lord association found; Dhana Yoga signal {score}/100");
        }

        private (int Score, string Detail) BeneficInfluenceScore(KundaliChart chart, IEnumerable<HouseNumber> houses)
        {
            var targetHouses = houses.ToHashSet();
            var relatedPlanets = chart.PlanetPositions
                .Where(position => targetHouses.Contains(position.House))
                .Select(position => position.Planet)
                .Concat(chart.Aspects.Where(aspect => targetHouses.Contains((HouseNumber)aspect.TargetHouse)).Select(aspect => aspect.FromPlanet))
                .Distinct()
                .ToList();

            var benefics = relatedPlanets.Where(IsNaturalBenefic).Select(planet => PlanetScore(GetPlanet(chart, planet))).ToList();
            var malefics = relatedPlanets.Where(IsNaturalMaleficOrShadow).Select(planet => PlanetScore(GetPlanet(chart, planet))).ToList();
            var housePower = AverageScores(targetHouses.Select(house => HouseScore(chart, house)).ToArray());
            var beneficSupport = benefics.Count == 0 ? housePower : AverageScores(housePower, AverageScores(benefics.ToArray()));
            var pressureAdjusted = malefics.Count == 0 ? beneficSupport : AverageScores(beneficSupport, 100 - AverageScores(malefics.ToArray()));

            return (pressureAdjusted, $"Benefic influence {pressureAdjusted}/100 from {benefics.Count} benefic and {malefics.Count} malefic/shadow contacts on growth houses");
        }

        private (int? Score, string Detail) AssociationScore(KundaliChart chart, Planet first, Planet second)
        {
            var firstPosition = GetPlanet(chart, first);
            var secondPosition = GetPlanet(chart, second);
            if (firstPosition == null || secondPosition == null)
            {
                return (null, $"{first}/{second} missing");
            }

            bool conjunction = firstPosition.House == secondPosition.House;
            bool aspect = chart.Aspects.Any(a => a.FromPlanet == first && a.ToPlanet == second)
                || chart.Aspects.Any(a => a.FromPlanet == second && a.ToPlanet == first);
            bool exchange = SignLord(firstPosition.Sign) == second && SignLord(secondPosition.Sign) == first;

            if (!conjunction && !aspect && !exchange)
            {
                return (null, $"{first}/{second} not associated");
            }

            var relationship = IntraChartPlanetRelationshipScore(chart, first, second);
            var score = AverageScores(relationship.Score, PlanetScore(firstPosition), PlanetScore(secondPosition));
            return (score, $"{first}/{second} association {score}/100 through {(conjunction ? "conjunction" : aspect ? "aspect" : "exchange")}");
        }

        private (int Score, string Detail) IntraChartPlanetRelationshipScore(KundaliChart chart, Planet first, Planet second)
        {
            var firstPosition = GetPlanet(chart, first);
            var secondPosition = GetPlanet(chart, second);
            if (firstPosition == null || secondPosition == null)
            {
                return (0, $"{first}/{second} missing");
            }

            var relationScore = PlanetRelationshipScore(firstPosition, secondPosition);
            var firstScore = PlanetScore(firstPosition);
            var secondScore = PlanetScore(secondPosition);
            var aspectOrConjunctionScore = chart.Aspects.Any(a => a.FromPlanet == first && a.ToPlanet == second)
                || chart.Aspects.Any(a => a.FromPlanet == second && a.ToPlanet == first)
                || firstPosition.House == secondPosition.House
                    ? AverageScores(firstScore, secondScore)
                    : PlanetRelationScore(first, second, firstScore, secondScore);
            var score = AverageScores(relationScore, aspectOrConjunctionScore);

            return (score, $"{first}-{second} relationship {score}/100 ({FormatPlanet(firstPosition)}; {FormatPlanet(secondPosition)})");
        }

        private int PlanetRelationshipScore(PlanetPosition first, PlanetPosition second)
        {
            var firstScore = PlanetScore(first);
            var secondScore = PlanetScore(second);
            var signRelation = PlanetRelationScore(first.Planet, SignLord(second.Sign), firstScore, secondScore);
            var reverseSignRelation = PlanetRelationScore(second.Planet, SignLord(first.Sign), secondScore, firstScore);
            var houseDistanceScore = HouseDistanceScore(first.House, second.House);
            var strengthAverage = AverageScores(firstScore, secondScore);
            return WeightedScore(new[]
            {
                ScoreComponentInput.Available("Sign lord relation", AverageScores(signRelation, reverseSignRelation), ""),
                ScoreComponentInput.Available("House distance", houseDistanceScore, ""),
                ScoreComponentInput.Available("Planet strength", strengthAverage, "")
            });
        }

        private int PlanetRelationScore(Planet from, Planet? to, int fromStrength, int toStrength)
        {
            if (!to.HasValue)
            {
                return fromStrength;
            }

            var fromMeta = _astrologyKnowledgeService.GetPlanet(from.ToString());
            if (fromMeta == null)
            {
                return AverageScores(fromStrength, toStrength);
            }

            var toName = to.Value.ToString();
            var relationshipStrength = AverageScores(fromStrength, toStrength);

            if (from == to.Value || fromMeta.Friends.Contains(toName))
            {
                return relationshipStrength;
            }

            if (fromMeta.Enemies.Contains(toName))
            {
                return 100 - relationshipStrength;
            }

            return AverageScores(relationshipStrength, 100 - Math.Abs(fromStrength - toStrength));
        }

        private int HouseDistanceScore(HouseNumber first, HouseNumber second)
        {
            var distance = HouseFrom(first, second);
            return distance switch
            {
                1 or 5 or 9 => AverageScores(HouseCategoryScore(first), HouseCategoryScore(second)),
                4 or 7 or 10 => AverageScores(HouseCategoryScore(first), HouseCategoryScore(second)),
                2 or 3 or 11 => AverageScores(HouseCategoryScore(first), HouseCategoryScore(second), 100 - Math.Abs((int)first - (int)second)),
                6 or 8 or 12 => 100 - AverageScores(HouseCategoryScore(first), HouseCategoryScore(second)),
                _ => AverageScores(HouseCategoryScore(first), HouseCategoryScore(second))
            };
        }

        private static int HouseCategoryScore(HouseNumber house)
        {
            return house switch
            {
                HouseNumber.First or HouseNumber.Fourth or HouseNumber.Fifth or HouseNumber.Seventh or HouseNumber.Ninth or HouseNumber.Tenth or HouseNumber.Eleventh => 80,
                HouseNumber.Second or HouseNumber.Third => 65,
                _ => 40
            };
        }

        private PlanetPosition? GetPlanet(KundaliChart chart, Planet planet)
        {
            return chart.PlanetPositions.FirstOrDefault(position => position.Planet == planet);
        }

        private int PlanetScore(PlanetPosition? position)
        {
            return position == null ? 0 : _planetScoreService.Calculate(position).Score;
        }

        private int AveragePlanetScore(KundaliChart chart, params Planet[] planets)
        {
            var scores = planets
                .Select(planet => GetPlanet(chart, planet))
                .Where(position => position != null)
                .Select(position => PlanetScore(position))
                .ToArray();

            return scores.Length == 0 ? 0 : AverageScores(scores);
        }

        private static int AverageScores(params int[] scores)
        {
            return scores.Length == 0 ? 0 : Clamp((int)Math.Round(scores.Average()));
        }

        private static int AverageHouseScore(KundaliChart chart, IEnumerable<HouseNumber> houseNumbers)
        {
            var scores = houseNumbers.Select(house => HouseScore(chart, house)).Where(score => score > 0).ToArray();
            return scores.Length == 0 ? 0 : AverageScores(scores);
        }

        private (int? Score, string Detail) HouseLordScore(KundaliChart chart, HouseNumber houseNumber)
        {
            var lord = HouseLord(chart, houseNumber);
            if (!lord.HasValue)
            {
                return (null, $"{houseNumber} lord unavailable");
            }

            var position = GetPlanet(chart, lord.Value);
            if (position == null)
            {
                return (null, $"{houseNumber} lord {lord.Value} missing");
            }

            return (PlanetScore(position), $"{houseNumber} lord {lord.Value} score {PlanetScore(position)}/100 in house {(int)position.House}");
        }

        private Planet? HouseLord(KundaliChart chart, HouseNumber houseNumber)
        {
            var house = GetHouse(chart, houseNumber);
            if (house == null || string.IsNullOrWhiteSpace(house.HouseOwner))
            {
                return null;
            }

            return Enum.TryParse<Planet>(house.HouseOwner, ignoreCase: true, out var lord)
                ? lord
                : null;
        }

        private static int HouseScore(KundaliChart chart, HouseNumber houseNumber)
        {
            return GetHouse(chart, houseNumber)?.Score ?? 0;
        }

        private static HouseDto? GetHouse(KundaliChart chart, HouseNumber houseNumber)
        {
            return chart.Houses.FirstOrDefault(house => house.HouseNumber == houseNumber);
        }

        private Planet? SignLord(ZodiacSign sign)
        {
            var signName = sign.ToString();
            var planet = _astrologyKnowledgeService.GetPlanets()
                .FirstOrDefault(item => item.OwnSigns.Any(ownSign => ownSign.Equals(signName, StringComparison.OrdinalIgnoreCase)));

            return planet != null && Enum.TryParse<Planet>(planet.Name, ignoreCase: true, out var lord)
                ? lord
                : null;
        }

        private bool IsNaturalBenefic(Planet planet)
        {
            var meta = _astrologyKnowledgeService.GetPlanet(planet.ToString());
            return meta?.Classification.Equals("NaturalBenefic", StringComparison.OrdinalIgnoreCase) == true;
        }

        private bool IsNaturalMaleficOrShadow(Planet planet)
        {
            var meta = _astrologyKnowledgeService.GetPlanet(planet.ToString());
            return meta?.Classification.Equals("NaturalMalefic", StringComparison.OrdinalIgnoreCase) == true
                || meta?.Classification.Equals("ShadowPlanet", StringComparison.OrdinalIgnoreCase) == true;
        }

        private static bool IsDusthana(HouseNumber house)
        {
            return house is HouseNumber.Sixth or HouseNumber.Eighth or HouseNumber.Twelfth;
        }

        private static IEnumerable<Planet> GetConjunctPlanets(KundaliChart chart, Planet target)
        {
            var targetPosition = chart.PlanetPositions.FirstOrDefault(position => position.Planet == target);
            if (targetPosition == null)
            {
                return Enumerable.Empty<Planet>();
            }

            return chart.PlanetPositions
                .Where(position => position.Planet != target && position.House == targetPosition.House)
                .Select(position => position.Planet);
        }

        private static int HouseFrom(HouseNumber referenceHouse, HouseNumber targetHouse)
        {
            var distance = ((int)targetHouse - (int)referenceHouse + 12) % 12 + 1;
            return distance;
        }

        private static int WeightedScore(IEnumerable<ScoreComponentInput> components)
        {
            var items = components.Where(item => item.Score.HasValue).ToList();
            var totalWeight = items.Sum(item => item.Weight);
            if (items.Count == 0 || totalWeight <= 0)
            {
                return 0;
            }

            return Clamp((int)Math.Round(items.Sum(item => item.Score!.Value * item.Weight) / totalWeight));
        }

        private static void AddComponentFactors(CompatibilityDimension dimension, IEnumerable<ScoreComponentInput> components)
        {
            var items = components.Where(item => item.Score.HasValue).ToList();
            var totalWeight = items.Sum(item => item.Weight);

            foreach (var component in items)
            {
                var normalizedWeight = totalWeight <= 0 ? 0 : component.Weight / totalWeight;
                var weightedImpact = normalizedWeight <= 0
                    ? 0
                    : (int)Math.Round((component.Score!.Value - 50) * normalizedWeight);
                var impactPrefix = weightedImpact > 0 ? $"+{weightedImpact}" : weightedImpact.ToString();
                dimension.Factors.Add($"{impactPrefix} {component.Label}: {component.Score}/100, weight {normalizedWeight:0.##}. {component.Detail}");
            }
        }

        private static List<ScoreComponent> BuildScoreBreakdown(IEnumerable<ScoreComponentInput> components)
        {
            var items = components.Where(item => item.Score.HasValue).ToList();
            var totalWeight = items.Sum(item => item.Weight);

            if (items.Count == 0 || totalWeight <= 0)
            {
                return new List<ScoreComponent>();
            }

            return items
                .Select(item =>
                {
                    var normalizedWeight = item.Weight / totalWeight;
                    return new ScoreComponent
                    {
                        Label = item.Label,
                        Score = item.Score!.Value,
                        Weight = Math.Round(normalizedWeight, 4),
                        Contribution = Math.Round(item.Score.Value * normalizedWeight, 2)
                    };
                })
                .ToList();
        }

        private static string BuildSummary(string name, int score)
        {
            if (score >= 70) return $"{name} is strongly supported by Kundali indicators.";
            if (score >= 50) return $"{name} is moderate, with mixed Kundali support and pressure.";
            return $"{name} needs conscious effort based on Kundali indicators.";
        }

        private static int ConfidenceFromData(List<ScoreComponentInput> components, KundaliChart chartA, KundaliChart chartB)
        {
            var expected = components.Count;
            var available = components.Count(component => component.Score.HasValue);
            var completeness = expected == 0 ? 0 : (int)Math.Round((double)available / expected * 100);
            var chartCompleteness = AverageScores(
                Percent(chartA.PlanetPositions.Count, 9),
                Percent(chartB.PlanetPositions.Count, 9),
                Percent(chartA.Houses.Count, 12),
                Percent(chartB.Houses.Count, 12));

            return AverageScores(completeness, chartCompleteness);
        }

        private static int Percent(double score, double max)
        {
            if (max <= 0)
            {
                return 0;
            }

            return Clamp((int)Math.Round(score / max * 100));
        }

        private static string FormatPlanet(PlanetPosition position)
        {
            return $"{position.Planet} in {position.Sign}, {position.Nakshatra} pada {position.Pada}, house {(int)position.House}";
        }

        private static string HouseDetail(KundaliChart chart, HouseNumber houseNumber)
        {
            var house = GetHouse(chart, houseNumber);
            return house == null
                ? $"{houseNumber}: not available"
                : $"{house.HouseName} in {house.Sign}, lord {house.HouseOwner}, score {house.Score}/100";
        }

        private void ExtractDominantThemes(CompatibilityResult result)
        {
            if (result.Dimensions.Count == 0)
                return;

            result.DominantStrengths = result.Dimensions
                .OrderByDescending(d => d.Score)
                .Take(2)
                .Select(d => d.Name)
                .ToList();

            result.DominantChallenges = result.Dimensions
                .OrderBy(d => d.Score)
                .Take(2)
                .Select(d => d.Name)
                .ToList();

            result.Factors.Add($"Overall compatibility: {result.OverallCompatibilityScore}/100");
            result.Factors.AddRange(
                result.Dimensions
                    .SelectMany(d => d.Factors)
                    .Distinct()
            );
        }

        private string GenerateSummary(CompatibilityResult result)
        {
            var template = $"Compatibility Analysis: {result.PersonAName} & {result.PersonBName}";

            if (result.OverallCompatibilityScore >= 70)
                return $"{template} - High compatibility potential (Score: {result.OverallCompatibilityScore}/100). Favorable Kundali foundation for relationship growth.";
            else if (result.OverallCompatibilityScore >= 50)
                return $"{template} - Moderate compatibility with both strengths and challenges (Score: {result.OverallCompatibilityScore}/100). Growth through mutual effort.";
            else
                return $"{template} - Compatibility requires awareness and conscious effort (Score: {result.OverallCompatibilityScore}/100). Focus on mutual support.";
        }

        private static int Clamp(int score)
        {
            return Math.Clamp(score, 0, 100);
        }

        private sealed record ScoreComponentInput(string Label, int? Score, string Detail, double Weight)
        {
            public static ScoreComponentInput Available(string label, int score, string detail, double weight = 1)
            {
                return new ScoreComponentInput(label, Clamp(score), detail, weight);
            }

            public static ScoreComponentInput Unavailable(string label, string detail, double weight = 1)
            {
                return new ScoreComponentInput(label, null, detail, weight);
            }
        }
    }
}
