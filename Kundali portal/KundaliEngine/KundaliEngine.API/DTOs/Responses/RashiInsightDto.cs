namespace KundaliEngine.API.DTOs.Responses
{
    public class RashiInsightDto
    {
        public string Name { get; set; } = string.Empty;
        public string EnglishName { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Ruler { get; set; } = string.Empty;
        public string Element { get; set; } = string.Empty;
        public string Strength { get; set; } = string.Empty;
        public string Weakness { get; set; } = string.Empty;
        public string Gemstone { get; set; } = string.Empty;
        public string Metal { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public List<int> LuckyNumbers { get; set; } = new();
        public string FavorableDay { get; set; } = string.Empty;
        public string FavorableDirection { get; set; } = string.Empty;
        public GhatChakraDto? GhatChakra { get; set; }
    }

    public class GhatChakraDto
    {
        public string Month { get; set; } = string.Empty;
        public string Tithi { get; set; } = string.Empty;
        public string TithiNumbers { get; set; } = string.Empty;
        public string Weekday { get; set; } = string.Empty;
        public string Nakshatra { get; set; } = string.Empty;
        public string Yoga { get; set; } = string.Empty;
        public string Karana { get; set; } = string.Empty;
        public string MoonMale { get; set; } = string.Empty;
        public string MoonFemale { get; set; } = string.Empty;
    }
}
