using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models
{
    public class SudarshanChakraResult
    {
        public string Name { get; set; } = "Sudarshan Chakra";
        public string Purpose { get; set; } = string.Empty;
        public string Usage { get; set; } = string.Empty;
        public string CalculationMethod { get; set; } = string.Empty;
        public int OverallScore { get; set; }
        public List<SudarshanChakraWheel> Wheels { get; set; } = new();
        public List<SudarshanHouseEmphasis> HouseEmphasis { get; set; } = new();
        public List<SudarshanChakraInterpretation> Interpretations { get; set; } = new();
        public string OverallPrediction { get; set; } = string.Empty;
    }

    public class SudarshanChakraWheel
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public ZodiacSign ReferenceSign { get; set; }
        public string Focus { get; set; } = string.Empty;
        public List<SudarshanChakraHouse> Houses { get; set; } = new();
    }

    public class SudarshanChakraHouse
    {
        public int HouseNumber { get; set; }
        public ZodiacSign Sign { get; set; }
        public List<SudarshanPlanetPlacement> Planets { get; set; } = new();
        public List<string> AspectedBy { get; set; } = new();
        public string Theme { get; set; } = string.Empty;
    }

    public class SudarshanPlanetPlacement
    {
        public Planet Planet { get; set; }
        public ZodiacSign Sign { get; set; }
        public int House { get; set; }
        public double Longitude { get; set; }
        public double DegreeInSign { get; set; }
        public bool IsRetrograde { get; set; }
        public bool IsCombust { get; set; }
        public string DetailedStrength { get; set; } = string.Empty;
    }

    public class SudarshanHouseEmphasis
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

    public class SudarshanChakraInterpretation
    {
        public string Area { get; set; } = string.Empty;
        public int Score { get; set; }
        public string Tone { get; set; } = string.Empty;
        public List<string> Factors { get; set; } = new();
        public string Prediction { get; set; } = string.Empty;
    }
}

