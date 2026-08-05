using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Models;
using HoraLife.PredictionEngine.Services.Career;

namespace HoraLife.PredictionEngine.Services;

/// <summary>
/// Coordinates prediction profile selection, Dasha-period selection,
/// evidence collection and life-area claim generation.
/// </summary>
public sealed class PredictionEngineService
{
    private readonly PredictionAreaProfileProviderService _profileService = new();
    private readonly DashaPeriodSelectorService _dashaSelector = new();
    private readonly PredictionEvidenceSelectorService _evidenceSelector = new();
    private readonly CareerClaimGeneratorService _careerGenerator = new();

    public PredictionResult Generate(PredictionRequest request, PredictionContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var profile = _profileService.Get(request.LifeArea);

        var dashaPeriods = _dashaSelector.Select(context.DashaTimeline, request);

         var evidence = _evidenceSelector.Select(context, profile, dashaPeriods);

        var claims = GenerateClaims(
            request,
            evidence);

        return new PredictionResult
        {
            LifeArea = request.LifeArea,
            FocusKey = request.FocusKey,
            TimeScope = request.TimeScope,
            ReferenceDate = request.ReferenceDate,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Claims = claims
        };
    }

    private IReadOnlyList<PredictionClaim> GenerateClaims(
        PredictionRequest request,
        PredictionEvidenceSet evidence)
    {
        return request.LifeArea switch
        {
            PredictionLifeArea.Career =>
                _careerGenerator.Generate(request, evidence),

            _ => throw new NotSupportedException(
                $"Prediction area '{request.LifeArea}' is not implemented.")
        };
    }
}