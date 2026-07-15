using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Calculations;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models.Muhurtha;

namespace KundaliEngine.API.Services.Muhurtha;

public class MuhurthaService : IMuhurthaService
{
    private readonly MuhurthaMetadataService _metadataService;
    private readonly IMuhurthaRuleConfigurationProvider _ruleConfigurationProvider;
    private readonly IMuhurthaCandidateGenerator _candidateGenerator;
    private readonly IMuhurthaRecommendationService _recommendationService;
    private readonly IMuhurthaRuleEvaluationService _ruleEvaluationService;
    private readonly IEphemerisProvider _ephemerisProvider;
    private readonly PanchangService _panchangService;

    public MuhurthaService(
        MuhurthaMetadataService metadataService,
        IMuhurthaRuleConfigurationProvider ruleConfigurationProvider,
        IMuhurthaCandidateGenerator candidateGenerator,
        IMuhurthaRecommendationService recommendationService,
        IMuhurthaRuleEvaluationService ruleEvaluationService,
        IEphemerisProvider ephemerisProvider,
        PanchangService panchangService)
    {
        _metadataService = metadataService;
        _ruleConfigurationProvider = ruleConfigurationProvider;
        _candidateGenerator = candidateGenerator;
        _recommendationService = recommendationService;
        _ruleEvaluationService = ruleEvaluationService;
        _ephemerisProvider = ephemerisProvider;
        _panchangService = panchangService;
    }

    public async Task<MuhurthaResponse> FindMuhurthaAsync(
        MuhurthaRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var eventConfiguration = _metadataService.GetEventConfiguration(request.EventType)
            ?? new MuhurthaEventConfiguration
            {
                EventType = request.EventType,
                DisplayName = request.EventType.ToString(),
                IsActive = true
            };

        var regionalProfile = _metadataService.GetRegionalProfile(request.RegionalProfile)
            ?? new MuhurthaRegionalProfile
            {
                ProfileType = request.RegionalProfile,
                DisplayName = request.RegionalProfile.ToString()
            };

        var ruleConfigurations = await _ruleConfigurationProvider.GetRulesAsync(
            request.EventType,
            request.RegionalProfile,
            cancellationToken);

        var context = new MuhurthaRuleContext
        {
            Request = request,
            EventConfiguration = eventConfiguration,
            RegionalProfile = regionalProfile,
            RuleConfigurations = ruleConfigurations
        };

        var candidates = await _candidateGenerator.GenerateAsync(context, cancellationToken);
        foreach (var candidate in candidates)
        {
            candidate.RuleResults = (await _ruleEvaluationService.EvaluateAsync(context, candidate, cancellationToken)).ToList();
        }

        var recommendations = _recommendationService.RankAndMerge(candidates);
        AssignPanchangTimings(request, recommendations);
        AssignSlotIds(request, recommendations);

        return new MuhurthaResponse
        {
            EventType = request.EventType,
            RegionalProfile = request.RegionalProfile,
            Location = request.Location,
            RequestedStart = request.RequestedStart,
            RequestedEnd = request.RequestedEnd,
            UsedPersonalHoroscope = context.ShouldEvaluatePersonalHoroscope,
            UnsupportedCalculations = BuildUnsupportedCalculations(context),
            RecommendedSlots = recommendations.ToList()
        };
    }



    private void AssignPanchangTimings(
        MuhurthaRequest request,
        IReadOnlyList<MuhurthaRecommendation> recommendations)
    {
        var location = new GeoLocation(request.Location.Latitude, request.Location.Longitude);
        foreach (var recommendation in recommendations)
        {
            recommendation.PanchangTimings = BuildPanchangTimings(recommendation, location);
        }
    }

    private MuhurthaPanchangTimings BuildPanchangTimings(
        MuhurthaRecommendation recommendation,
        GeoLocation location)
    {
        var midpoint = recommendation.StartTime + TimeSpan.FromTicks((recommendation.EndTime - recommendation.StartTime).Ticks / 2);
        var sun = _ephemerisProvider.GetPlanet(Planet.Sun, midpoint.UtcDateTime, location);
        var moon = _ephemerisProvider.GetPlanet(Planet.Moon, midpoint.UtcDateTime, location);
        var panchang = _panchangService.Calculate(midpoint.DateTime, sun.Longitude, moon.Longitude);
        var ascendant = _ephemerisProvider.GetAscendant(midpoint.UtcDateTime, location);

        return new MuhurthaPanchangTimings
        {
            Tithi = BuildDetail(
                ResolveMuhurthaTithi(sun.Longitude, moon.Longitude),
                BuildInterval(midpoint, time => TithiIndex(time, location), TimeSpan.FromHours(3), TimeSpan.FromHours(36))),
            Nakshatra = BuildDetail(
                panchang.Nakshatra,
                BuildInterval(midpoint, time => NakshatraIndex(time, location), TimeSpan.FromHours(3), TimeSpan.FromHours(36))),
            Karana = BuildDetail(
                panchang.Karana,
                BuildInterval(midpoint, time => KaranaDisplayIndex(time, location), TimeSpan.FromMinutes(90), TimeSpan.FromHours(72))),
            Yoga = BuildDetail(
                panchang.Yoga,
                BuildInterval(midpoint, time => YogaIndex(time, location), TimeSpan.FromHours(3), TimeSpan.FromHours(48))),
            ChandraRashi = BuildDetail(
                moon.Sign.ToString(),
                BuildInterval(midpoint, time => MoonSignIndex(time, location), TimeSpan.FromHours(6), TimeSpan.FromDays(4))),
            Lagna = BuildDetail(
                ZodiacCalculator.GetSign(ascendant.Longitude).ToString(),
                BuildInterval(midpoint, time => LagnaSignIndex(time, location), TimeSpan.FromMinutes(30), TimeSpan.FromHours(6)))
        };
    }

    private static MuhurthaTimingDetail BuildDetail(
        string name,
        (DateTimeOffset Start, DateTimeOffset End) interval)
    {
        return new MuhurthaTimingDetail
        {
            Name = name,
            StartTime = interval.Start,
            EndTime = interval.End
        };
    }

    private (DateTimeOffset Start, DateTimeOffset End) BuildInterval(
        DateTimeOffset midpoint,
        Func<DateTime, int> keySelector,
        TimeSpan step,
        TimeSpan maxSearch)
    {
        var key = keySelector(midpoint.UtcDateTime);
        return (
            FindBoundary(midpoint, keySelector, key, -step, maxSearch, forward: false),
            FindBoundary(midpoint, keySelector, key, step, maxSearch, forward: true));
    }

    private static DateTimeOffset FindBoundary(
        DateTimeOffset midpoint,
        Func<DateTime, int> keySelector,
        int key,
        TimeSpan step,
        TimeSpan maxSearch,
        bool forward)
    {
        var inside = midpoint;
        var outside = midpoint + step;
        var searched = step.Duration();

        while (searched <= maxSearch && keySelector(outside.UtcDateTime) == key)
        {
            inside = outside;
            outside += step;
            searched += step.Duration();
        }

        if (keySelector(outside.UtcDateTime) == key)
        {
            return outside;
        }

        for (var i = 0; i < 24; i++)
        {
            var probe = new DateTimeOffset(inside.Ticks + ((outside.Ticks - inside.Ticks) / 2), inside.Offset);
            if (keySelector(probe.UtcDateTime) == key)
            {
                inside = probe;
            }
            else
            {
                outside = probe;
            }
        }

        return forward ? outside : inside;
    }

    private int TithiIndex(DateTime utc, GeoLocation location)
    {
        var sun = _ephemerisProvider.GetPlanet(Planet.Sun, utc, location);
        var moon = _ephemerisProvider.GetPlanet(Planet.Moon, utc, location);
        return (int)(NormalizeLongitude(moon.Longitude - sun.Longitude) / 12);
    }

    private int NakshatraIndex(DateTime utc, GeoLocation location)
    {
        var moon = _ephemerisProvider.GetPlanet(Planet.Moon, utc, location);
        return (int)(NormalizeLongitude(moon.Longitude) / (360.0 / 27.0));
    }

    private int KaranaDisplayIndex(DateTime utc, GeoLocation location)
    {
        var sun = _ephemerisProvider.GetPlanet(Planet.Sun, utc, location);
        var moon = _ephemerisProvider.GetPlanet(Planet.Moon, utc, location);
        return Math.Clamp((int)(NormalizeLongitude(moon.Longitude - sun.Longitude) / 6), 0, 10);
    }

    private int YogaIndex(DateTime utc, GeoLocation location)
    {
        var sun = _ephemerisProvider.GetPlanet(Planet.Sun, utc, location);
        var moon = _ephemerisProvider.GetPlanet(Planet.Moon, utc, location);
        return (int)(NormalizeLongitude(sun.Longitude + moon.Longitude) / (360.0 / 27.0));
    }

    private int MoonSignIndex(DateTime utc, GeoLocation location)
    {
        return (int)_ephemerisProvider.GetPlanet(Planet.Moon, utc, location).Sign;
    }

    private int LagnaSignIndex(DateTime utc, GeoLocation location)
    {
        return (int)ZodiacCalculator.GetSign(_ephemerisProvider.GetAscendant(utc, location).Longitude);
    }

    private static string ResolveMuhurthaTithi(double sunLongitude, double moonLongitude)
    {
        var diff = NormalizeLongitude(moonLongitude - sunLongitude);
        var tithiNumber = (int)(diff / 12) + 1;
        var pakshaTithiNumber = ((tithiNumber - 1) % 15) + 1;
        var isShuklaPaksha = tithiNumber <= 15;

        return pakshaTithiNumber switch
        {
            1 => "Prathama",
            2 => "Dvitiya",
            3 => "Tritiya",
            4 => "Chaturthi",
            5 => "Panchami",
            6 => "Shashthi",
            7 => "Saptami",
            8 => "Ashtami",
            9 => "Navami",
            10 => "Dashami",
            11 => "Ekadashi",
            12 => "Dwadashi",
            13 => "Trayodashi",
            14 => "Chaturdashi",
            15 when isShuklaPaksha => "Purnima",
            15 => "Amavasya",
            _ => "Unknown"
        };
    }

    private static double NormalizeLongitude(double angle)
    {
        return (angle % 360 + 360) % 360;
    }

    private static void AssignSlotIds(
        MuhurthaRequest request,
        IReadOnlyList<MuhurthaRecommendation> recommendations)
    {
        foreach (var recommendation in recommendations)
        {
            recommendation.SlotId = BuildSlotId(request, recommendation);
        }
    }

    private static string BuildSlotId(
        MuhurthaRequest request,
        MuhurthaRecommendation recommendation)
    {
        var location = Slug(request.Location.Name);
        var eventType = Slug(request.EventType.ToString());
        var start = recommendation.StartTime.ToString("yyyyMMdd-HHmm");
        var end = recommendation.EndTime.ToString("HHmm");

        return $"MUH-{start}-{end}-{eventType}-{location}".ToUpperInvariant();
    }

    private static string Slug(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "NA";
        }

        var chars = value
            .Trim()
            .Select(character => char.IsLetterOrDigit(character) ? char.ToUpperInvariant(character) : '-')
            .ToArray();
        var slug = new string(chars);

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Trim('-');
    }
    private static void ValidateRequest(MuhurthaRequest request)
    {
        if (request.RequestedEnd <= request.RequestedStart)
        {
            throw new ArgumentException("RequestedEnd must be later than RequestedStart.");
        }

        if (string.IsNullOrWhiteSpace(request.Location.Timezone))
        {
            throw new ArgumentException("Location timezone is required.");
        }
    }

    private static List<string> BuildUnsupportedCalculations(MuhurthaRuleContext context)
    {
        var unsupported = new List<string>
        {
            "Varjyam source-data calculation",
            "Panchanga transition-boundary splitting beyond generated time windows",
            "Eclipse and Sutak period calculation",
            "Sankranti restriction windows",
            "Detailed Navamsha confirmation and Vargottama support",
            "Expert override and audit workflow",
            "Natal contraindication rules by event type"
        };

        if (!context.Request.Filters.CheckPersonalHoroscope)
        {
            unsupported.Add("Personal horoscope rules are available but were not requested for this search.");
        }

        if (!context.Request.Filters.CheckMuhurthaLagna)
        {
            unsupported.Add("Muhurtha chart rules are available but were not requested for this search.");
        }

        return unsupported;
    }
}




