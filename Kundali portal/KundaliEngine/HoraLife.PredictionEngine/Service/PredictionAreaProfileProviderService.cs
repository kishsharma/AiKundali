using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Models;

namespace HoraLife.PredictionEngine.Services;

public sealed class PredictionAreaProfileProviderService
{
    private static readonly PredictionAreaProfile CareerProfile = new()
    {
        LifeArea = PredictionLifeArea.Career,
        PrimaryHouses = Array.AsReadOnly([10]),
        SupportingHouses = Array.AsReadOnly([2, 6, 11]),
        NaturalSignificators =
            Array.AsReadOnly(["Sun", "Saturn", "Mercury"])
    };

    public PredictionAreaProfile Get(PredictionLifeArea lifeArea)
    {
        return lifeArea switch
        {
            PredictionLifeArea.Career => CareerProfile,

            _ => throw new NotSupportedException(
                $"Prediction profile for '{lifeArea}' is not implemented.")
        };
    }
}