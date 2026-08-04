using HoraLife.PredictionEngine.Models.Common;
namespace HoraLife.PredictionEngine.Models.Career
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

    
    
  
  



   
}
