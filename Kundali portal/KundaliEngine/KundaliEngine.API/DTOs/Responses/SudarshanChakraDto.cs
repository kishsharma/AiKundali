namespace KundaliEngine.API.DTOs.Responses
{
    public class SudarshanChakraDto
    {
        public string Name { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string Usage { get; set; } = string.Empty;
        public string CalculationMethod { get; set; } = string.Empty;
        public int OverallScore { get; set; }
        public string OverallPrediction { get; set; } = string.Empty;
        public List<SudarshanHouseEmphasisDto> HouseEmphasis { get; set; } = new();
        public List<SudarshanChakraInterpretationDto> Interpretations { get; set; } = new();
    }

    public class SudarshanHouseEmphasisDto
    {
        public int HouseNumber { get; set; }
        public string Theme { get; set; } = string.Empty;
        public int LagnaPlanetCount { get; set; }
        public int MoonPlanetCount { get; set; }
        public int SunPlanetCount { get; set; }
        public int TotalPlanetCount { get; set; }
        public bool IsRepeatedAcrossReferences { get; set; }
        public string Prediction { get; set; } = string.Empty;
    }

    public class SudarshanChakraInterpretationDto
    {
        public string Area { get; set; } = string.Empty;
        public int Score { get; set; }
        public string Tone { get; set; } = string.Empty;
        public List<string> Factors { get; set; } = new();
        public string Prediction { get; set; } = string.Empty;
    }
}
