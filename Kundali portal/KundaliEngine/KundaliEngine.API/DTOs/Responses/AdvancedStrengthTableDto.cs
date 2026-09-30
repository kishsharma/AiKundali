using System.Text.Json.Serialization;

namespace KundaliEngine.API.DTOs.Responses
{
    public class AdvancedStrengthTableDto
    {
        public string MethodNote { get; set; } = string.Empty;
        public List<PlanetaryStrengthRowDto> PlanetaryStrengths { get; set; } = new();
        [JsonPropertyName("shadbala")]
        public List<ShadbalaRowDto> Shadbala { get; set; } = new();
        public List<HouseSupportRowDto> HouseSupport { get; set; } = new();
    }

    public class ShadbalaRowDto
    {
        public string Planet { get; set; } = string.Empty;
        public string Sign { get; set; } = string.Empty;
        public int House { get; set; }
        public double SthanaBala { get; set; }
        public double DigBala { get; set; }
        public double KalaBala { get; set; }
        public double CheshtaBala { get; set; }
        public double NaisargikaBala { get; set; }
        public double DrikBala { get; set; }
        public double Total { get; set; }
        public double Required { get; set; }
        public int Percent { get; set; }
        public string StrengthLevel { get; set; } = string.Empty;
        public string Interpretation { get; set; } = string.Empty;
    }

    public class PlanetaryStrengthRowDto
    {
        public string Planet { get; set; } = string.Empty;
        public string Sign { get; set; } = string.Empty;
        public int House { get; set; }
        public int TotalScore { get; set; }
        public string StrengthLevel { get; set; } = string.Empty;
        public string SthanaBala { get; set; } = string.Empty;
        public string DigBala { get; set; } = string.Empty;
        public string KalaBala { get; set; } = string.Empty;
        public string CheshtaBala { get; set; } = string.Empty;
        public string NaisargikaBala { get; set; } = string.Empty;
        public string DrikBala { get; set; } = string.Empty;
        public string Interpretation { get; set; } = string.Empty;
    }

    public class HouseSupportRowDto
    {
        public int House { get; set; }
        public string Sign { get; set; } = string.Empty;
        public string HouseName { get; set; } = string.Empty;
        public int Score { get; set; }
        public int EstimatedBavPoints { get; set; }
        public int EstimatedSavPoints { get; set; }
        public string SupportLevel { get; set; } = string.Empty;
        public string LifeAreas { get; set; } = string.Empty;
        public string TransitUsefulness { get; set; } = string.Empty;
    }
}
