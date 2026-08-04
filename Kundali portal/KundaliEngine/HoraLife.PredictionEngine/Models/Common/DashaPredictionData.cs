using HoraLife.PredictionEngine.Models.Career;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoraLife.PredictionEngine.Models.Common
{
    public class DashaPredictionData
    {
        public DashaPeriodPredictionData Mahadasha { get; set; } = new();
        public DashaPeriodPredictionData Antardasha { get; set; } = new();
        public DashaPeriodPredictionData Pratyantardasha { get; set; } = new();
    }

}
