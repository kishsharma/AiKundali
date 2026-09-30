using KundaliEngine.Astrology.Services.DivisionalCharts.Calculators;
using KundaliEngine.Astrology.Services.DivisionalCharts.Interfaces;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.Domain.Models.DivisionalCharts;

namespace KundaliEngine.Astrology.Services.DivisionalCharts
{
    public class DivisionalChartService
    {
        private readonly Dictionary<DivisionalChartType, IDivisionalChartCalculator> _calculators;

        public DivisionalChartService()
        {
            _calculators = new Dictionary<DivisionalChartType, IDivisionalChartCalculator>
            {
                { DivisionalChartType.D1, new RashiCalculator() },
                { DivisionalChartType.D2, new HoraCalculator() },
                { DivisionalChartType.D3, new DrekkanaCalculator() },
                { DivisionalChartType.D4, new ChaturthamsaCalculator() },
                { DivisionalChartType.D7, new SaptamsaCalculator() },
                { DivisionalChartType.D9, new NavamsaCalculator() },
                { DivisionalChartType.D10, new DashamsaCalculator() },
                { DivisionalChartType.D12, new DwadashamsaCalculator() },
                { DivisionalChartType.D16, new ShodasamsaCalculator() },
                { DivisionalChartType.D20, new VimsamsaCalculator() },
                { DivisionalChartType.D24, new SiddhamsaCalculator() },
                { DivisionalChartType.D27, new BhamsaCalculator() },
                { DivisionalChartType.D30, new TrimsamsaCalculator() },
                { DivisionalChartType.D40, new KhavedamsaCalculator() },
                { DivisionalChartType.D45, new AkshavedamsaCalculator() },
                { DivisionalChartType.D60, new ShashtiamsaCalculator() }
            };
        }

        public IReadOnlyCollection<DivisionalChartType> SupportedChartTypes => _calculators.Keys.ToList();

        public DivisionalChart GenerateDivisionalChart(KundaliChart baseChart, DivisionalChartType chartType)
        {
            return GenerateDivisionalChart(baseChart, chartType, baseChart.AscendantDegree);
        }

        public DivisionalChart GenerateDivisionalChart( KundaliChart baseChart, DivisionalChartType chartType, double ascendantDegreeInSign)
        {
            if (!_calculators.TryGetValue(chartType, out var calculator))
            {
                throw new NotImplementedException($"{chartType} calculator not implemented.");
            }

            var divisionalPlanets = calculator.Calculate(baseChart.PlanetPositions);
            var divisionalAscendantSign = calculator.CalculateSign(baseChart.AscendantSign, ascendantDegreeInSign);
            var ascendantDegree = (int)Math.Floor(ascendantDegreeInSign);

            foreach (var planet in divisionalPlanets)
            {
                planet.House = CalculateHouseFromAscendant(planet.Sign, divisionalAscendantSign);
            }

            return new DivisionalChart
            {
                ChartType = chartType,
                AscendantSign = divisionalAscendantSign,
                AscendantDegree = ascendantDegree,
                PlanetPositions = divisionalPlanets,
                Description = GetDescription(chartType)
            };
        }

        private static HouseNumber CalculateHouseFromAscendant(ZodiacSign sign, ZodiacSign ascendantSign)
        {
            var house = (((int)sign - (int)ascendantSign + 12) % 12) + 1;
            return (HouseNumber)house;
        }

        private static string GetDescription(DivisionalChartType chartType)
        {
            return chartType switch
            {
                DivisionalChartType.D1 => "Rashi birth chart",
                DivisionalChartType.D2 => "Hora divisional chart",
                DivisionalChartType.D3 => "Drekkana divisional chart",
                DivisionalChartType.D4 => "Chaturthamsa divisional chart",
                DivisionalChartType.D7 => "Saptamsa divisional chart",
                DivisionalChartType.D9 => "Navamsa divisional chart",
                DivisionalChartType.D10 => "Dashamsa divisional chart",
                DivisionalChartType.D12 => "Dwadashamsa divisional chart",
                DivisionalChartType.D16 => "Shodasamsa divisional chart",
                DivisionalChartType.D20 => "Vimsamsa divisional chart",
                DivisionalChartType.D24 => "Siddhamsa divisional chart",
                DivisionalChartType.D27 => "Bhamsa divisional chart",
                DivisionalChartType.D30 => "Trimsamsa divisional chart",
                DivisionalChartType.D40 => "Khavedamsa divisional chart",
                DivisionalChartType.D45 => "Akshavedamsa divisional chart",
                DivisionalChartType.D60 => "Shashtiamsa divisional chart",
                _ => $"{chartType} divisional chart"
            };
        }
    }
}
