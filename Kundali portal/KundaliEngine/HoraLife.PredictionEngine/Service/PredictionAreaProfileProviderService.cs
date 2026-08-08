using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Metadata;
using HoraLife.PredictionEngine.Models;

namespace HoraLife.PredictionEngine.Services;

public sealed class PredictionAreaProfileProviderService
{
    private readonly PredictionMetadataProvider _metadataProvider;

    internal PredictionAreaProfileProviderService(
        PredictionMetadataProvider metadataProvider)
    {
        _metadataProvider = metadataProvider
            ?? throw new ArgumentNullException(nameof(metadataProvider));
    }

    public PredictionAreaProfile Get(PredictionLifeArea lifeArea)
    {
        var metadata =_metadataProvider.GetLifeArea(lifeArea);

        return new PredictionAreaProfile
        {
            LifeArea = metadata.LifeArea,

            PrimaryHouses =metadata.PrimaryHouses.ToArray(),

            SupportingHouses =metadata.SupportingHouses.ToArray(),

            NaturalSignificators =metadata.NaturalSignificators.ToArray()
        };
    }
}