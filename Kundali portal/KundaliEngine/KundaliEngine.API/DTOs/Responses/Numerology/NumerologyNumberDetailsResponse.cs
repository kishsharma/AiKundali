namespace KundaliEngine.API.DTOs.Responses.Numerology
{
    public class NumerologyNumberDetailsResponse
    {
        public string Summary { get; set; } = string.Empty;

        public List<string> Traits { get; set; } = [];

        public List<string> Strengths { get; set; } = [];

        public List<string> Challenges { get; set; } = [];

        public List<int> FavourableNumbers { get; set; } = [];

        public List<string> FavourableDays { get; set; } = [];

        public List<string> FavourableColours { get; set; } = [];
    }
}
