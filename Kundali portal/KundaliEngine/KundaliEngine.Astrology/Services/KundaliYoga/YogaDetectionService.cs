using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Domain.Models;
using KundaliEngine.KnowledgeBase.Services;
using KundaliHouse = KundaliEngine.Domain.Models.HouseDto;

namespace KundaliEngine.Astrology.Services.KundaliYoga
{
    public class YogaDetectionService
    {
        private readonly IReadOnlyList<IYogaRule> _rules;
        private readonly AspectService _aspectService;
        private readonly PlanetStrengthService _planetStrengthService;
        private readonly YogaMetadataService _yogaMetadataService;

        public YogaDetectionService(
            IEnumerable<IYogaRule> rules,
            AspectService aspectService,
            PlanetStrengthService planetStrengthService,
            YogaMetadataService yogaMetadataService)
        {
            _rules = rules?.ToList() ?? throw new ArgumentNullException(nameof(rules));
            _aspectService = aspectService ?? throw new ArgumentNullException(nameof(aspectService));
            _planetStrengthService = planetStrengthService ?? throw new ArgumentNullException(nameof(planetStrengthService));
            _yogaMetadataService = yogaMetadataService ?? throw new ArgumentNullException(nameof(yogaMetadataService));
        }

        public List<YogaRuleMatch> DetectYogas(List<PlanetPosition> planets, List<KundaliHouse> houses)
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
            var metadataKeys = _yogaMetadataService.GetAllYogas()
                .Select(yoga => yoga.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return _rules
                .Where(rule => metadataKeys.Contains(rule.Key))
                .Select(rule => rule.Evaluate(context))
                .Where(match => match != null)
                .GroupBy(match => match!.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First()!)
                .ToList();
        }
    }
}
