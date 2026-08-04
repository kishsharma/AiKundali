using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HoraLife.PredictionEngine.Models.Career
{
    public sealed class CareerPredictionResult
    {
        public List<CareerPredictionFactor> SupportiveFactors { get; init; } = [];
        public List<CareerPredictionFactor> ChallengingFactors { get; init; } = [];
        public List<CareerPredictionFactor> CurrentPeriodInfluences { get; init; } = [];
    }
}
