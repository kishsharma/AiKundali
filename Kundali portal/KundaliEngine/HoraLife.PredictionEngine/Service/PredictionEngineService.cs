using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Metadata;
using HoraLife.PredictionEngine.Models;
using HoraLife.PredictionEngine.Services.Career;
using HoraLife.PredictionEngine.Synthesis;

namespace HoraLife.PredictionEngine.Services;

/// <summary>
/// Coordinates prediction profile selection, Dasha-period selection,
/// evidence collection and life-area claim generation.
/// </summary>
public sealed class PredictionEngineService
{
    private readonly PredictionAreaProfileProviderService _profileService;
    private readonly DashaPeriodSelectorService _dashaSelector = new();
    private readonly PredictionEvidenceSelectorService _evidenceSelector = new();
    private readonly CareerClaimGeneratorService _careerGenerator = new();
    private readonly CareerDashaClaimGeneratorService   _careerDashaGenerator = new();
    private readonly PredictionClaimConsolidatorService  _claimConsolidator = new();
    private readonly PredictionSynthesisService _synthesisService;
    internal PredictionEngineService(PredictionAreaProfileProviderService profileService, PredictionSynthesisService synthesisService)
    {
        _profileService = profileService;
        _synthesisService = synthesisService;
    }


    public PredictionResult Generate(PredictionRequest request, PredictionContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var profile = _profileService.Get(request.LifeArea);

        var dashaPeriods = _dashaSelector.Select(context.DashaTimeline, request);

         var evidence = _evidenceSelector.Select(context, profile, dashaPeriods);

        var sourceClaims = GenerateClaims(        request,      evidence);
        var consolidatedClaim = _claimConsolidator.Consolidate(  request, sourceClaims);

        var finalClaims = consolidatedClaim is null
        ? sourceClaims
        :
        [
            .. sourceClaims,
            consolidatedClaim
        ];

        var synthesis = _synthesisService.Build(request, evidence,finalClaims);

        return new PredictionResult
        {
            LifeArea = request.LifeArea,
            FocusKey = request.FocusKey,
            TimeScope = request.TimeScope,
            ReferenceDate = request.ReferenceDate,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            Claims = finalClaims,
            Synthesis = synthesis
        };
    }

    private IReadOnlyList<PredictionClaim> GenerateClaims( PredictionRequest request, PredictionEvidenceSet evidence)
    {
        return request.LifeArea switch
        {
            PredictionLifeArea.Career =>
                GenerateCareerClaims(request, evidence),

            _ => throw new NotSupportedException(
                $"Prediction area '{request.LifeArea}' is not implemented.")
        };
    }

    private IReadOnlyList<PredictionClaim> GenerateCareerClaims( PredictionRequest request, PredictionEvidenceSet evidence)
    {
        var natalClaims = _careerGenerator.Generate(
            request,
            evidence);

        var dashaClaims = _careerDashaGenerator.Generate(
            request,
            evidence);

        return natalClaims
            .Concat(dashaClaims)
            .ToArray();
    }
}