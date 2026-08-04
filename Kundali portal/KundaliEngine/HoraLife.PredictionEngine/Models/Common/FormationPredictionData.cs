using HoraLife.PredictionEngine.Models.Career;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoraLife.PredictionEngine.Models.Common
{
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
}
