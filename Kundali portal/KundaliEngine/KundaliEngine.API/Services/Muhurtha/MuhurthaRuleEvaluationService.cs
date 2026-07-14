using KundaliEngine.API.Helper_functions;
using KundaliEngine.API.Services.Muhurtha.Models;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Calculations;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Dasha;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.Domain.Models.Muhurtha;
using KundaliEngine.Domain.Rules;

namespace KundaliEngine.API.Services.Muhurtha;

public class MuhurthaRuleEvaluationService : IMuhurthaRuleEvaluationService
{
    private static readonly HashSet<int> FavourableChandraPositions = new() { 1, 3, 6, 7, 10, 11 };
    private static readonly HashSet<int> DifficultChandraPositions = new() { 4, 8, 12 };

    private readonly MuhurthaMetadataService _metadataService;
    private readonly IMuhurthaSunriseSunsetService _sunriseSunsetService;
    private readonly IMuhurthaGenericTimeWindowService _genericTimeWindowService;
    private readonly IEphemerisProvider _ephemerisProvider;
    private readonly PanchangService _panchangService;
    private readonly ChartService _chartService;
    private readonly DashaService _dashaService;
    private readonly AspectService _aspectService;
    private readonly PlanetStrengthService _planetStrengthService;

    public MuhurthaRuleEvaluationService(
        MuhurthaMetadataService metadataService,
        IMuhurthaSunriseSunsetService sunriseSunsetService,
        IMuhurthaGenericTimeWindowService genericTimeWindowService,
        IEphemerisProvider ephemerisProvider,
        PanchangService panchangService,
        ChartService chartService,
        DashaService dashaService,
        AspectService aspectService,
        PlanetStrengthService planetStrengthService)
    {
        _metadataService = metadataService;
        _sunriseSunsetService = sunriseSunsetService;
        _genericTimeWindowService = genericTimeWindowService;
        _ephemerisProvider = ephemerisProvider;
        _panchangService = panchangService;
        _chartService = chartService;
        _dashaService = dashaService;
        _aspectService = aspectService;
        _planetStrengthService = planetStrengthService;
    }

    public Task<IReadOnlyList<MuhurthaRuleResult>> EvaluateAsync(
        MuhurthaRuleContext context,
        MuhurthaCandidateSlot candidate,
        CancellationToken cancellationToken = default)
    {
        var results = new List<MuhurthaRuleResult>();
        if (TryBuildVastuEligibilityResult(candidate, out var vastuEligibilityResult))
        {
            results.Add(vastuEligibilityResult);
        }

        results.AddRange(EvaluateGenericTimeRules(context, candidate));

        var midpoint = candidate.StartTime + TimeSpan.FromTicks((candidate.EndTime - candidate.StartTime).Ticks / 2);
        var eventPlanets = _ephemerisProvider.GetAllPlanets(
            midpoint.UtcDateTime,
            new GeoLocation(context.Request.Location.Latitude, context.Request.Location.Longitude)).ToList();
        var sun = eventPlanets.First(planet => planet.Planet == Planet.Sun);
        var moon = eventPlanets.First(planet => planet.Planet == Planet.Moon);
        var panchang = _panchangService.Calculate(midpoint.DateTime, sun.Longitude, moon.Longitude);
        var muhurthaTithi = ResolveMuhurthaTithi(sun.Longitude, moon.Longitude);
        var location = new GeoLocation(context.Request.Location.Latitude, context.Request.Location.Longitude);
        var intervals = BuildDisplayIntervals(midpoint, location);

        results.AddRange(EvaluatePanchangaRules(context, panchang, muhurthaTithi, intervals));

        if (context.Request.Filters.CheckPersonalHoroscope)
        {
            results.AddRange(EvaluatePersonalRules(context, midpoint, moon));
        }

        if (context.Request.Filters.CheckMuhurthaLagna)
        {
            results.AddRange(EvaluateMuhurthaChartRules(context, midpoint, eventPlanets));
        }

        return Task.FromResult<IReadOnlyList<MuhurthaRuleResult>>(results);
    }

    private static bool TryBuildVastuEligibilityResult(
        MuhurthaCandidateSlot candidate,
        out MuhurthaRuleResult result)
    {
        result = new MuhurthaRuleResult();
        if (!candidate.Metadata.TryGetValue("vastuEligibility.paksha", out var paksha))
        {
            return false;
        }

        var tithi = candidate.Metadata.GetValueOrDefault("vastuEligibility.tithi", string.Empty);
        var weekday = candidate.Metadata.GetValueOrDefault("vastuEligibility.weekday", string.Empty);
        var moonSign = candidate.Metadata.GetValueOrDefault("vastuEligibility.moonSign", string.Empty);
        var applicability = candidate.Metadata.GetValueOrDefault("vastuEligibility.applicability", string.Empty);
        var ahuti = candidate.Metadata.GetValueOrDefault("vastuEligibility.ahuti", string.Empty);
        var source = candidate.Metadata.GetValueOrDefault("vastuEligibility.source", "VastuEligibilityRules");
        var scoreImpact = int.TryParse(candidate.Metadata.GetValueOrDefault("vastuEligibility.scoreImpact", "10"), out var parsedScore)
            ? parsedScore
            : 10;

        result = new MuhurthaRuleResult
        {
            RuleKey = "vastu_date_eligibility",
            Status = MuhurthaRuleStatus.Passed,
            Severity = MuhurthaRuleSeverity.Positive,
            ScoreImpact = scoreImpact,
            Summary = $"Vastu eligibility matched: {paksha} {tithi}, {weekday}, Moon in {moonSign}.",
            Recommendation = "This slot is inside an inferred traditional Vastu/Gruha Pravesha date yoga.",
            Metadata =
            {
                ["source"] = source,
                ["group"] = MuhurthaRuleGroup.PanchangaRules.ToString(),
                ["paksha"] = paksha,
                ["tithi"] = tithi,
                ["weekday"] = weekday,
                ["moonSign"] = moonSign,
                ["applicability"] = applicability,
                ["ahuti"] = ahuti
            }
        };

        return true;
    }

    private IEnumerable<MuhurthaRuleResult> EvaluateGenericTimeRules(
        MuhurthaRuleContext context,
        MuhurthaCandidateSlot candidate)
    {
        var date = DateOnly.FromDateTime(candidate.StartTime.Date);
        var (sunrise, sunset) = _sunriseSunsetService.Calculate(date, context.Request.Location, candidate.StartTime.Offset);
        var dayContext = new MuhurthaDayContext
        {
            Date = date,
            Sunrise = sunrise,
            Sunset = sunset,
            DayStart = candidate.StartTime,
            DayEnd = candidate.EndTime
        };

        foreach (var window in _genericTimeWindowService.BuildWindows(dayContext))
        {
            if (!Overlaps(candidate.StartTime, candidate.EndTime, window.StartTime, window.EndTime))
            {
                continue;
            }

            yield return BuildWindowResult(context, window, candidate);
        }
    }

    private IEnumerable<MuhurthaRuleResult> EvaluatePanchangaRules(
        MuhurthaRuleContext context,
        PanchangDetails panchang,
        string muhurthaTithi,
        IReadOnlyDictionary<string, (DateTimeOffset Start, DateTimeOffset End)> intervals)
    {
        var suitability = _metadataService.GetPanchangaSuitability(context.Request.EventType);
        if (suitability is null)
        {
            yield return BuildSkipped(
                "panchanga_suitability",
                "No Panchanga suitability configuration was found for this event.",
                MuhurthaRuleGroup.PanchangaRules);
            yield break;
        }

        yield return AddIntervalMetadata(EvaluateTithiFactor(panchang, muhurthaTithi, suitability.PositiveTithis, suitability.AvoidTithis), intervals, "tithi");
        yield return AddIntervalMetadata(EvaluateNamedFactor("nakshatra_suitability", "Nakshatra", panchang.Nakshatra, suitability.PositiveNakshatras, suitability.AvoidNakshatras), intervals, "nakshatra");
        yield return AddIntervalMetadata(EvaluateNamedFactor("yoga_suitability", "Yoga", panchang.Yoga, suitability.PositiveYogas, suitability.AvoidYogas), intervals, "yoga");
        yield return AddIntervalMetadata(EvaluateNamedFactor("karana_suitability", "Karana", panchang.Karana, suitability.PositiveKaranas, suitability.AvoidKaranas), intervals, "karana");

        if (string.Equals(panchang.Karana, "Visti", StringComparison.OrdinalIgnoreCase))
        {
            yield return new MuhurthaRuleResult
            {
                RuleKey = "bhadra_vishti",
                Status = MuhurthaRuleStatus.Failed,
                Severity = MuhurthaRuleSeverity.StrongAvoid,
                ScoreImpact = -14,
                Summary = "The slot falls under Vishti Karana, treated as Bhadra for auspicious work.",
                Recommendation = "Prefer a slot outside Vishti Karana for auspicious ceremonies.",
                Metadata =
                {
                    ["source"] = "PanchangService",
                    ["group"] = MuhurthaRuleGroup.PanchangaRules.ToString()
                }
            };
        }
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
    private IReadOnlyDictionary<string, (DateTimeOffset Start, DateTimeOffset End)> BuildDisplayIntervals(DateTimeOffset midpoint, GeoLocation location)
    {
        return new Dictionary<string, (DateTimeOffset Start, DateTimeOffset End)>
        {
            ["tithi"] = BuildInterval(midpoint, time => TithiIndex(time, location), TimeSpan.FromHours(3), TimeSpan.FromHours(36)),
            ["nakshatra"] = BuildInterval(midpoint, time => NakshatraIndex(time, location), TimeSpan.FromHours(3), TimeSpan.FromHours(36)),
            ["yoga"] = BuildInterval(midpoint, time => YogaIndex(time, location), TimeSpan.FromHours(3), TimeSpan.FromHours(48)),
            ["karana"] = BuildInterval(midpoint, time => KaranaIndex(time, location), TimeSpan.FromMinutes(90), TimeSpan.FromHours(24))
        };
    }

    private (DateTimeOffset Start, DateTimeOffset End) BuildSignInterval(DateTimeOffset midpoint, GeoLocation location, Planet planet)
    {
        return BuildInterval(midpoint, time => (int)_ephemerisProvider.GetPlanet(planet, time, location).Sign, TimeSpan.FromHours(6), TimeSpan.FromDays(4));
    }

    private (DateTimeOffset Start, DateTimeOffset End) BuildLagnaInterval(DateTimeOffset midpoint, GeoLocation location)
    {
        return BuildInterval(midpoint, time => (int)ZodiacCalculator.GetSign(_ephemerisProvider.GetAscendant(time, location).Longitude), TimeSpan.FromMinutes(30), TimeSpan.FromHours(6));
    }

    private (DateTimeOffset Start, DateTimeOffset End) BuildInterval(DateTimeOffset midpoint, Func<DateTime, int> keySelector, TimeSpan step, TimeSpan maxSearch)
    {
        var key = keySelector(midpoint.UtcDateTime);
        return (
            FindBoundary(midpoint, keySelector, key, -step, maxSearch, forward: false),
            FindBoundary(midpoint, keySelector, key, step, maxSearch, forward: true));
    }

    private static DateTimeOffset FindBoundary(DateTimeOffset midpoint, Func<DateTime, int> keySelector, int key, TimeSpan step, TimeSpan maxSearch, bool forward)
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
            var midpointTicks = inside.Ticks + ((outside.Ticks - inside.Ticks) / 2);
            var probe = new DateTimeOffset(midpointTicks, inside.Offset);
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

    private int YogaIndex(DateTime utc, GeoLocation location)
    {
        var sun = _ephemerisProvider.GetPlanet(Planet.Sun, utc, location);
        var moon = _ephemerisProvider.GetPlanet(Planet.Moon, utc, location);
        return (int)(NormalizeLongitude(sun.Longitude + moon.Longitude) / (360.0 / 27.0));
    }

    private int KaranaIndex(DateTime utc, GeoLocation location)
    {
        var sun = _ephemerisProvider.GetPlanet(Planet.Sun, utc, location);
        var moon = _ephemerisProvider.GetPlanet(Planet.Moon, utc, location);
        return Math.Clamp((int)(NormalizeLongitude(moon.Longitude - sun.Longitude) / 6), 0, 10);
    }

    private static MuhurthaRuleResult AddIntervalMetadata(
        MuhurthaRuleResult result,
        IReadOnlyDictionary<string, (DateTimeOffset Start, DateTimeOffset End)> intervals,
        string key)
    {
        if (intervals.TryGetValue(key, out var interval))
        {
            result.Metadata["startTime"] = FormatMetadataTime(interval.Start);
            result.Metadata["endTime"] = FormatMetadataTime(interval.End);
        }

        return result;
    }

    private static string FormatMetadataTime(DateTimeOffset value)
    {
        return value.ToString("HH:mm");
    }

    private IEnumerable<MuhurthaRuleResult> EvaluatePersonalRules(
        MuhurthaRuleContext context,
        DateTimeOffset midpoint,
        PlanetPosition eventMoon)
    {
        if (!TryBuildNatalChart(context.Request.Horoscope, context.Request.RequestedStart.Offset, out var natalPlanets, out var birthUtc))
        {
            yield return BuildSkipped(
                "personal_horoscope_not_supplied",
                "Personal horoscope checks were requested but complete horoscope details were not supplied.",
                MuhurthaRuleGroup.PersonalHoroscopeRules);
            yield break;
        }

        var natalMoon = natalPlanets.First(planet => planet.Planet == Planet.Moon);
        yield return EvaluateTaraBala(natalMoon.Nakshatra, eventMoon.Nakshatra);
        yield return EvaluateChandraBala(natalMoon.Sign, eventMoon.Sign);

        var birthDasha = _dashaService.GetBirthMahadasha(natalMoon);
        var dasha = _dashaService.CalculateCurrentDasha(
            birthDasha.Planet,
            birthDasha.RemainingYears,
            birthUtc,
            midpoint.UtcDateTime);
        var pratyantardasha = CalculateCurrentPratyantardasha(dasha.CurrentAntardasha, midpoint.UtcDateTime);
        yield return new MuhurthaRuleResult
        {
            RuleKey = "current_dasha",
            Status = MuhurthaRuleStatus.Passed,
            Severity = MuhurthaRuleSeverity.Neutral,
            Summary = $"Current Dasha at this slot: {dasha.CurrentMahadasha.Planet} Mahadasha, {dasha.CurrentAntardasha.SubPlanet} Antardasha, and {pratyantardasha} Pratyantardasha.",
            Recommendation = "Use Dasha lord strength and event-house links to refine personalized Muhurtha preference.",
            Metadata =
            {
                ["source"] = "DashaService",
                ["group"] = MuhurthaRuleGroup.PersonalHoroscopeRules.ToString(),
                ["pratyantardasha"] = pratyantardasha.ToString()
            }
        };

        foreach (var result in EvaluateAdvancedPersonalRules(context, midpoint, natalPlanets, dasha, pratyantardasha))
        {
            yield return result;
        }
    }

    private IEnumerable<MuhurthaRuleResult> EvaluateMuhurthaChartRules(
        MuhurthaRuleContext context,
        DateTimeOffset midpoint,
        List<PlanetPosition> eventPlanets)
    {
        var chartPlanets = _chartService.GenerateChart(
            midpoint.UtcDateTime,
            context.Request.Location.Latitude,
            context.Request.Location.Longitude);
        var ascendant = _ephemerisProvider.GetAscendant(
            midpoint.UtcDateTime,
            new GeoLocation(context.Request.Location.Latitude, context.Request.Location.Longitude));
        var lagnaSign = ZodiacCalculator.GetSign(ascendant.Longitude);
        var chartLocation = new GeoLocation(context.Request.Location.Latitude, context.Request.Location.Longitude);
        var moonInterval = BuildSignInterval(midpoint, chartLocation, Planet.Moon);
        var lagnaInterval = BuildLagnaInterval(midpoint, chartLocation);
        var moon = chartPlanets.First(planet => planet.Planet == Planet.Moon);
        var beneficsInEventHouses = chartPlanets
            .Where(planet => context.EventConfiguration.ImportantHouses.Contains((int)planet.House)
                && HelperData.IsNaturalBenefic(planet.Planet))
            .Select(planet => planet.Planet.ToString())
            .Distinct()
            .ToList();
        var maleficsInEventHouses = chartPlanets
            .Where(planet => context.EventConfiguration.ImportantHouses.Contains((int)planet.House)
                && HelperData.IsNaturalMaleficOrShadow(planet.Planet))
            .Select(planet => planet.Planet.ToString())
            .Distinct()
            .ToList();

        yield return new MuhurthaRuleResult
        {
            RuleKey = "moon_strength",
            Status = moon.IsCombust ? MuhurthaRuleStatus.Warning : MuhurthaRuleStatus.Passed,
            Severity = moon.IsCombust ? MuhurthaRuleSeverity.SoftAvoid : MuhurthaRuleSeverity.Positive,
            ScoreImpact = moon.IsCombust ? -6 : 6,
            Summary = moon.IsCombust
                ? "Muhurtha Moon is marked combust or weak by the chart service."
                : "Muhurtha Moon is available for chart-level evaluation.",
            Recommendation = "Prefer a strong, unafflicted Moon for important activities.",
            Metadata =
            {
                ["source"] = "ChartService",
                ["group"] = MuhurthaRuleGroup.MuhurthaChartRules.ToString(),
                ["moonSign"] = moon.Sign.ToString(),
                ["moonStartTime"] = FormatMetadataTime(moonInterval.Start),
                ["moonEndTime"] = FormatMetadataTime(moonInterval.End),
                ["lagnaSign"] = lagnaSign.ToString(),
                ["lagnaStartTime"] = FormatMetadataTime(lagnaInterval.Start),
                ["lagnaEndTime"] = FormatMetadataTime(lagnaInterval.End)
            }
        };

        if (beneficsInEventHouses.Count > 0)
        {
            yield return new MuhurthaRuleResult
            {
                RuleKey = "event_house_benefic_support",
                Status = MuhurthaRuleStatus.Passed,
                Severity = MuhurthaRuleSeverity.Positive,
                ScoreImpact = 8,
                Summary = $"Benefic support found in event houses: {string.Join(", ", beneficsInEventHouses)}.",
                Recommendation = "This supports the selected event houses.",
                Metadata =
                {
                    ["source"] = "ChartService",
                    ["group"] = MuhurthaRuleGroup.MuhurthaChartRules.ToString()
                }
            };
        }

        if (maleficsInEventHouses.Count > 0)
        {
            yield return new MuhurthaRuleResult
            {
                RuleKey = "event_house_malefic_pressure",
                Status = MuhurthaRuleStatus.Warning,
                Severity = MuhurthaRuleSeverity.SoftAvoid,
                ScoreImpact = -6,
                Summary = $"Malefic or shadow planet pressure found in event houses: {string.Join(", ", maleficsInEventHouses)}.",
                Recommendation = "Review this slot carefully for the selected event.",
                Metadata =
                {
                    ["source"] = "ChartService",
                    ["group"] = MuhurthaRuleGroup.MuhurthaChartRules.ToString()
                }
            };
        }
    }

    private IEnumerable<MuhurthaRuleResult> EvaluateAdvancedPersonalRules(
        MuhurthaRuleContext context,
        DateTimeOffset midpoint,
        List<PlanetPosition> natalPlanets,
        DashaCalculationResult dasha,
        Planet pratyantardasha)
    {
        var activePlanets = new[]
        {
            dasha.CurrentMahadasha.Planet,
            dasha.CurrentAntardasha.SubPlanet,
            pratyantardasha
        }.Distinct().ToList();

        foreach (var planet in activePlanets)
        {
            var natalPosition = natalPlanets.FirstOrDefault(position => position.Planet == planet);
            if (natalPosition is null)
            {
                continue;
            }

            yield return EvaluateDashaLordNatalStrength(planet, natalPosition);
            yield return EvaluateDashaLordEventHouseLink(context, planet, natalPosition);
        }

        foreach (var result in EvaluateTransitActivation(context, midpoint, activePlanets))
        {
            yield return result;
        }
    }

    private MuhurthaRuleResult EvaluateDashaLordNatalStrength(Planet planet, PlanetPosition natalPosition)
    {
        var strength = _planetStrengthService.GetStrength(planet, natalPosition.Sign);
        var scoreImpact = strength switch
        {
            PlanetStrength.Exalted => 12,
            PlanetStrength.OwnSign => 9,
            PlanetStrength.Debilitated => -14,
            _ => 0
        };

        if (natalPosition.IsCombust)
        {
            scoreImpact -= 5;
        }

        if (natalPosition.IsRetrograde)
        {
            scoreImpact -= 2;
        }

        var severity = scoreImpact switch
        {
            > 0 => MuhurthaRuleSeverity.Positive,
            < -8 => MuhurthaRuleSeverity.StrongAvoid,
            < 0 => MuhurthaRuleSeverity.SoftAvoid,
            _ => MuhurthaRuleSeverity.Neutral
        };

        return new MuhurthaRuleResult
        {
            RuleKey = "dasha_lord_natal_strength",
            Status = scoreImpact < 0 ? MuhurthaRuleStatus.Warning : MuhurthaRuleStatus.Passed,
            Severity = severity,
            ScoreImpact = scoreImpact,
            Summary = $"Active Dasha lord {planet} has natal strength {strength} in {natalPosition.Sign}.",
            Recommendation = scoreImpact < 0
                ? "Prefer stronger supporting rules when an active Dasha lord is weak or afflicted."
                : "This active Dasha lord supports personalized timing.",
            Metadata =
            {
                ["source"] = "PlanetStrengthService",
                ["group"] = MuhurthaRuleGroup.PersonalHoroscopeRules.ToString(),
                ["planet"] = planet.ToString(),
                ["natalHouse"] = ((int)natalPosition.House).ToString()
            }
        };
    }

    private static MuhurthaRuleResult EvaluateDashaLordEventHouseLink(
        MuhurthaRuleContext context,
        Planet planet,
        PlanetPosition natalPosition)
    {
        var natalHouse = (int)natalPosition.House;
        var supportsEventHouse = context.EventConfiguration.ImportantHouses.Contains(natalHouse);
        var difficultHouse = natalHouse is 6 or 8 or 12;
        var scoreImpact = supportsEventHouse ? 10 : difficultHouse ? -7 : 0;

        return new MuhurthaRuleResult
        {
            RuleKey = "dasha_lord_event_house_link",
            Status = scoreImpact < 0 ? MuhurthaRuleStatus.Warning : MuhurthaRuleStatus.Passed,
            Severity = supportsEventHouse
                ? MuhurthaRuleSeverity.Positive
                : difficultHouse ? MuhurthaRuleSeverity.SoftAvoid : MuhurthaRuleSeverity.Neutral,
            ScoreImpact = scoreImpact,
            Summary = supportsEventHouse
                ? $"Active Dasha lord {planet} occupies natal event house {natalHouse}."
                : $"Active Dasha lord {planet} occupies natal house {natalHouse}.",
            Recommendation = supportsEventHouse
                ? "This links the active Dasha to the selected event."
                : difficultHouse ? "Review carefully because the active Dasha lord is placed in a difficult natal house." : "This Dasha lord has no direct event-house link.",
            Metadata =
            {
                ["source"] = "MuhurthaRuleEvaluationService",
                ["group"] = MuhurthaRuleGroup.PersonalHoroscopeRules.ToString(),
                ["planet"] = planet.ToString(),
                ["natalHouse"] = natalHouse.ToString()
            }
        };
    }

    private IEnumerable<MuhurthaRuleResult> EvaluateTransitActivation(
        MuhurthaRuleContext context,
        DateTimeOffset midpoint,
        IReadOnlyList<Planet> activePlanets)
    {
        var transitPlanets = _chartService.GenerateChart(
            midpoint.UtcDateTime,
            context.Request.Location.Latitude,
            context.Request.Location.Longitude);
        var aspectResult = _aspectService.CalculateGrahaDrishti(transitPlanets);
        var eventHouses = context.EventConfiguration.ImportantHouses;
        var activeTransitOccupants = transitPlanets
            .Where(position => activePlanets.Contains(position.Planet) && eventHouses.Contains((int)position.House))
            .Select(position => $"{position.Planet} in house {(int)position.House}")
            .ToList();
        var activeTransitAspects = aspectResult.HouseAspects
            .Where(aspect => activePlanets.Contains(aspect.FromPlanet) && eventHouses.Contains(aspect.ToHouse))
            .Select(aspect => $"{aspect.FromPlanet} aspects house {aspect.ToHouse}")
            .Distinct()
            .ToList();
        var moon = transitPlanets.FirstOrDefault(position => position.Planet == Planet.Moon);

        if (activeTransitOccupants.Count > 0)
        {
            yield return new MuhurthaRuleResult
            {
                RuleKey = "active_dasha_transit_event_house",
                Status = MuhurthaRuleStatus.Passed,
                Severity = MuhurthaRuleSeverity.Positive,
                ScoreImpact = 9,
                Summary = $"Active Dasha planet transit supports event houses: {string.Join(", ", activeTransitOccupants)}.",
                Recommendation = "This is a direct activation of the selected event houses.",
                Metadata =
                {
                    ["source"] = "ChartService",
                    ["group"] = MuhurthaRuleGroup.PersonalHoroscopeRules.ToString()
                }
            };
        }

        if (activeTransitAspects.Count > 0)
        {
            yield return new MuhurthaRuleResult
            {
                RuleKey = "active_dasha_aspects_event_house",
                Status = MuhurthaRuleStatus.Passed,
                Severity = MuhurthaRuleSeverity.Positive,
                ScoreImpact = 5,
                Summary = $"Active Dasha planets aspect event houses: {string.Join(", ", activeTransitAspects)}.",
                Recommendation = "This provides secondary event-house activation.",
                Metadata =
                {
                    ["source"] = "AspectService",
                    ["group"] = MuhurthaRuleGroup.PersonalHoroscopeRules.ToString()
                }
            };
        }

        if (moon is not null)
        {
            var moonInEventHouse = eventHouses.Contains((int)moon.House);
            yield return new MuhurthaRuleResult
            {
                RuleKey = "transit_moon_event_house_activation",
                Status = MuhurthaRuleStatus.Passed,
                Severity = moonInEventHouse ? MuhurthaRuleSeverity.Positive : MuhurthaRuleSeverity.Neutral,
                ScoreImpact = moonInEventHouse ? 6 : 0,
                Summary = moonInEventHouse
                    ? $"Transit Moon activates event house {(int)moon.House}."
                    : $"Transit Moon is in house {(int)moon.House}, outside the configured event houses.",
                Recommendation = moonInEventHouse
                    ? "This adds fast-moving Moon support for the selected event."
                    : "Use this together with Tara Bala and Chandra Bala.",
                Metadata =
                {
                    ["source"] = "ChartService",
                    ["group"] = MuhurthaRuleGroup.PersonalHoroscopeRules.ToString(),
                    ["moonHouse"] = ((int)moon.House).ToString()
                }
            };
        }
    }

    private static Planet CalculateCurrentPratyantardasha(AntardashaPeriod antardasha, DateTime currentDateUtc)
    {
        var order = DashaRules.VimshottariOrder;
        var startIndex = order.FindIndex(item => item.Planet == antardasha.SubPlanet);
        if (startIndex < 0 || antardasha.EndDate <= antardasha.StartDate)
        {
            return antardasha.SubPlanet;
        }

        var totalTicks = (antardasha.EndDate - antardasha.StartDate).Ticks;
        var elapsedTicks = Math.Clamp((currentDateUtc - antardasha.StartDate).Ticks, 0, totalTicks);
        var cursor = 0L;

        for (var i = 0; i < order.Count; i++)
        {
            var item = order[(startIndex + i) % order.Count];
            var durationTicks = (long)Math.Round(totalTicks * (item.Years / 120.0));
            cursor += durationTicks;
            if (elapsedTicks <= cursor)
            {
                return item.Planet;
            }
        }

        return order[(startIndex + order.Count - 1) % order.Count].Planet;
    }
    private MuhurthaRuleResult BuildWindowResult(
        MuhurthaRuleContext context,
        MuhurthaTimeWindow window,
        MuhurthaCandidateSlot candidate)
    {
        var config = context.RuleConfigurations.FirstOrDefault(rule =>
            string.Equals(rule.RuleKey, window.RuleKey, StringComparison.OrdinalIgnoreCase));
        var severity = config?.Severity ?? window.DefaultSeverity;
        var scoreImpact = config?.ScoreImpact == 0 ? window.DefaultScoreImpact : config?.ScoreImpact ?? window.DefaultScoreImpact;
        var hardRejected = config?.HardReject == true || severity == MuhurthaRuleSeverity.HardBlock;

        return new MuhurthaRuleResult
        {
            RuleKey = window.RuleKey,
            Status = window.IsPositive ? MuhurthaRuleStatus.Passed : MuhurthaRuleStatus.Warning,
            Severity = severity,
            ScoreImpact = scoreImpact,
            HardRejected = hardRejected,
            StartTime = Max(candidate.StartTime, window.StartTime),
            EndTime = Min(candidate.EndTime, window.EndTime),
            Summary = window.IsPositive
                ? $"The slot overlaps {window.DisplayName}, which can support an acceptable Muhurtha."
                : $"The slot overlaps {window.DisplayName}.",
            Recommendation = window.IsPositive
                ? "Use this as a secondary support after major defects are cleared."
                : $"Prefer a slot outside {window.DisplayName} where possible.",
            Metadata =
            {
                ["source"] = "GenericTimeWindowService",
                ["group"] = MuhurthaRuleGroup.GenericTimeRules.ToString()
            }
        };
    }

    private static MuhurthaRuleResult EvaluateTithiFactor(
        PanchangDetails panchang,
        string muhurthaTithi,
        List<string> positiveValues,
        List<string> avoidValues)
    {
        var result = EvaluateNamedFactor("tithi_suitability", "Tithi", muhurthaTithi, positiveValues, avoidValues);
        result.Metadata["maas"] = panchang.Maas;
        result.Metadata["shake"] = panchang.Shake;
        result.Metadata["samvat"] = panchang.Samvat;
        result.Metadata["vaar"] = panchang.Vaar;
        result.Metadata["paksha"] = panchang.Paksha;
        return result;
    }
    private static MuhurthaRuleResult EvaluateNamedFactor(
        string ruleKey,
        string label,
        string value,
        List<string> positiveValues,
        List<string> avoidValues)
    {
        var isPositive = positiveValues.Contains(value, StringComparer.OrdinalIgnoreCase);
        var isAvoid = string.Equals(value, "Amavasya", StringComparison.OrdinalIgnoreCase) || avoidValues.Contains(value, StringComparer.OrdinalIgnoreCase);

        return new MuhurthaRuleResult
        {
            RuleKey = ruleKey,
            Status = isAvoid ? MuhurthaRuleStatus.Warning : MuhurthaRuleStatus.Passed,
            Severity = isAvoid
                ? MuhurthaRuleSeverity.StrongAvoid
                : isPositive ? MuhurthaRuleSeverity.Positive : MuhurthaRuleSeverity.Neutral,
            ScoreImpact = isAvoid ? -10 : isPositive ? 7 : 0,
            Summary = $"{label} is {value}.",
            Recommendation = isAvoid
                ? $"Avoid this {label} for the selected event where possible."
                : isPositive ? $"This {label} supports the selected event." : $"This {label} is treated as neutral for the selected event.",
            Metadata =
            {
                ["source"] = "PanchangService",
                ["group"] = MuhurthaRuleGroup.PanchangaRules.ToString(),
                ["value"] = value
            }
        };
    }

    private static MuhurthaRuleResult EvaluateTaraBala(Nakshatra natalNakshatra, Nakshatra eventNakshatra)
    {
        var inclusiveCount = (((int)eventNakshatra - (int)natalNakshatra + 27) % 27) + 1;
        var taraPosition = inclusiveCount % 9;
        if (taraPosition == 0)
        {
            taraPosition = 9;
        }

        var (taraName, severity, scoreImpact) = taraPosition switch
        {
            2 => ("Sampat", MuhurthaRuleSeverity.Positive, 8),
            3 => ("Vipat", MuhurthaRuleSeverity.StrongAvoid, -12),
            4 => ("Kshema", MuhurthaRuleSeverity.Positive, 8),
            5 => ("Pratyari", MuhurthaRuleSeverity.StrongAvoid, -12),
            6 => ("Sadhana", MuhurthaRuleSeverity.Positive, 8),
            7 => ("Naidhana", MuhurthaRuleSeverity.HardBlock, -30),
            8 => ("Mitra", MuhurthaRuleSeverity.Positive, 8),
            9 => ("Parama Mitra", MuhurthaRuleSeverity.Positive, 12),
            _ => ("Janma", MuhurthaRuleSeverity.SoftAvoid, -5)
        };

        return new MuhurthaRuleResult
        {
            RuleKey = "tara_bala",
            Status = severity is MuhurthaRuleSeverity.Positive ? MuhurthaRuleStatus.Passed : MuhurthaRuleStatus.Warning,
            Severity = severity,
            ScoreImpact = scoreImpact,
            HardRejected = severity == MuhurthaRuleSeverity.HardBlock,
            Summary = $"Tara Bala is {taraName} from natal {natalNakshatra} to Muhurtha Moon {eventNakshatra}.",
            Recommendation = severity == MuhurthaRuleSeverity.HardBlock
                ? "Prefer another slot unless an expert override is appropriate."
                : "Use Tara Bala together with Chandra Bala and chart strength.",
            Metadata =
            {
                ["source"] = "NakshatraCalculator",
                ["group"] = MuhurthaRuleGroup.PersonalHoroscopeRules.ToString(),
                ["taraPosition"] = taraPosition.ToString()
            }
        };
    }

    private static MuhurthaRuleResult EvaluateChandraBala(ZodiacSign natalMoonSign, ZodiacSign eventMoonSign)
    {
        var position = (((int)eventMoonSign - (int)natalMoonSign + 12) % 12) + 1;
        var favourable = FavourableChandraPositions.Contains(position);
        var difficult = DifficultChandraPositions.Contains(position);

        return new MuhurthaRuleResult
        {
            RuleKey = "chandra_bala",
            Status = difficult ? MuhurthaRuleStatus.Warning : MuhurthaRuleStatus.Passed,
            Severity = favourable
                ? MuhurthaRuleSeverity.Positive
                : difficult ? MuhurthaRuleSeverity.StrongAvoid : MuhurthaRuleSeverity.Neutral,
            ScoreImpact = favourable ? 10 : difficult ? -12 : 0,
            Summary = $"Muhurtha Moon is in the {position} position from natal Moon.",
            Recommendation = difficult
                ? "Prefer a slot where Moon is not in a difficult Chandra Bala position."
                : "Chandra Bala is acceptable for this slot.",
            Metadata =
            {
                ["source"] = "ChartService",
                ["group"] = MuhurthaRuleGroup.PersonalHoroscopeRules.ToString(),
                ["position"] = position.ToString()
            }
        };
    }

    private bool TryBuildNatalChart(
        MuhurthaHoroscopeReference? horoscope,
        TimeSpan fallbackOffset,
        out List<PlanetPosition> natalPlanets,
        out DateTime birthUtc)
    {
        natalPlanets = new List<PlanetPosition>();
        birthUtc = DateTime.MinValue;
        if (horoscope?.BirthDateTime is null || horoscope.Latitude is null || horoscope.Longitude is null)
        {
            return false;
        }

        birthUtc = DateTime.SpecifyKind(horoscope.BirthDateTime.Value - fallbackOffset, DateTimeKind.Utc);
        natalPlanets = _chartService.GenerateChart(birthUtc, horoscope.Latitude.Value, horoscope.Longitude.Value);
        return true;
    }
    private static MuhurthaRuleResult BuildSkipped(
        string ruleKey,
        string summary,
        MuhurthaRuleGroup group)
    {
        return new MuhurthaRuleResult
        {
            RuleKey = ruleKey,
            Status = MuhurthaRuleStatus.Skipped,
            Severity = MuhurthaRuleSeverity.Neutral,
            Summary = summary,
            Recommendation = "Provide the required source data or configuration to evaluate this rule.",
            Metadata =
            {
                ["source"] = "MuhurthaRuleEvaluationService",
                ["group"] = group.ToString()
            }
        };
    }

    private static bool Overlaps(DateTimeOffset aStart, DateTimeOffset aEnd, DateTimeOffset bStart, DateTimeOffset bEnd)
    {
        return aStart < bEnd && bStart < aEnd;
    }

    private static DateTimeOffset Max(DateTimeOffset a, DateTimeOffset b) => a > b ? a : b;

    private static DateTimeOffset Min(DateTimeOffset a, DateTimeOffset b) => a < b ? a : b;
}














