namespace KundaliEngine.API.DTOs.Responses
{
    public class TransitActivationDto
    {
        public string Planet { get; set; } = string.Empty;

        public string Sign { get; set; } = string.Empty;

        public int NatalHouse { get; set; }

        public int HouseFromMoon { get; set; }

        public string FocusArea { get; set; } = string.Empty;

        public List<string> NatalPlanetsInHouse { get; set; } = new();

        public string NatalHouseLord { get; set; } = string.Empty;

        public bool IsMahadashaPlanet { get; set; }

        public bool IsAntardashaPlanet { get; set; }

        public bool ActivatesNatalMahadashaPlanet { get; set; }

        public bool ActivatesNatalAntardashaPlanet { get; set; }

        public bool IsHouseLordMahadashaPlanet { get; set; }

        public bool IsHouseLordAntardashaPlanet { get; set; }

        public bool IsRetrograde { get; set; }

        public string InterpretationHint { get; set; } = string.Empty;
    }
}
