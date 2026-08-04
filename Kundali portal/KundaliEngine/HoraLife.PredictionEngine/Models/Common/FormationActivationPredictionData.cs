using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoraLife.PredictionEngine.Models.Common
{
    public class FormationActivationPredictionData
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string MahadashaPlanet { get; set; } = string.Empty;
        public string AntardashaPlanet { get; set; } = string.Empty;
        public string ActivationLevel { get; set; } = string.Empty;
        public bool IsCurrentlyActive { get; set; }
        public List<string> TriggeredBy { get; set; } = new();
    }
}
