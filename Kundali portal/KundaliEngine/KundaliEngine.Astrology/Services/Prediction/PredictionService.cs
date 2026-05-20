using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.KnowledgeBase.Models;
using KundaliEngine.KnowledgeBase.Services;

namespace KundaliEngine.Astrology.Services.Prediction
{
    public class PredictionService
    {
        private readonly AstrologyKnowledgeService _knowledgeService;

        public PredictionService()
            : this(new AstrologyKnowledgeService())
        {
        }

        public PredictionService(AstrologyKnowledgeService knowledgeService)
        {
            _knowledgeService = knowledgeService;
        }

        public string GetPrediction(PlanetPosition planet, PlanetStrength strength)
        {
            HouseKnowledge house = GetHouseKnowledge(planet.House);
            PlanetKnowledge planetKnowledge = GetPlanetKnowledge(planet.Planet);
            string strengthModifier = GetStrengthModifier(strength);

            return BuildPrediction(planet, house, planetKnowledge, strengthModifier);
        }

        public string GetDashaPrediction(Planet planet)
        {
            PlanetKnowledge planetKnowledge = GetPlanetKnowledge(planet);

            return HasText(planetKnowledge.DashaPrediction)
                ? planetKnowledge.DashaPrediction
                : "activates important karmic themes.";
        }

        public string GetDashaActivatedPrediction(PlanetPosition planetPosition, Planet planet, PlanetStrength strength)
        {
            HouseKnowledge house = GetHouseKnowledge(planetPosition.House);
            PlanetKnowledge planetKnowledge = GetPlanetKnowledge(planet);
            string strengthModifier = GetStrengthModifier(strength);

            return BuildDashaActivatedPrediction(
                planet,
                planetPosition,
                house,
                planetKnowledge,
                strengthModifier);
        }

        public string GetCombinedDashaPrediction(Planet maha, Planet antar)
        {
            string mahaTheme = GetShortDashaTheme(maha);
            string antarTheme = GetShortDashaTheme(antar);

            return
                $"{mahaTheme} become strongly influenced by " +
                $"{antarTheme.ToLower()}. " +
                $"This period may combine both planetary energies " +
                $"and create important karmic developments.";
        }

        private string BuildPrediction(
            PlanetPosition planet,
            HouseKnowledge house,
            PlanetKnowledge planetKnowledge,
            string strengthModifier)
        {
            string opening = $"{planet.Planet} in the {planet.House} house brings {GetPredictionNature(planetKnowledge)} into the area of {GetPredictionArea(house, planet.House)}.";
            string constructive = $"When expressed constructively, it can {GetPredictionExpression(house)} through {GetPositiveOutcome(planetKnowledge)}.";
            string difficult = $"If this energy becomes unbalanced, it may show as {GetNegativeOutcome(planetKnowledge)}.";

            return JoinSentences(opening, constructive, difficult, strengthModifier);
        }

        private string BuildDashaActivatedPrediction(
            Planet planet,
            PlanetPosition planetPosition,
            HouseKnowledge house,
            PlanetKnowledge planetKnowledge,
            string strengthModifier)
        {
            string opening = $"During the {planet} dasha, the planet activates {GetPredictionNature(planetKnowledge)} through the {planetPosition.House} house.";
            string houseFocus = $"This makes {GetPredictionArea(house, planetPosition.House)} more active, so life may bring events that {GetPredictionExpression(house)}.";
            string constructive = $"On the constructive side, this period can support {GetPositiveOutcome(planetKnowledge)}.";
            string difficult = $"If handled unconsciously, the same energy may create {GetNegativeOutcome(planetKnowledge)}.";

            return JoinSentences(opening, houseFocus, constructive, difficult, strengthModifier);
        }

        private HouseKnowledge GetHouseKnowledge(HouseNumber house)
        {
            return _knowledgeService.GetHouse((int)house) ?? new HouseKnowledge
            {
                Number = (int)house,
                Name = $"House {house}",
                PredictionArea = $"house {house}",
                PredictionExpression = "influence the matters of this house"
            };
        }

        private PlanetKnowledge GetPlanetKnowledge(Planet planet)
        {
            return _knowledgeService.GetPlanet(planet.ToString()) ?? new PlanetKnowledge
            {
                Name = planet.ToString(),
                PredictionNature = "general life influences",
                PositiveOutcome = "growth and learning through this placement",
                NegativeOutcome = "confusion or imbalance when the energy is not understood",
                ShortDashaTheme = "important life themes"
            };
        }

        private string GetStrengthModifier(PlanetStrength strength)
        {
            return _knowledgeService.GetStrength(strength.ToString())?.PredictionModifier ?? string.Empty;
        }

        private static string GetPredictionArea(HouseKnowledge house, HouseNumber houseNumber)
        {
            return HasText(house.PredictionArea)
                ? house.PredictionArea
                : $"house {houseNumber}";
        }

        private static string GetPredictionExpression(HouseKnowledge house)
        {
            return HasText(house.PredictionExpression)
                ? house.PredictionExpression
                : "influence the matters of this house";
        }

        private static string GetPredictionNature(PlanetKnowledge planet)
        {
            return HasText(planet.PredictionNature)
                ? planet.PredictionNature
                : "general life influences";
        }

        private static string GetPositiveOutcome(PlanetKnowledge planet)
        {
            return HasText(planet.PositiveOutcome)
                ? planet.PositiveOutcome
                : "growth and learning through this placement";
        }

        private static string GetNegativeOutcome(PlanetKnowledge planet)
        {
            return HasText(planet.NegativeOutcome)
                ? planet.NegativeOutcome
                : "confusion or imbalance when the energy is not understood";
        }

        private string GetShortDashaTheme(Planet planet)
        {
            PlanetKnowledge planetKnowledge = GetPlanetKnowledge(planet);

            return HasText(planetKnowledge.ShortDashaTheme)
                ? planetKnowledge.ShortDashaTheme
                : "important life themes";
        }

        private static string JoinSentences(params string[] parts)
        {
            return string.Join(" ", parts.Where(HasText));
        }

        private static bool HasText(string? value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }
    }
}
