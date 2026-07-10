namespace KundaliEngine.API.DTOs.Responses
{
    public class TransitInsightDto
    {
        public DateTime CalculatedAtUtc { get; set; }

        public string LocationMode { get; set; } = string.Empty;

        public string Place { get; set; } = string.Empty;

        public string NatalAscendant { get; set; } = string.Empty;

        public List<PlanetDto> Planets { get; set; } = new();

        public List<TransitActivationDto> Activations { get; set; } = new();
    }
}
