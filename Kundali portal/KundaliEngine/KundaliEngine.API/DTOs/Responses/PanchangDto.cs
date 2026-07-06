namespace KundaliEngine.API.DTOs.Responses
{
    public class PanchangDto
    {
        // Lunar Day
        public string Tithi { get; set; } = string.Empty;

        public string Paksha { get; set; } = string.Empty;

        // Star
        public string Nakshatra { get; set; } = string.Empty;

        public int Pada { get; set; }

        // Yogas & Karana
        public string Yoga { get; set; } = string.Empty;

        public string Karana { get; set; } = string.Empty;

        // Weekday
        public string Vaar { get; set; } = string.Empty;

        // Lunar Month
        public string Maas { get; set; } = string.Empty;

        // New: Shaka Samvat year
        public string Shake { get; set; } = string.Empty;

        // New: Vikram Samvat year
        public string Samvat { get; set; } = string.Empty;

        // Optional Future Support
        public bool IsAuspicious { get; set; }

        public string Summary { get; set; } = string.Empty;
    }
}