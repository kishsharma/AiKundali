using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.DivisionalCharts.Calculators;
using KundaliEngine.Astrology.Services.DivisionalCharts.Interfaces;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.Domain.Models.DivisionalCharts;
namespace KundaliEngine.Astrology.Services.DivisionalCharts
{
    public class DivisionalChartService
    {
        private readonly HouseService _houseService;

        private readonly Dictionary< DivisionalChartType,
            IDivisionalChartCalculator> _calculators;

        public DivisionalChartService()
        {
            _houseService = new HouseService();

            _calculators =new Dictionary<DivisionalChartType,IDivisionalChartCalculator>
            {
            {DivisionalChartType.D9,new NavamsaCalculator()}
            };
        }

        public DivisionalChart GenerateDivisionalChart(  KundaliChart baseChart, DivisionalChartType chartType)
        {
            if (!_calculators.ContainsKey(chartType))
            {
                throw new NotImplementedException(
                    $"{chartType} calculator not implemented.");
            }

            var calculator =
                _calculators[chartType];

            var divisionalPlanets =
                calculator.Calculate(
                    baseChart.PlanetPositions);

            return new DivisionalChart
            {
                ChartType = chartType,

                PlanetPositions = divisionalPlanets,

                Description =
                    $"{chartType} divisional chart"
            };
        }
    }
}