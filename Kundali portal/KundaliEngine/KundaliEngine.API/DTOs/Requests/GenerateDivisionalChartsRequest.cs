using KundaliEngine.Domain.Enums;

namespace KundaliEngine.API.DTOs.Requests
{
    public class GenerateDivisionalChartsRequest : GenerateKundaliRequest
    {
        public List<DivisionalChartType> ChartTypes { get; set; } = new();
    }
}
