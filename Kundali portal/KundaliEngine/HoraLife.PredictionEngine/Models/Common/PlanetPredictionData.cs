using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoraLife.PredictionEngine.Models.Common
{
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

}
