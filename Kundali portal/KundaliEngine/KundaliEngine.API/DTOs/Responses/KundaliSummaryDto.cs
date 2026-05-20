namespace KundaliEngine.API.DTOs.Responses
{
    public class KundaliSummaryDto
    {
        // Person identity
        public string Name { get; set; } = string.Empty;

        // Core identity
        public string Ascendant { get; set; } = string.Empty;

        // Planetary quick signs
        public string MoonSign { get; set; } = string.Empty;
        public string SunSign { get; set; } = string.Empty;

        // Panchang
        public string Nakshatra { get; set; } = string.Empty;
        public string Tithi { get; set; } = string.Empty;

        // Current time activation
        public string CurrentMahadasha { get; set; } = string.Empty;
        public string CurrentAntardasha { get; set; } = string.Empty;

        // Quick interpretation
        public string Summary { get; set; } = string.Empty;
    }
}