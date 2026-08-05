
namespace HoraLife.PredictionEngine.Models.Common
{
    public class DashaPredictionData
    {
        public DashaPeriodPredictionData Mahadasha { get; set; } = new();
        public DashaPeriodPredictionData Antardasha { get; set; } = new();
        public DashaPeriodPredictionData Pratyantardasha { get; set; } = new();
    }

}
