using KundaliEngine.API.DTOs.Requests;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Dasha;
using KundaliEngine.Astrology.Services.Scoring;
using KundaliEngine.Domain.Entities;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Services.CurrentLifeFocus;

public interface ICurrentLifeFocusService
{
    Task<CurrentLifeFocusResponse> GenerateAsync(
        CurrentLifeFocusRequest request,
        Guid? userId,
        CancellationToken cancellationToken = default);
}

public interface IFocusedLifeAreaAnalysisService
{
    FocusedLifeAreaResponse Analyze(Guid analysisId, Guid? userId, FocusedLifeAreaRequest request);
}

public class CurrentLifeFocusService : ICurrentLifeFocusService
{
    private readonly KundaliDbContext _dbContext;
    private readonly ChartService _chartService;
    private readonly DashaService _dashaService;
    private readonly HouseScoreService _houseScoreService;
    private readonly AspectService _aspectService;
    private readonly IEphemerisProvider _ephemerisProvider;
    private readonly CurrentLifeFocusMetadataService _metadataService;
    private readonly IHouseActivationScorer _houseActivationScorer;
    private readonly ILifeAreaMapper _lifeAreaMapper;
    private readonly ICurrentLifeFocusAnalysisStore _analysisStore;
    private readonly ILogger<CurrentLifeFocusService> _logger;

    public CurrentLifeFocusService(
        KundaliDbContext dbContext,
        ChartService chartService,
        DashaService dashaService,
        HouseScoreService houseScoreService,
        AspectService aspectService,
        IEphemerisProvider ephemerisProvider,
        CurrentLifeFocusMetadataService metadataService,
        IHouseActivationScorer houseActivationScorer,
        ILifeAreaMapper lifeAreaMapper,
        ICurrentLifeFocusAnalysisStore analysisStore,
        ILogger<CurrentLifeFocusService> logger)
    {
        _dbContext = dbContext;
        _chartService = chartService;
        _dashaService = dashaService;
        _houseScoreService = houseScoreService;
        _aspectService = aspectService;
        _ephemerisProvider = ephemerisProvider;
        _metadataService = metadataService;
        _houseActivationScorer = houseActivationScorer;
        _lifeAreaMapper = lifeAreaMapper;
        _analysisStore = analysisStore;
        _logger = logger;
    }

    public async Task<CurrentLifeFocusResponse> GenerateAsync(
        CurrentLifeFocusRequest request,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        var kundali = await LoadKundaliAsync(request.KundaliId, userId, cancellationToken);
        var configuration = _metadataService.GetConfiguration();
        var birthUtc = ResolveBirthUtc(kundali);
        var analysisUtc = request.AnalysisDate.UtcDateTime;
        var latitude = Convert.ToDouble(kundali.Latitude ?? throw new ArgumentException("Saved Kundali is missing latitude."));
        var longitude = Convert.ToDouble(kundali.Longitude ?? throw new ArgumentException("Saved Kundali is missing longitude."));

        var natalPlanets = _chartService.GenerateChart(birthUtc, latitude, longitude);
        var natalMoon = natalPlanets.FirstOrDefault(planet => planet.Planet == Planet.Moon)
            ?? throw new InvalidOperationException("Missing Dasha data because natal Moon could not be calculated.");
        var natalAscendantSign = CalculateAscendantSign(birthUtc, latitude, longitude);
        var natalMoonSign = natalMoon.Sign;
        var birthDasha = _dashaService.GetBirthMahadasha(natalMoon);
        var dasha = _dashaService.CalculateCurrentDasha(
            birthDasha.Planet,
            birthDasha.RemainingYears,
            birthUtc,
            analysisUtc);

        var natalAspects = _aspectService.CalculateGrahaDrishti(natalPlanets);
        var natalHouseScores = _houseScoreService.CalculateAllHouseScores(natalAscendantSign, natalPlanets, natalAspects);
        var transitRaw = _chartService.GenerateChart(analysisUtc, latitude, longitude);
        var transitFromLagna = RehouseFromReference(transitRaw, natalAscendantSign);
        var transitFromMoon = RehouseFromReference(transitRaw, natalMoonSign);
        var transitAspectsFromLagna = _aspectService.CalculateGrahaDrishti(transitFromLagna);
        var transitAspectsFromMoon = _aspectService.CalculateGrahaDrishti(transitFromMoon);

        var houseScores = _houseActivationScorer.Score(new HouseActivationContext
        {
            NatalPlanets = natalPlanets,
            TransitPlanetsFromLagna = transitFromLagna,
            TransitPlanetsFromMoon = transitFromMoon,
            TransitAspectsFromLagna = transitAspectsFromLagna,
            TransitAspectsFromMoon = transitAspectsFromMoon,
            NatalHouseScores = natalHouseScores,
            Mahadasha = dasha.CurrentMahadasha.Planet,
            Antardasha = dasha.CurrentAntardasha.SubPlanet,
            NatalAscendantSign = natalAscendantSign,
            NatalMoonSign = natalMoonSign,
            Scoring = configuration.Scoring
        }).ToList();

        var rankedAreas = _lifeAreaMapper.Map(houseScores, configuration).ToList();
        var areas = BuildDisplayedAreas(rankedAreas);
        var snapshot = new CurrentLifeFocusSnapshot
        {
            AnalysisId = Guid.NewGuid(),
            KundaliId = kundali.Id,
            UserId = userId,
            AnalysisDate = request.AnalysisDate,
            Mahadasha = dasha.CurrentMahadasha.Planet,
            Antardasha = dasha.CurrentAntardasha.SubPlanet,
            HouseScores = houseScores,
            Areas = areas
        };
        _analysisStore.Save(snapshot, TimeSpan.FromMinutes(configuration.Scoring.AnalysisExpiryMinutes));

        _logger.LogInformation(
            "CurrentLifeFocusGenerated analysisId={AnalysisId} kundaliId={KundaliId} userId={UserId} analysisDate={AnalysisDate}",
            snapshot.AnalysisId,
            kundali.Id,
            userId,
            request.AnalysisDate);

        return new CurrentLifeFocusResponse
        {
            AnalysisId = snapshot.AnalysisId,
            AnalysisDate = request.AnalysisDate,
            ActiveDasha = new CurrentLifeFocusDashaDto
            {
                Mahadasha = snapshot.Mahadasha.ToString(),
                Antardasha = snapshot.Antardasha.ToString()
            },
            Areas = areas.Select(MapArea).ToList(),
            Question = "Which area best matches what you are currently experiencing?"
        };
    }


    private static List<LifeAreaActivation> BuildDisplayedAreas(IReadOnlyList<LifeAreaActivation> rankedAreas)
    {
        var mustShowKeys = new[]
        {
            "business_suitability",
            "partnership_contracts",
            "separation_relationship_pressure"
        };

        var selected = rankedAreas.Take(3).ToList();

        foreach (var key in mustShowKeys)
        {
            var area = rankedAreas.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase));
            if (area == null || selected.Any(item => string.Equals(item.Key, area.Key, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            selected.Add(area);
        }

        return selected
            .OrderByDescending(area => area.ActivationScore)
            .ThenByDescending(area => area.Confidence)
            .ThenBy(area => Array.IndexOf(mustShowKeys, area.Key) < 0 ? 99 : Array.IndexOf(mustShowKeys, area.Key))
            .ToList();
    }
    private async Task<Kundali> LoadKundaliAsync(Guid kundaliId, Guid? userId, CancellationToken cancellationToken)
    {
        var kundali = await _dbContext.Kundalis
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == kundaliId && item.UserId == userId, cancellationToken);

        if (kundali == null)
        {
            throw new KeyNotFoundException("Saved Kundali was not found.");
        }

        if (kundali.Latitude == null || kundali.Longitude == null)
        {
            throw new ArgumentException("Saved Kundali is missing birth location coordinates.");
        }

        return kundali;
    }

    private static void ValidateRequest(CurrentLifeFocusRequest request)
    {
        if (request == null)
        {
            throw new ArgumentException("Request is required.");
        }

        if (request.KundaliId == Guid.Empty)
        {
            throw new ArgumentException("kundaliId is required.");
        }

        if (request.AnalysisDate == default)
        {
            throw new ArgumentException("analysisDate is required.");
        }
    }

    private static CurrentLifeFocusAreaDto MapArea(LifeAreaActivation area)
    {
        return new CurrentLifeFocusAreaDto
        {
            Key = area.Key,
            Title = area.Title,
            ActivationScore = area.ActivationScore,
            ActivationLevel = area.ActivationLevel,
            Tone = area.Tone,
            Confidence = area.Confidence,
            ActivatedHouses = area.ActivatedHouses,
            ShortReasons = area.ShortReasons
        };
    }

    private ZodiacSign CalculateAscendantSign(DateTime birthUtc, double latitude, double longitude)
    {
        var ascendantLongitude = _ephemerisProvider.GetAscendant(birthUtc, new GeoLocation(latitude, longitude)).Longitude;
        var ascendantIndex = (int)(NormalizeLongitude(ascendantLongitude) / 30.0);
        return (ZodiacSign)((ascendantIndex % 12) + 1);
    }

    private static List<PlanetPosition> RehouseFromReference(IEnumerable<PlanetPosition> source, ZodiacSign referenceSign)
    {
        return source.Select(position => CloneWithHouse(position, CalculateHouseFromReference(position.Sign, referenceSign))).ToList();
    }

    private static PlanetPosition CloneWithHouse(PlanetPosition source, HouseNumber house)
    {
        return new PlanetPosition
        {
            Planet = source.Planet,
            PlanetName = source.PlanetName,
            Longitude = source.Longitude,
            Latitude = source.Latitude,
            Sign = source.Sign,
            DegreeInSign = source.DegreeInSign,
            Degree = source.Degree,
            Minute = source.Minute,
            Nakshatra = source.Nakshatra,
            Pada = source.Pada,
            House = house,
            IsRetrograde = source.IsRetrograde,
            DetailedStrength = source.DetailedStrength,
            IsCombust = source.IsCombust
        };
    }

    private static HouseNumber CalculateHouseFromReference(ZodiacSign sign, ZodiacSign referenceSign)
    {
        var house = (((int)sign - (int)referenceSign + 12) % 12) + 1;
        return (HouseNumber)house;
    }

    private static DateTime ResolveBirthUtc(Kundali kundali)
    {
        var birthLocal = kundali.DateOfBirth.ToDateTime(kundali.TimeOfBirth);
        var offset = ParseOffset(kundali.TimeZone) ?? TimeSpan.FromHours(5.5);
        return new DateTimeOffset(DateTime.SpecifyKind(birthLocal, DateTimeKind.Unspecified), offset).UtcDateTime;
    }

    private static TimeSpan? ParseOffset(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("UTC", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return TimeSpan.TryParse(value[3..], out var offset) ? offset : null;
    }

    private static double NormalizeLongitude(double longitude)
    {
        longitude %= 360.0;
        if (longitude < 0)
        {
            longitude += 360.0;
        }

        return longitude;
    }
}

public class FocusedLifeAreaAnalysisService : IFocusedLifeAreaAnalysisService
{
    private readonly CurrentLifeFocusMetadataService _metadataService;
    private readonly ICurrentLifeFocusAnalysisStore _analysisStore;
    private readonly ILogger<FocusedLifeAreaAnalysisService> _logger;
    private readonly IFocusedLifeAreaNarrativeBuilder _narrativeBuilder;

    public FocusedLifeAreaAnalysisService(
        CurrentLifeFocusMetadataService metadataService,
        ICurrentLifeFocusAnalysisStore analysisStore,
        ILogger<FocusedLifeAreaAnalysisService> logger,
        IFocusedLifeAreaNarrativeBuilder narrativeBuilder)
    {
        _metadataService = metadataService;
        _analysisStore = analysisStore;
        _logger = logger;
        _narrativeBuilder = narrativeBuilder;
    }

    public FocusedLifeAreaResponse Analyze(Guid analysisId, Guid? userId, FocusedLifeAreaRequest request)
    {
        if (analysisId == Guid.Empty)
        {
            throw new ArgumentException("analysisId is required.");
        }

        if (request == null)
        {
            throw new ArgumentException("Request is required.");
        }

        if (string.IsNullOrWhiteSpace(request.SelectedAreaKey))
        {
            _logger.LogInformation("CurrentLifeFocusNoneSelected analysisId={AnalysisId} userId={UserId}", analysisId, userId);
            throw new ArgumentException("selectedAreaKey is required.");
        }

        if (!_analysisStore.TryGet(analysisId, userId, out var snapshot))
        {
            throw new KeyNotFoundException("Current Life Focus analysis was not found or has expired.");
        }

        var areaConfig = _metadataService.GetLifeArea(request.SelectedAreaKey)
            ?? throw new ArgumentException("selectedAreaKey does not exist in Current Life Focus metadata.");

        if (!string.IsNullOrWhiteSpace(request.SpecificConcernKey)
            && !areaConfig.FollowUpOptions.Any(option => string.Equals(option.Key, request.SpecificConcernKey, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("specificConcernKey does not belong to the selected life area.");
        }

        var area = snapshot.Areas.FirstOrDefault(item => string.Equals(item.Key, areaConfig.Key, StringComparison.OrdinalIgnoreCase))
            ?? BuildUnrankedArea(areaConfig, snapshot);

        _analysisStore.RecordSelection(analysisId, areaConfig.Key, request.SpecificConcernKey);
        _logger.LogInformation(
            "CurrentLifeFocusAreaSelected analysisId={AnalysisId} userId={UserId} selectedAreaKey={SelectedAreaKey}",
            analysisId,
            userId,
            areaConfig.Key);

        if (!string.IsNullOrWhiteSpace(request.SpecificConcernKey))
        {
            _logger.LogInformation(
                "CurrentLifeFocusSpecificConcernSelected analysisId={AnalysisId} userId={UserId} selectedAreaKey={SelectedAreaKey} specificConcernKey={SpecificConcernKey}",
                analysisId,
                userId,
                areaConfig.Key,
                request.SpecificConcernKey);
        }

        var possibleExperiences = areaConfig.PossibleExperiences.Take(5).ToList();
        var supportiveFactors = BuildSupportiveFactors(areaConfig, area);
        var pressureFactors = BuildPressureFactors(areaConfig, area);
        var activatedHouses = BuildActivatedHouses(areaConfig, snapshot.HouseScores);
        var decisionGuidance = BuildDecisionGuidance(areaConfig, area, request.SpecificConcernKey, activatedHouses, supportiveFactors, pressureFactors);
        var calculationBasis = BuildCalculationBasis(snapshot, activatedHouses, area, request.SpecificConcernKey);

        return new FocusedLifeAreaResponse
        {
            AreaKey = areaConfig.Key,
            Title = areaConfig.DisplayName,
            ActivationLevel = area.ActivationLevel,
            Tone = area.Tone,
            Summary = BuildSummary(areaConfig, area),
            Narrative = _narrativeBuilder.Build(
                areaConfig,
                area,
                snapshot,
                activatedHouses,
                possibleExperiences,
                supportiveFactors,
                pressureFactors),
            DecisionGuidance = decisionGuidance,
            CalculationBasis = calculationBasis,
            PossibleExperiences = possibleExperiences,
            SupportiveFactors = supportiveFactors,
            PressureFactors = pressureFactors,
            ActivatedHouses = activatedHouses,
            FollowUpQuestion = areaConfig.FollowUpQuestion,
            FollowUpOptions = areaConfig.FollowUpOptions.Select(option => new CurrentLifeFocusOptionDto
            {
                Key = option.Key,
                Label = option.Label
            }).ToList()
        };
    }

    private static LifeAreaActivation BuildUnrankedArea(LifeAreaConfiguration areaConfig, CurrentLifeFocusSnapshot snapshot)
    {
        var activeHouses = areaConfig.Houses
            .OrderByDescending(house => snapshot.HouseScores.FirstOrDefault(score => score.House == house.House)?.TotalScore ?? 0)
            .Take(3)
            .Select(house => house.House)
            .ToList();

        return new LifeAreaActivation
        {
            Key = areaConfig.Key,
            Title = areaConfig.DisplayName,
            ActivationScore = activeHouses.Count == 0 ? 0 : (int)Math.Round(activeHouses.Average(h => snapshot.HouseScores.First(s => s.House == h).TotalScore)),
            ActivationLevel = "Low",
            Tone = "Mixed",
            ActivatedHouses = activeHouses
        };
    }

    private static FocusedDecisionGuidanceDto BuildDecisionGuidance(
        LifeAreaConfiguration areaConfig,
        LifeAreaActivation area,
        string? specificConcernKey,
        IReadOnlyList<FocusedActivatedHouseDto> activatedHouses,
        IReadOnlyList<string> supportiveFactors,
        IReadOnlyList<string> pressureFactors)
    {
        var concern = NormalizeConcern(specificConcernKey, areaConfig.Key);
        var score = area.ActivationScore;
        var pressureDominant = string.Equals(area.Tone, "Pressured", StringComparison.OrdinalIgnoreCase);
        var supportiveDominant = string.Equals(area.Tone, "Supportive", StringComparison.OrdinalIgnoreCase);
        var verdict = BuildVerdictLabel(score, pressureDominant, supportiveDominant, concern);
        var houses = activatedHouses.Select(house => house.House).ToHashSet();

        return new FocusedDecisionGuidanceDto
        {
            QuestionType = BuildQuestionType(concern, areaConfig.DisplayName),
            VerdictLabel = verdict,
            Recommendation = BuildRecommendation(concern, verdict, score, pressureDominant, supportiveDominant),
            Why = BuildDecisionWhy(areaConfig, area, activatedHouses),
            BestApproach = BuildBestApproach(concern, pressureDominant, supportiveDominant, houses),
            SuitableFor = BuildSuitableFor(concern, houses, supportiveDominant),
            Cautions = BuildDecisionCautions(concern, pressureDominant, pressureFactors)
        };
    }

    private static List<string> BuildCalculationBasis(
        CurrentLifeFocusSnapshot snapshot,
        IReadOnlyList<FocusedActivatedHouseDto> activatedHouses,
        LifeAreaActivation area,
        string? specificConcernKey)
    {
        var basis = new List<string>
        {
            $"Active Dasha: {snapshot.Mahadasha} Mahadasha / {snapshot.Antardasha} Antardasha.",
            "Natal houses are judged through house strength, dasha lord placement/ownership, and current activation.",
            "Gochar pressure/support uses Saturn, Jupiter, Rahu, Ketu and Mars from both Lagna and Moon, including aspects.",
            $"Support score {area.SupportScore}, pressure score {area.PressureScore}, confidence {area.Confidence}."
        };

        if (!string.IsNullOrWhiteSpace(specificConcernKey))
        {
            basis.Add($"Specific concern selected: {specificConcernKey.Replace('_', ' ')}.");
        }

        basis.AddRange(activatedHouses.Select(house => $"House {house.House}: {house.Role}, activation score {house.ActivationScore}."));
        return basis.Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToList();
    }

    private static string NormalizeConcern(string? specificConcernKey, string areaKey)
    {
        var value = string.IsNullOrWhiteSpace(specificConcernKey) ? areaKey : specificConcernKey;
        return value.Trim().ToLowerInvariant();
    }

    private static string BuildQuestionType(string concern, string areaName)
    {
        if (IsBusinessConcern(concern)) return "Business suitability";
        if (IsPartnershipConcern(concern)) return "Partnership suitability";
        if (IsSeparationConcern(concern)) return "Separation or relationship distance";
        return areaName;
    }

    private static string BuildVerdictLabel(int score, bool pressureDominant, bool supportiveDominant, string concern)
    {
        if (IsSeparationConcern(concern))
        {
            if (pressureDominant && score >= 55) return "High pressure, avoid rushed separation decisions";
            if (supportiveDominant) return "Repair is more supported than rupture";
            return "Mixed, needs counselling and timing review";
        }

        if (pressureDominant && score >= 60) return "Possible, but with caution";
        if (supportiveDominant && score >= 55) return "Suitable with steady effort";
        if (score >= 70) return "Strongly activated, proceed carefully";
        if (score >= 40) return "Moderately suitable after preparation";
        return "Not strongly supported right now";
    }

    private static string BuildRecommendation(string concern, string verdict, int score, bool pressureDominant, bool supportiveDominant)
    {
        if (IsBusinessConcern(concern))
        {
            return pressureDominant
                ? "Business can be explored only with risk control, written planning, debt discipline and slower commitments. Avoid starting purely from stress or urgency."
                : supportiveDominant
                    ? "Business is worth exploring, especially if skills, clients, communication and recurring gains are already visible. Start structured, not impulsively."
                    : "Business is possible, but the chart asks for validation first: test demand, protect savings and avoid over-expansion.";
        }

        if (IsPartnershipConcern(concern))
        {
            return pressureDominant
                ? "Partnership needs strict agreements, role clarity and exit clauses. Avoid informal trust-based arrangements."
                : supportiveDominant
                    ? "Partnership can work better when values, money handling, duties and decision rights are clearly documented."
                    : "Partnership is workable only after checking compatibility of money, effort and authority expectations.";
        }

        if (IsSeparationConcern(concern))
        {
            return pressureDominant
                ? "The period can intensify distance or conflict, but final separation decisions should be delayed until emotions, family pressure and legal facts are reviewed calmly."
                : "This period supports repair more than rupture if both sides can communicate practically and reduce family or financial pressure.";
        }

        return pressureDominant
            ? "Handle this area with patience, structure and practical safeguards before making irreversible decisions."
            : $"{verdict}. Use the supportive factors through steady action and avoid forcing quick outcomes.";
    }

    private static string BuildDecisionWhy(
        LifeAreaConfiguration areaConfig,
        LifeAreaActivation area,
        IReadOnlyList<FocusedActivatedHouseDto> activatedHouses)
    {
        var houses = activatedHouses.Count == 0
            ? "the relevant houses"
            : string.Join(", ", activatedHouses.Take(3).Select(house => $"house {house.House} ({house.Role.ToLowerInvariant()})"));
        return $"This judgment combines {areaConfig.DisplayName.ToLowerInvariant()} houses, current Dasha activation, Gochar pressure/support and natal house sensitivity. Currently {houses} carry the main evidence, with activation {area.ActivationScore}/100 and a {area.Tone.ToLowerInvariant()} tone.";
    }

    private static string BuildBestApproach(string concern, bool pressureDominant, bool supportiveDominant, HashSet<int> houses)
    {
        if (IsBusinessConcern(concern))
        {
            if (houses.Contains(3) || houses.Contains(10)) return "Prefer skill-led, service-led, advisory, communication, sales, technology, operations or self-effort based business models before heavy asset models.";
            if (houses.Contains(11)) return "Prefer network, distribution, community, recurring income or client-referral models.";
            if (houses.Contains(8) || houses.Contains(12)) return "Avoid high leverage. Prefer research, compliance, healing, foreign/remote or back-end work with clean contracts.";
            return "Begin with a small proof of demand, clean accounts and measurable monthly review.";
        }

        if (IsPartnershipConcern(concern))
        {
            return pressureDominant
                ? "Use written agreements, separate responsibilities, transparent accounts and a defined conflict-resolution process."
                : "Choose partners who add complementary skills, network or capital without blurring authority and money boundaries.";
        }

        if (IsSeparationConcern(concern))
        {
            return "Separate temporary distance, counselling, family mediation, finances and legal decision-making into different steps; do not treat one fight as the final chart verdict.";
        }

        return supportiveDominant
            ? "Act steadily while the supportive current is available."
            : "Reduce pressure first, then decide.";
    }

    private static List<string> BuildSuitableFor(string concern, HashSet<int> houses, bool supportiveDominant)
    {
        if (IsBusinessConcern(concern))
        {
            var items = new List<string>();
            if (houses.Contains(3)) items.Add("communication, media, writing, marketing, sales, short-travel or skill-based work");
            if (houses.Contains(6)) items.Add("service, problem-solving, operations, health routines, compliance or competitive fields");
            if (houses.Contains(7)) items.Add("client-facing trade, consulting, contracts and marketplace work");
            if (houses.Contains(10)) items.Add("professional services, management, authority-based work and public-facing roles");
            if (houses.Contains(11)) items.Add("network-led gains, communities, distribution and recurring-income models");
            if (houses.Contains(12)) items.Add("foreign, remote, digital, retreat, hospital, import/export or behind-the-scenes work");
            return items.Count == 0 ? new List<string> { supportiveDominant ? "structured business after market validation" : "small test projects before full business commitment" } : items.Take(5).ToList();
        }

        if (IsPartnershipConcern(concern))
        {
            return new List<string> { "documented roles", "transparent money handling", "client or contract based cooperation", "partners with complementary skills" };
        }

        if (IsSeparationConcern(concern))
        {
            return new List<string> { "counselling", "temporary cooling-off period", "family boundary setting", "financial clarity before final decisions" };
        }

        return new List<string>();
    }

    private static List<string> BuildDecisionCautions(string concern, bool pressureDominant, IReadOnlyList<string> pressureFactors)
    {
        var cautions = pressureFactors.Take(3).ToList();
        if (IsBusinessConcern(concern))
        {
            cautions.Add("Do not judge business only from the 10th house; 2nd, 6th, 7th and 11th houses must also support cash flow, service capacity, trade and gains.");
            cautions.Add("Avoid debt-heavy starts if 6th/8th/12th pressure is active.");
        }
        else if (IsPartnershipConcern(concern))
        {
            cautions.Add("Do not enter partnership only due to trust or friendship; check contracts, money, authority and dispute handling.");
        }
        else if (IsSeparationConcern(concern))
        {
            cautions.Add("Astrology should not replace legal, safety or counselling support in separation matters.");
            cautions.Add("High pressure periods can show conflict activation, not always permanent separation.");
        }
        else if (pressureDominant)
        {
            cautions.Add("Pressure is active, so irreversible decisions need slower review.");
        }

        return cautions.Distinct(StringComparer.OrdinalIgnoreCase).Take(5).ToList();
    }

    private static bool IsBusinessConcern(string concern)
    {
        return concern.Contains("business", StringComparison.OrdinalIgnoreCase)
            || concern.Contains("career_business", StringComparison.OrdinalIgnoreCase)
            || concern.Contains("startup", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPartnershipConcern(string concern)
    {
        return concern.Contains("partner", StringComparison.OrdinalIgnoreCase)
            || concern.Contains("partnership", StringComparison.OrdinalIgnoreCase)
            || concern.Contains("agreement", StringComparison.OrdinalIgnoreCase)
            || concern.Contains("client", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSeparationConcern(string concern)
    {
        return concern.Contains("separation", StringComparison.OrdinalIgnoreCase)
            || concern.Contains("divorce", StringComparison.OrdinalIgnoreCase)
            || concern.Contains("distance", StringComparison.OrdinalIgnoreCase)
            || concern.Contains("relationship_break", StringComparison.OrdinalIgnoreCase);
    }
    private static string BuildSummary(LifeAreaConfiguration areaConfig, LifeAreaActivation area)
    {
        var template = string.IsNullOrWhiteSpace(areaConfig.SummaryTemplate)
            ? $"{areaConfig.DisplayName} currently receives {area.ActivationLevel.ToLowerInvariant()} attention."
            : areaConfig.SummaryTemplate;

        return template
            .Replace("{activationLevel}", area.ActivationLevel.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase)
            .Replace("{tone}", area.Tone.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);
    }

    private static List<string> BuildSupportiveFactors(LifeAreaConfiguration areaConfig, LifeAreaActivation area)
    {
        var factors = area.SupportiveFactors.Count > 0
            ? area.SupportiveFactors
            : new List<string> { $"{areaConfig.DisplayName} may receive support where active houses show constructive capacity." };

        return factors
            .Select(Cautious)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();
    }

    private static List<string> BuildPressureFactors(LifeAreaConfiguration areaConfig, LifeAreaActivation area)
    {
        var factors = area.PressureFactors.Count > 0
            ? area.PressureFactors
            : new List<string> { $"{areaConfig.DisplayName} may need patient handling where pressure factors are present." };

        return factors
            .Select(Cautious)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();
    }

    private static List<FocusedActivatedHouseDto> BuildActivatedHouses(
        LifeAreaConfiguration areaConfig,
        IReadOnlyList<HouseActivationScore> houseScores)
    {
        return areaConfig.Houses
            .Select(house => new
            {
                house.House,
                Role = GetHouseRole(house.House),
                Score = houseScores.FirstOrDefault(score => score.House == house.House)?.TotalScore ?? 0
            })
            .OrderByDescending(house => house.Score)
            .ThenBy(house => house.House)
            .Take(4)
            .Select(house => new FocusedActivatedHouseDto
            {
                House = house.House,
                Role = house.Role,
                ActivationScore = house.Score
            })
            .ToList();
    }

    private static string GetHouseRole(int house)
    {
        return house switch
        {
            1 => "Self, vitality and direction",
            2 => "Money, family and values",
            3 => "Effort, communication and courage",
            4 => "Home, property and inner peace",
            5 => "Children, education and creativity",
            6 => "Debt, disputes, work and health routines",
            7 => "Relationship, marriage and agreements",
            8 => "Shared resources, uncertainty and transformation",
            9 => "Travel, guidance and higher learning",
            10 => "Profession, responsibility and status",
            11 => "Gains, networks and fulfilment",
            12 => "Sleep, expenses, retreat and foreign matters",
            _ => "Life area"
        };
    }

    private static string Cautious(string value)
    {
        if (value.Contains("will ", StringComparison.OrdinalIgnoreCase))
        {
            return value.Replace("will ", "may ", StringComparison.OrdinalIgnoreCase);
        }

        return value;
    }
}




