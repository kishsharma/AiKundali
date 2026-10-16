using KundaliEngine.API.DTOs.Requests;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Dasha;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.Astrology.Services.Scoring;
using KundaliEngine.Domain.Entities;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Services.Horoscope;

public interface IHoroscopePeriodService
{
    Task<HoroscopePeriodResponse> GenerateAsync(
        HoroscopePeriodRequest request,
        Guid? userId,
        CancellationToken cancellationToken = default);
}

public sealed class HoroscopePeriodService : IHoroscopePeriodService
{
    private readonly KundaliDbContext _dbContext;
    private readonly ChartService _chartService;
    private readonly DashaService _dashaService;
    private readonly PanchangService _panchangService;
    private readonly IEphemerisProvider _ephemerisProvider;
    private readonly AspectService _aspectService;
    private readonly HoroscopeNatalScorer _natalScorer;
    private readonly HoroscopeHorizonExtrasBuilder _horizonExtrasBuilder;
    private readonly ILogger<HoroscopePeriodService> _logger;

    public HoroscopePeriodService(
        KundaliDbContext dbContext,
        ChartService chartService,
        DashaService dashaService,
        PanchangService panchangService,
        IEphemerisProvider ephemerisProvider,
        AspectService aspectService,
        HoroscopeNatalScorer natalScorer,
        HoroscopeHorizonExtrasBuilder horizonExtrasBuilder,
        ILogger<HoroscopePeriodService> logger)
    {
        _dbContext = dbContext;
        _chartService = chartService;
        _dashaService = dashaService;
        _panchangService = panchangService;
        _ephemerisProvider = ephemerisProvider;
        _aspectService = aspectService;
        _natalScorer = natalScorer;
        _horizonExtrasBuilder = horizonExtrasBuilder;
        _logger = logger;
    }

    public async Task<HoroscopePeriodResponse> GenerateAsync(
        HoroscopePeriodRequest request,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        Validate(request);
        var kundali = await LoadKundaliAsync(request.KundaliId, userId, cancellationToken);
        var weights = HoroscopeFactorMatrix.ForHorizon(request.Horizon);
        var (from, to) = ResolvePeriod(request.Horizon, request.AnchorDate);

        var offset = ParseOffset(request.Timezone)
            ?? ParseOffset(kundali.TimeZone)
            ?? TimeSpan.FromHours(5.5);
        var analysisLocal = request.AnchorDate.ToDateTime(new TimeOnly(12, 0));
        var analysisUtc = new DateTimeOffset(
            DateTime.SpecifyKind(analysisLocal, DateTimeKind.Unspecified),
            offset).UtcDateTime;

        var latitude = request.Latitude ?? Convert.ToDouble(kundali.Latitude!.Value);
        var longitude = request.Longitude ?? Convert.ToDouble(kundali.Longitude!.Value);
        var birthUtc = ResolveBirthUtc(kundali);

        var natalPlanets = _chartService.GenerateChart(birthUtc, latitude, longitude);
        var natalMoon = natalPlanets.FirstOrDefault(p => p.Planet == Planet.Moon)
            ?? throw new InvalidOperationException("Natal Moon could not be calculated.");
        var natalAscendant = CalculateAscendantSign(birthUtc, latitude, longitude);
        var birthDasha = _dashaService.GetBirthMahadasha(natalMoon);
        var dasha = _dashaService.CalculateCurrentDasha(
            birthDasha.Planet,
            birthDasha.RemainingYears,
            birthUtc,
            analysisUtc);

        var transitRaw = _chartService.GenerateChart(analysisUtc, latitude, longitude);
        var transitFromLagna = RehouseFromReference(transitRaw, natalAscendant);
        var transitFromMoon = RehouseFromReference(transitRaw, natalMoon.Sign);

        var gochara = HoroscopeGocharaScorer.Score(
            transitFromLagna,
            transitFromMoon,
            dasha.CurrentMahadasha.Planet,
            dasha.CurrentAntardasha.SubPlanet,
            weights);
        HoroscopeAspectScorer.AppendAspectHits(
            gochara,
            _aspectService,
            transitFromLagna,
            transitFromMoon,
            dasha.CurrentMahadasha.Planet,
            dasha.CurrentAntardasha.SubPlanet);
        HoroscopeAspectScorer.RecalculateOverall(gochara);

        var sun = transitRaw.FirstOrDefault(p => p.Planet == Planet.Sun);
        var moon = transitRaw.FirstOrDefault(p => p.Planet == Planet.Moon);
        var panchangaDetails = _panchangService.Calculate(
            analysisLocal,
            sun?.Longitude ?? 0,
            moon?.Longitude ?? natalMoon.Longitude);
        var panchanga = HoroscopePanchangaScorer.Score(panchangaDetails, natalMoon.Nakshatra, weights);

        var dashaTone = ScoreDashaTone(dasha, natalPlanets);
        var natalScore = _natalScorer.Score(
            natalPlanets,
            natalAscendant,
            dasha.CurrentMahadasha.Planet,
            dasha.CurrentAntardasha.SubPlanet);

        var overallScore = Math.Clamp((int)Math.Round(
            natalScore.Score * weights.Natal
            + dashaTone.Score * weights.Dasha
            + gochara.Score * weights.Gochara
            + panchanga.Score * weights.Panchanga), 0, 100);

        var proposedTone = overallScore >= 62 ? "Supportive" : overallScore <= 42 ? "Challenging" : "Mixed";
        var overallTone = HoroscopeTithiPraveshaGuard.ApplyHierarchicalToneLimit(proposedTone, dashaTone.Tone);

        var horizonExtras = _horizonExtrasBuilder.Build(
            request.Horizon,
            from,
            to,
            analysisLocal,
            offset,
            latitude,
            longitude,
            panchanga.Maas,
            dasha);

        var areas = BuildAreas(
            dasha.CurrentMahadasha.Planet,
            dasha.CurrentAntardasha.SubPlanet,
            gochara,
            panchanga,
            natalScore,
            overallTone,
            weights,
            request.Horizon);

        var summaryFacts = new List<string>
        {
            $"Running {dasha.CurrentMahadasha.Planet}–{dasha.CurrentAntardasha.SubPlanet} Vimshottari.",
            $"Moon tara: {panchanga.TaraName} ({panchanga.TaraTone}).",
            gochara.Facts.FirstOrDefault() ?? "Gochara is mixed from Lagna and Moon.",
            $"{panchanga.Paksha} {panchanga.Tithi}, {panchanga.Vaar}."
        };

        if (natalScore.ActiveFormations.Count > 0)
        {
            summaryFacts.Insert(1, natalScore.ActiveFormations[0].Fact);
        }

        if (weights.UsePratyantarSoft
            && dasha.CurrentPratyantardasha?.InnerPlanet != default)
        {
            summaryFacts.Insert(1,
                $"Soft day corroboration: {dasha.CurrentPratyantardasha.InnerPlanet} pratyantardasha (birth-time sensitive).");
        }

        foreach (var extra in horizonExtras.Facts.Take(2))
        {
            if (summaryFacts.Count < 5)
            {
                summaryFacts.Add(extra);
            }
        }

        var response = new HoroscopePeriodResponse
        {
            Horizon = request.Horizon,
            From = from,
            To = to,
            OverallTone = overallTone,
            OverallScore = overallScore,
            SummaryFacts = summaryFacts.Where(f => !string.IsNullOrWhiteSpace(f)).Distinct().Take(5).ToList(),
            Areas = areas,
            Panchanga = MapPanchanga(panchanga),
            GocharaHits = gochara.Hits.Select(h => new HoroscopeGocharaHitDto
            {
                Planet = h.Planet,
                Reference = h.Reference,
                House = h.House,
                Sign = h.Sign,
                Tone = h.Tone,
                Fact = h.Fact
            }).ToList(),
            DashaContext = new HoroscopeDashaContextDto
            {
                Mahadasha = dasha.CurrentMahadasha.Planet.ToString(),
                Antardasha = dasha.CurrentAntardasha.SubPlanet.ToString(),
                Pratyantardasha = weights.UsePratyantarSoft
                    && dasha.CurrentPratyantardasha?.InnerPlanet != default
                    ? dasha.CurrentPratyantardasha.InnerPlanet.ToString()
                    : null,
                MahadashaStartUtc = dasha.CurrentMahadasha.StartDate,
                MahadashaEndUtc = dasha.CurrentMahadasha.EndDate,
                AntardashaStartUtc = dasha.CurrentAntardasha.StartDate,
                AntardashaEndUtc = dasha.CurrentAntardasha.EndDate,
                Facts = dashaTone.Facts
            },
            ActiveFormations = natalScore.ActiveFormations.Select(f => new HoroscopeActiveFormationDto
            {
                Type = f.Type,
                Key = f.Key,
                Name = f.Name,
                Tone = f.Tone,
                Fact = f.Fact
            }).ToList(),
            HorizonExtras = horizonExtras,
            TithiPravesha = HoroscopeTithiPraveshaGuard.BuildStatus(request.Horizon),
            Limitations = HoroscopeTithiPraveshaGuard.BuildLimitations(request.Horizon).ToList()
        };

        _logger.LogInformation(
            "HoroscopePeriodGenerated horizon={Horizon} kundaliId={KundaliId} userId={UserId} tone={Tone} score={Score}",
            request.Horizon,
            kundali.Id,
            userId,
            overallTone,
            overallScore);

        return response;
    }

    private static List<HoroscopeAreaDto> BuildAreas(
        Planet mahadasha,
        Planet antardasha,
        HoroscopeGocharaScore gochara,
        HoroscopePanchangaScore panchanga,
        HoroscopeNatalScore natal,
        string overallTone,
        HoroscopeHorizonWeights weights,
        HoroscopeHorizon horizon)
    {
        var areas = new List<HoroscopeAreaDto>();

        foreach (var definition in HoroscopeFactorMatrix.Areas)
        {
            var facts = new List<string>();
            var support = 0;
            var pressure = 0;

            foreach (var house in definition.Houses)
            {
                foreach (var hit in gochara.Hits.Where(h => h.House == house))
                {
                    facts.Add(hit.Fact);
                    if (hit.Tone == "Supportive")
                    {
                        support += 8;
                    }
                    else if (hit.Tone == "Challenging")
                    {
                        pressure += 8;
                    }
                }

                if (horizon == HoroscopeHorizon.Monthly)
                {
                    var houseScore = natal.HouseScores
                        .FirstOrDefault(h => (int)h.HouseNumber == house)?.Score ?? 50;
                    if (houseScore >= 60)
                    {
                        support += 4;
                        facts.Add($"Natal house {house} strength {houseScore} supports {definition.Title.ToLowerInvariant()} (AV-style).");
                    }
                    else if (houseScore <= 40)
                    {
                        pressure += 4;
                        facts.Add($"Natal house {house} strength {houseScore} weakens {definition.Title.ToLowerInvariant()} (AV-style).");
                    }
                }
            }

            if (definition.Karakas.Any(k => string.Equals(k, mahadasha.ToString(), StringComparison.OrdinalIgnoreCase)))
            {
                support += 6;
                facts.Add($"{mahadasha} Mahadasha is a karaka for {definition.Title.ToLowerInvariant()}.");
            }

            if (definition.Karakas.Any(k => string.Equals(k, antardasha.ToString(), StringComparison.OrdinalIgnoreCase)))
            {
                support += 5;
                facts.Add($"{antardasha} Antardasha colours {definition.Title.ToLowerInvariant()} this period.");
            }

            foreach (var formation in natal.ActiveFormations.Take(2))
            {
                if (formation.Tone == "Supportive")
                {
                    support += 3;
                }
                else if (formation.Tone == "Challenging")
                {
                    pressure += 3;
                }
            }

            if (definition.Key is "mind" or "health" && panchanga.TaraTone == "Challenging")
            {
                pressure += (int)Math.Round(8 * (0.5 + weights.Panchanga));
                facts.Add($"Moon tara {panchanga.TaraName} adds caution for {definition.Title.ToLowerInvariant()}.");
            }

            if (definition.Key is "money" or "relations" && panchanga.TaraTone == "Supportive")
            {
                support += (int)Math.Round(6 * (0.5 + weights.Panchanga));
            }

            var delta = support - pressure;
            var score = Math.Clamp(50 + delta, 0, 100);
            var tone = score >= 60 ? "Supportive" : score <= 40 ? "Challenging" : "Mixed";
            tone = HoroscopeTithiPraveshaGuard.ApplyHierarchicalToneLimit(
                tone,
                overallTone == "Challenging" ? "Challenging" : "Mixed");

            if (facts.Count == 0)
            {
                facts.Add($"No sharp gochara hit on houses {string.Join("/", definition.Houses)} — tone follows overall period.");
                tone = overallTone;
                score = Math.Clamp(overallTone == "Supportive" ? 58 : overallTone == "Challenging" ? 42 : 50, 0, 100);
            }

            var area = new HoroscopeAreaDto
            {
                Key = definition.Key,
                Title = definition.Title,
                Score = score,
                Tone = tone,
                Facts = facts.Distinct(StringComparer.OrdinalIgnoreCase).Take(5).ToList(),
                Prediction = BuildLaymanPrediction(definition.Key, tone)
            };

            if (tone == "Supportive")
            {
                area.Do.Add(BuildDoTip(definition.Key));
            }
            else if (tone == "Challenging")
            {
                area.Caution.Add(BuildCautionTip(definition.Key));
                area.Avoid.Add(BuildAvoidTip(definition.Key));
            }

            areas.Add(area);
        }

        // Keep catalog order (health → money → … → career) so rows don't reshuffle per chart.
        return areas;
    }

    private static string BuildLaymanPrediction(string areaKey, string tone)
    {
        var supportive = tone.Equals("Supportive", StringComparison.OrdinalIgnoreCase);
        var challenging = tone.Equals("Challenging", StringComparison.OrdinalIgnoreCase);

        return areaKey switch
        {
            "health" when supportive => "Energy and recovery look favourable — keep daily routines steady and rest well.",
            "health" when challenging => "Body and energy need extra care — slow down and avoid overstrain.",
            "health" => "Vitality feels mixed — balance activity with proper rest.",

            "money" when supportive => "Money matters look steadier — a good window for careful planning and saving.",
            "money" when challenging => "Cash flow may feel tight or delayed — spend only on essentials for now.",
            "money" => "Finances are uneven — track spending and avoid big commitments.",

            "home" when supportive => "Home and family atmosphere can feel calmer — a good time to settle small matters.",
            "home" when challenging => "Home peace may feel strained — keep conversations soft and expect delays.",
            "home" => "Domestic life is average — small adjustments help more than big changes.",

            "mind" when supportive => "Thinking and learning feel clearer — good for study, ideas, and planning.",
            "mind" when challenging => "Focus may scatter — avoid rushed decisions and give yourself quiet time.",
            "mind" => "Mental energy is mixed — short focused work beats long stretches.",

            "relations" when supportive => "Relationships can warm up — a good time to reconnect and clear small issues.",
            "relations" when challenging => "Partnerships need patience — pause before reacting or making promises.",
            "relations" => "Relationships are steady but sensitive — listen more than you argue.",

            "career" when supportive => "Work progress looks supportive — push key tasks and ask for visibility.",
            "career" when challenging => "Career pressure may rise — deliver carefully and avoid office conflicts.",
            "career" => "Work pace is mixed — finish priorities before taking on new ones.",

            _ when supportive => "This area looks favourable — lean into constructive next steps.",
            _ when challenging => "This area needs care — go slow and reduce unnecessary risk.",
            _ => "This area is mixed — stay practical and review results as the period unfolds."
        };
    }

    private static string BuildDoTip(string areaKey) => areaKey switch
    {
        "health" => "Keep sleep, water, and light movement consistent.",
        "money" => "Review budget and clear small dues early.",
        "home" => "Spend quality time at home and tidy unfinished chores.",
        "mind" => "Block focused time for study or creative work.",
        "relations" => "Reach out kindly and clear misunderstandings early.",
        "career" => "Complete one high-impact task and share progress.",
        _ => "Take one constructive step while support is present."
    };

    private static string BuildCautionTip(string areaKey) => areaKey switch
    {
        "health" => "Do not ignore fatigue or skip meals.",
        "money" => "Avoid impulsive purchases and new loans.",
        "home" => "Avoid heated family arguments over small issues.",
        "mind" => "Avoid overthinking late at night.",
        "relations" => "Avoid sharp words or sudden break-ups.",
        "career" => "Avoid confrontations and last-minute risks at work.",
        _ => "Reduce haste and protect your energy."
    };

    private static string BuildAvoidTip(string areaKey) => areaKey switch
    {
        "health" => "Avoid forcing heavy workouts or late nights.",
        "money" => "Avoid speculative bets and large unplanned spending.",
        "home" => "Avoid relocating or major home decisions in a rush.",
        "mind" => "Avoid multitasking when decisions matter.",
        "relations" => "Avoid forcing outcomes in partnerships.",
        "career" => "Avoid job changes or confrontations without a plan.",
        _ => "Avoid forcing outcomes until pressure eases."
    };

    private static (int Score, string Tone, List<string> Facts) ScoreDashaTone(
        DashaCalculationResult dasha,
        IReadOnlyList<PlanetPosition> natalPlanets)
    {
        var facts = new List<string>
        {
            $"{dasha.CurrentMahadasha.Planet} Mahadasha until {dasha.CurrentMahadasha.EndDate:yyyy-MM-dd}.",
            $"{dasha.CurrentAntardasha.SubPlanet} Antardasha until {dasha.CurrentAntardasha.EndDate:yyyy-MM-dd}."
        };

        var maha = natalPlanets.FirstOrDefault(p => p.Planet == dasha.CurrentMahadasha.Planet);
        var antar = natalPlanets.FirstOrDefault(p => p.Planet == dasha.CurrentAntardasha.SubPlanet);
        var support = 0;
        var pressure = 0;

        void Judge(PlanetPosition? position, string label)
        {
            if (position == null)
            {
                return;
            }

            var house = (int)position.House;
            if (house is 1 or 5 or 9 or 10 or 11)
            {
                support += label.StartsWith("Maha", StringComparison.Ordinal) ? 12 : 10;
                facts.Add($"{label} occupies house {house} natally — constructive seat.");
            }
            else if (house is 6 or 8 or 12)
            {
                pressure += label.StartsWith("Maha", StringComparison.Ordinal) ? 12 : 10;
                facts.Add($"{label} occupies house {house} natally — dusthana pressure.");
            }
        }

        Judge(maha, "Mahadasha lord");
        Judge(antar, "Antardasha lord");

        var delta = support - pressure;
        var tone = delta >= 8 ? "Supportive" : delta <= -8 ? "Challenging" : "Mixed";
        var score = Math.Clamp(50 + delta, 0, 100);
        return (score, tone, facts);
    }

    private static HoroscopePanchangaDto MapPanchanga(HoroscopePanchangaScore score) => new()
    {
        Tithi = score.Tithi,
        Paksha = score.Paksha,
        Nakshatra = score.Nakshatra,
        Pada = score.Pada,
        Yoga = score.Yoga,
        Karana = score.Karana,
        Vaar = score.Vaar,
        Maas = score.Maas,
        TaraNumber = score.TaraNumber,
        TaraName = score.TaraName,
        TaraTone = score.TaraTone,
        Score = score.Score,
        Facts = score.Facts
    };

    private static (DateOnly From, DateOnly To) ResolvePeriod(HoroscopeHorizon horizon, DateOnly anchor) =>
        horizon switch
        {
            HoroscopeHorizon.Weekly => (StartOfWeek(anchor), StartOfWeek(anchor).AddDays(6)),
            HoroscopeHorizon.Monthly => (new DateOnly(anchor.Year, anchor.Month, 1),
                new DateOnly(anchor.Year, anchor.Month, 1).AddMonths(1).AddDays(-1)),
            _ => (anchor, anchor)
        };

    private static DateOnly StartOfWeek(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-diff);
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

    private static void Validate(HoroscopePeriodRequest request)
    {
        if (request.KundaliId == Guid.Empty)
        {
            throw new ArgumentException("kundaliId is required.");
        }

        if (request.AnchorDate == default)
        {
            throw new ArgumentException("anchorDate is required.");
        }
    }

    private ZodiacSign CalculateAscendantSign(DateTime birthUtc, double latitude, double longitude)
    {
        var ascendantLongitude = _ephemerisProvider.GetAscendant(birthUtc, new GeoLocation(latitude, longitude)).Longitude;
        var ascendantIndex = (int)(NormalizeLongitude(ascendantLongitude) / 30.0);
        return (ZodiacSign)((ascendantIndex % 12) + 1);
    }

    internal static List<PlanetPosition> RehouseFromReference(IEnumerable<PlanetPosition> source, ZodiacSign referenceSign)
    {
        return source.Select(position => new PlanetPosition
        {
            Planet = position.Planet,
            PlanetName = position.PlanetName,
            Longitude = position.Longitude,
            Latitude = position.Latitude,
            Sign = position.Sign,
            DegreeInSign = position.DegreeInSign,
            Degree = position.Degree,
            Minute = position.Minute,
            Nakshatra = position.Nakshatra,
            Pada = position.Pada,
            House = (HouseNumber)((((int)position.Sign - (int)referenceSign + 12) % 12) + 1),
            IsRetrograde = position.IsRetrograde,
            DetailedStrength = position.DetailedStrength,
            IsCombust = position.IsCombust
        }).ToList();
    }

    private static DateTime ResolveBirthUtc(Kundali kundali)
    {
        var birthLocal = kundali.DateOfBirth.ToDateTime(kundali.TimeOfBirth);
        var offset = ParseOffset(kundali.TimeZone) ?? TimeSpan.FromHours(5.5);
        return new DateTimeOffset(DateTime.SpecifyKind(birthLocal, DateTimeKind.Unspecified), offset).UtcDateTime;
    }

    internal static TimeSpan? ParseOffset(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("UTC", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return TimeSpan.TryParse(value[3..], out var offset) ? offset : null;
    }

    private static double NormalizeLongitude(double longitude)
    {
        var value = longitude % 360.0;
        return value < 0 ? value + 360.0 : value;
    }
}
