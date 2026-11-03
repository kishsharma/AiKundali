using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.KnowledgeBase.Services;
using KundaliEngine.Astrology.Services.Scoring;

namespace KundaliEngine.Astrology.Services.Compatibility
{
    public class CompatibilityHouseAnalysisService
    {
        private static readonly (HouseNumber House, string Theme)[] RelevantHouses =
        {
            (HouseNumber.Second, "compatibility.houseTheme.2"),
            (HouseNumber.Fourth, "compatibility.houseTheme.4"),
            (HouseNumber.Fifth, "compatibility.houseTheme.5"),
            (HouseNumber.Sixth, "compatibility.houseTheme.6"),
            (HouseNumber.Seventh, "compatibility.houseTheme.7"),
            (HouseNumber.Eighth, "compatibility.houseTheme.8"),
            (HouseNumber.Twelfth, "compatibility.houseTheme.12")
        };

        private readonly AstrologyKnowledgeService _knowledge;
        private readonly PlanetScoreService _planetScore;

        public CompatibilityHouseAnalysisService(AstrologyKnowledgeService knowledge)
        {
            _knowledge = knowledge;
            _planetScore = new PlanetScoreService(knowledge);
        }

        public List<RelationshipHouseAnalysis> Analyze(KundaliChart? personA, KundaliChart? personB)
        {
            return RelevantHouses.Select(item =>
            {
                var evidenceA = BuildEvidence(personA, item.House);
                var evidenceB = BuildEvidence(personB, item.House);
                var combined = evidenceA.SupportScore.HasValue && evidenceB.SupportScore.HasValue
                    ? (int)Math.Round((evidenceA.SupportScore.Value + evidenceB.SupportScore.Value) / 2.0)
                    : (int?)null;

                return new RelationshipHouseAnalysis
                {
                    HouseNumber = (int)item.House,
                    ThemeKey = item.Theme,
                    PersonA = evidenceA,
                    PersonB = evidenceB,
                    CombinedScore = combined,
                    ConcernLevel = combined.HasValue ? ConcernFor(combined.Value) : "Unavailable"
                };
            }).ToList();
        }

        private RelationshipHouseEvidence BuildEvidence(KundaliChart? chart, HouseNumber houseNumber)
        {
            var house = chart?.Houses?.FirstOrDefault(item => item.HouseNumber == houseNumber);
            if (chart == null || house == null)
            {
                return new RelationshipHouseEvidence();
            }

            var houseLord = ParsePlanet(house.HouseOwner);
            var lordPosition = houseLord.HasValue
                ? chart.PlanetPositions.FirstOrDefault(position => position.Planet == houseLord.Value)
                : null;
            var lordStrength = lordPosition == null ? (int?)null : _planetScore.Calculate(lordPosition).Score;

            var occupants = house.Occupants.Distinct().ToList();
            var aspecting = chart.Aspects
                .Where(aspect => aspect.TargetHouse == (int)houseNumber)
                .Select(aspect => aspect.FromPlanet)
                .Distinct()
                .ToList();
            var influencePlanets = occupants.Concat(aspecting).Distinct().ToList();
            var benefics = influencePlanets.Where(IsNaturalBenefic)
                .Select(planet => FormatInfluence(chart, planet, occupants.Contains(planet), aspecting.Contains(planet))).ToList();
            var malefics = influencePlanets.Where(IsNaturalMalefic)
                .Select(planet => FormatInfluence(chart, planet, occupants.Contains(planet), aspecting.Contains(planet))).ToList();
            var mitigations = new List<string>();

            if (house.HasBeneficAspect && benefics.Count == 0)
            {
                mitigations.Add("A benefic aspect is recorded on this house.");
            }
            if (house.HasMaleficAspect && malefics.Count == 0)
            {
                malefics.Add("Malefic aspect recorded; the influencing planet was not identified in chart aspect data.");
            }
            if (benefics.Count > 0)
            {
                mitigations.Add($"Natural benefic contact: {string.Join(", ", benefics)}.");
            }
            if (lordStrength >= 65 && houseLord.HasValue)
            {
                mitigations.Add($"The house lord {houseLord.Value} has supportive calculated strength ({lordStrength}/100).");
            }

            var supportScore = Math.Clamp(house.Score, 0, 100);
            return new RelationshipHouseEvidence
            {
                IsAvailable = true,
                SupportScore = supportScore,
                ConcernLevel = ConcernFor(supportScore),
                HouseLord = houseLord?.ToString() ?? house.HouseOwner,
                HouseLordStrength = lordStrength,
                Occupants = occupants.Select(planet => planet.ToString()).ToList(),
                BeneficInfluences = benefics,
                MaleficInfluences = malefics,
                MitigatingFactors = mitigations,
                Factors = house.Factors?.ToList() ?? new List<string>()
            };
        }

        private bool IsNaturalBenefic(Planet planet) =>
            _knowledge.GetPlanet(planet.ToString())?.Classification.Equals("NaturalBenefic", StringComparison.OrdinalIgnoreCase) == true;

        private bool IsNaturalMalefic(Planet planet)
        {
            var classification = _knowledge.GetPlanet(planet.ToString())?.Classification;
            return classification?.Equals("NaturalMalefic", StringComparison.OrdinalIgnoreCase) == true
                || classification?.Equals("ShadowPlanet", StringComparison.OrdinalIgnoreCase) == true;
        }

        private string FormatInfluence(KundaliChart chart, Planet planet, bool isOccupant, bool aspectsHouse)
        {
            var position = chart.PlanetPositions.FirstOrDefault(item => item.Planet == planet);
            var roles = new List<string>();
            if (isOccupant) roles.Add("occupant");
            if (aspectsHouse) roles.Add("aspects this house");
            var strength = position == null ? "strength unavailable" : $"strength {_planetScore.Calculate(position).Score}/100";
            return $"{planet} ({string.Join("; ", roles)}; {strength})";
        }

        private static Planet? ParsePlanet(string? name) =>
            Enum.TryParse<Planet>(name, ignoreCase: true, out var planet) ? planet : null;

        private static string ConcernFor(int supportScore) => supportScore switch
        {
            >= 65 => "Low",
            >= 40 => "Moderate",
            _ => "High"
        };
    }
}
