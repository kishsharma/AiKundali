using HoraLife.PredictionEngine.Interpretation;
using HoraLife.PredictionEngine.Metadata;
using HoraLife.PredictionEngine.Services;
using HoraLife.PredictionEngine.Synthesis;

namespace HoraLife.PredictionEngine;

public static class PredictionEngineFactory
{
    public static PredictionEngineService CreateDefault()
    {
        var metadataPath = Path.Combine(
            AppContext.BaseDirectory,
            "Metadata",
            "Data",
            "life-areas.json");

        var metadataProvider = new PredictionMetadataProvider(metadataPath);
        var profileProvider = new PredictionAreaProfileProviderService(metadataProvider);
        var houseLordInterpreter = new HouseLordPlacementInterpreterService(metadataProvider);
        var synthesisService = new PredictionSynthesisService(houseLordInterpreter);

        return new PredictionEngineService(profileProvider, synthesisService);
    }
}
