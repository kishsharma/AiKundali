namespace KundaliEngine.API.DTOs.Responses
{
    public class FormationActivationPeriodsDto
    {
        public List<YogaDoshaActivationDto> CurrentlyActive { get; set; } = new();
        public List<YogaDoshaActivationDto> Upcoming { get; set; } = new();
        public List<YogaDoshaActivationDto> Past { get; set; } = new();
    }

    public class YogaDoshaActivationDto
    {
        public string FormationKey { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string MahadashaPlanet { get; set; } = string.Empty;
        public string AntardashaPlanet { get; set; } = string.Empty;
        public string ActivationLevel { get; set; } = string.Empty;
        public bool IsCurrentlyActive { get; set; }
        public List<string> TriggeredBy { get; set; } = new();
    }
}
