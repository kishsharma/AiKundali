using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.KundaliYoga;
using KundaliEngine.Domain.Models;
using KundaliHouse = KundaliEngine.Domain.Models.HouseDto;

namespace KundaliEngine.Astrology.Services.KundaliDosha
{
    public class DoshaDetectionService
    {
        private readonly IReadOnlyList<IDoshaRule> _rules;
        private readonly AspectService _aspectService;
        private readonly PlanetStrengthService _planetStrengthService;

        public DoshaDetectionService(
            IEnumerable<IDoshaRule> rules,
            AspectService aspectService,
            PlanetStrengthService planetStrengthService)
        {
            _rules = rules?.ToList() ?? throw new ArgumentNullException(nameof(rules));
            _aspectService = aspectService ?? throw new ArgumentNullException(nameof(aspectService));
            _planetStrengthService = planetStrengthService ?? throw new ArgumentNullException(nameof(planetStrengthService));
        }

        public List<DoshaRuleMatch> DetectDoshas(List<PlanetPosition> planets, List<KundaliHouse> houses)
        {
            if (planets is null)
            {
                throw new ArgumentNullException(nameof(planets));
            }

            if (houses is null)
            {
                throw new ArgumentNullException(nameof(houses));
            }

            var context = new YogaEvaluationContext(planets, houses, _aspectService, _planetStrengthService);

            return _rules
                .Select(rule => rule.Evaluate(context))
                .Where(match => match != null)
                .GroupBy(match => match!.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First()!)
                .ToList();
        }
    }
}
