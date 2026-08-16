using HoraLife.PredictionEngine.Enums;
using KundaliEngine.API.DTOs.Responses;

namespace KundaliEngine.API.Prediction.DTOs;

public sealed class PredictionQuestionRequestDto
{
    public PredictionLifeArea LifeArea { get; set; }

    public string FocusKey { get; set; } = string.Empty;

    public PredictionTimeScope TimeScope { get; set; }
        = PredictionTimeScope.Current;

    public DateTimeOffset? ReferenceDate { get; set; }

    public GenerateKundaliResponse? KundaliData { get; set; }
}
