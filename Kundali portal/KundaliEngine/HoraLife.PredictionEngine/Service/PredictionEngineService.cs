using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Metadata;
using HoraLife.PredictionEngine.Models;
using HoraLife.PredictionEngine.Services.Career;
using HoraLife.PredictionEngine.Services.Education;
using HoraLife.PredictionEngine.Services.Family;
using HoraLife.PredictionEngine.Services.Finance;
using HoraLife.PredictionEngine.Services.Health;
using HoraLife.PredictionEngine.Services.Property;
using HoraLife.PredictionEngine.Services.Relationships;
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
    private readonly FinanceClaimGeneratorService _financeGenerator = new();
    private readonly FinanceDashaClaimGeneratorService _financeDashaGenerator = new();
    private readonly RelationshipsClaimGeneratorService _relationshipsGenerator = new();
    private readonly RelationshipsDashaClaimGeneratorService _relationshipsDashaGenerator = new();
    private readonly EducationClaimGeneratorService _educationGenerator = new();
    private readonly EducationDashaClaimGeneratorService _educationDashaGenerator = new();
    private readonly FamilyClaimGeneratorService _familyGenerator = new();
    private readonly FamilyDashaClaimGeneratorService _familyDashaGenerator = new();
    private readonly PropertyClaimGeneratorService _propertyGenerator = new();
    private readonly PropertyDashaClaimGeneratorService _propertyDashaGenerator = new();
    private readonly HealthClaimGeneratorService _healthGenerator = new();
    private readonly HealthDashaClaimGeneratorService _healthDashaGenerator = new();

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

    private IReadOnlyList<PredictionClaim> GenerateClaims( PredictionRequest request,  PredictionEvidenceSet evidence)
    {
        return request.LifeArea switch
        {
            PredictionLifeArea.Career => GenerateCareerClaims(request, evidence),

            PredictionLifeArea.Finance => GenerateFinanceClaims(request, evidence),

            PredictionLifeArea.Relationships => GenerateRelationshipsClaims(request, evidence),

            PredictionLifeArea.Education => GenerateEducationClaims(request, evidence),

            PredictionLifeArea.Family => GenerateFamilyClaims(request, evidence),

            PredictionLifeArea.Property => GeneratePropertyClaims(request, evidence),

            PredictionLifeArea.Health => GenerateHealthClaims(request, evidence),

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

    private IReadOnlyList<PredictionClaim> GenerateFinanceClaims( PredictionRequest request, PredictionEvidenceSet evidence)
    {
        var natalClaims = _financeGenerator.Generate(
            request,
            evidence);

        var dashaClaims = _financeDashaGenerator.Generate(
            request,
            evidence);

        return natalClaims
            .Concat(dashaClaims)
            .ToArray();
    }

    private IReadOnlyList<PredictionClaim> GenerateRelationshipsClaims( PredictionRequest request, PredictionEvidenceSet evidence)
    {
        var natalClaims = _relationshipsGenerator.Generate(
            request,
            evidence);

        var dashaClaims = _relationshipsDashaGenerator.Generate(
            request,
            evidence);

        return natalClaims
            .Concat(dashaClaims)
            .ToArray();
    }

    private IReadOnlyList<PredictionClaim> GenerateEducationClaims( PredictionRequest request, PredictionEvidenceSet evidence)
    {
        var natalClaims = _educationGenerator.Generate(
            request,
            evidence);

        var dashaClaims = _educationDashaGenerator.Generate(
            request,
            evidence);

        return natalClaims
            .Concat(dashaClaims)
            .ToArray();
    }

    private IReadOnlyList<PredictionClaim> GenerateFamilyClaims( PredictionRequest request, PredictionEvidenceSet evidence)
    {
        var natalClaims = _familyGenerator.Generate(
            request,
            evidence);

        var dashaClaims = _familyDashaGenerator.Generate(
            request,
            evidence);

        return natalClaims
            .Concat(dashaClaims)
            .ToArray();
    }

    private IReadOnlyList<PredictionClaim> GeneratePropertyClaims( PredictionRequest request, PredictionEvidenceSet evidence)
    {
        var natalClaims = _propertyGenerator.Generate(
            request,
            evidence);

        var dashaClaims = _propertyDashaGenerator.Generate(
            request,
            evidence);

        return natalClaims
            .Concat(dashaClaims)
            .ToArray();
    }

    private IReadOnlyList<PredictionClaim> GenerateHealthClaims( PredictionRequest request, PredictionEvidenceSet evidence)
    {
        var natalClaims = _healthGenerator.Generate(
            request,
            evidence);

        var dashaClaims = _healthDashaGenerator.Generate(
            request,
            evidence);

        return natalClaims
            .Concat(dashaClaims)
            .ToArray();
    }
}
