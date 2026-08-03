namespace HoraLife.PredictionEngine.Models
{
    public class CareerPredictionInput
    {
        public HousePredictionData TenthHouse { get; set; } = new();
        public PlanetPredictionData TenthLord { get; set; } = new();
        public List<PlanetPredictionData> RelevantPlanets { get; set; } = [];
        public DashaPredictionData CurrentDasha { get; set; } = new();
        public List<FormationPredictionData> RelevantYogas { get; set; } = [];
        public List<FormationPredictionData> RelevantDoshas { get; set; } = [];
    }

    public class HousePredictionData
    {
        public int HouseNumber { get; set; }
        public string HouseName { get; set; } = string.Empty;
        public string Sign { get; set; } = string.Empty;
        public string HouseOwner { get; set; } = string.Empty;
        public List<string> Occupants { get; set; } = [];
        public bool HasBeneficAspect { get; set; }
        public bool HasMaleficAspect { get; set; }
        public List<string> AspectedBy { get; set; } = [];
        public int Score { get; set; }
        public List<string> Factors { get; set; } = [];
        public List<string> DominantThemes { get; set; } = [];
        public string SupportLevel { get; set; } = string.Empty;
        public int? EstimatedBavPoints { get; set; }
        public int? EstimatedSavPoints { get; set; }
    }

    public class PlanetPredictionData
    {
        public string Planet { get; set; } = string.Empty;
        public string Sign { get; set; } = string.Empty;
        public int House { get; set; }
        public int Degree { get; set; }
        public int Minute { get; set; }
        public double Longitude { get; set; }
        public string Nakshatra { get; set; } = string.Empty;
        public int Pada { get; set; }
        public bool IsCombust { get; set; }
        public bool IsRetrograde { get; set; }
        public string DetailedStrength { get; set; } = string.Empty;
        public string ConditionTone { get; set; } = string.Empty;
        public bool HasBeneficAspect { get; set; }
        public bool HasMaleficAspect { get; set; }
        public List<string> AspectedBy { get; set; } = [];
        public int? StrengthScore { get; set; }
        public string StrengthLevel { get; set; } = string.Empty;
        public string SthanaBala { get; set; } = string.Empty;
        public string DigBala { get; set; } = string.Empty;
        public string KalaBala { get; set; } = string.Empty;
        public string CheshtaBala { get; set; } = string.Empty;
        public string NaisargikaBala { get; set; } = string.Empty;
        public string DrikBala { get; set; } = string.Empty;
        public string FunctionalNature { get; set; } = string.Empty;
    }

    public class DashaPredictionData
    {
        public DashaPeriodPredictionData Mahadasha { get; set; } = new();
        public DashaPeriodPredictionData Antardasha { get; set; } = new();
        public DashaPeriodPredictionData Pratyantardasha { get; set; } = new();
    }

    public class DashaPeriodPredictionData
    {
        public string Planet { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class FormationPredictionData
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Nature { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public bool IsPresent { get; set; }
        public string Strength { get; set; } = string.Empty;
        public List<string> InvolvedPlanets { get; set; } = [];
        public List<int> InvolvedHouses { get; set; } = [];
        public string Reason { get; set; } = string.Empty;
        public List<string> CareerImpactStatements { get; set; } = [];
        public List<FormationActivationPredictionData> CurrentlyActive { get; set; } = [];
        public List<FormationActivationPredictionData> Upcoming { get; set; } = [];
        public List<FormationActivationPredictionData> Past { get; set; } = [];
    }

    public class FormationActivationPredictionData
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string MahadashaPlanet { get; set; } = string.Empty;
        public string AntardashaPlanet { get; set; } = string.Empty;
        public string ActivationLevel { get; set; } = string.Empty;
        public bool IsCurrentlyActive { get; set; }
        public List<string> TriggeredBy { get; set; } = [];
    }
}
