using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoraLife.PredictionEngine.Models.Common
{
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

}
