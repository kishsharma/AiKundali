using KundaliEngine.API.DTOs.Requests;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Dasha;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Services.Prediction;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.Domain.Rules;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace KundaliEngine.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashaController : ControllerBase
    {
        private readonly DashaService _dashaService;
        private readonly ChartService _chartService;
        private readonly PlanetStrengthService _planetStrengthService;
        private readonly PanchangService _panchangService;
        private readonly IEphemerisProvider _ephemerisProvider;
        private readonly PredictionService _predictionService = new();

        public DashaController(DashaService dashaService, ChartService chartService, PlanetStrengthService planetStrengthService,
            PanchangService panchangService,IEphemerisProvider ephemerisProvider)
        {
            _dashaService = dashaService;
            _chartService = chartService;
            _planetStrengthService = planetStrengthService;
            _panchangService = panchangService;
            _ephemerisProvider = ephemerisProvider;
        }

        [HttpPost("mahadasha-periods")]
        public ActionResult<List<MahadashaPeriodDto>> GetMahadashaPeriodsTimeline([FromBody] GenerateKundaliRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            try
            {
                var indiaZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                DateTime birthUtc = TimeZoneInfo.ConvertTimeToUtc(request.BirthDateTime, indiaZone);

                var planets = _chartService.GenerateChart(birthUtc, request.Latitude, request.Longitude);
                var moonPos = planets.First(p => p.Planet == Domain.Enums.Planet.Moon);
                var birthDasha = _dashaService.GetBirthMahadasha(moonPos);

                var mahadashaTimeline = _dashaService.GenerateMahadashaTimelineForYears(
                    birthDasha.Planet,
                    birthDasha.RemainingYears,
                    birthUtc,
                    120);

                // Build mahadasha + antardasha DTOs
                var mahadashaDtos = mahadashaTimeline.Select(m => new MahadashaPeriodDto
                {
                    Planet = m.Planet.ToString(),
                    Start = m.StartDate,
                    End = m.EndDate,
                    Antardashas = _dashaService.GenerateAntardashaForTimelinePeriod(
                            m,
                            birthDasha.Planet,
                            birthDasha.RemainingYears)
                        .Select(a => BuildAntardashaPeriodDto(m, a, birthDasha.Planet, birthDasha.RemainingYears))
                        .ToList()
                }).ToList();

                return Ok(mahadashaDtos);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating mahadasha periods: {ex.Message}");
            }
        }

        [HttpPost("mahadasha-analysis")]
        public ActionResult<MahadashaAnalysisDto> GetMahadashaAnalysis([FromBody] GenerateKundaliRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            try
            {
                var indiaZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                DateTime birthUtc = TimeZoneInfo.ConvertTimeToUtc(request.BirthDateTime, indiaZone);
                DateTime nowUtc = DateTime.UtcNow;

                var planets = _chartService.GenerateChart(birthUtc, request.Latitude, request.Longitude);
                var moonPos = planets.First(p => p.Planet == Planet.Moon);
                var sunPos = planets.First(p => p.Planet == Planet.Sun);
                var panchang = _panchangService.Calculate(birthUtc, sunPos.Longitude, moonPos.Longitude);
                var ascendant = CalculateAscendantSign(birthUtc, request.Latitude, request.Longitude);
                var birthDasha = _dashaService.GetBirthMahadasha(moonPos);

                var mahadashaTimeline = _dashaService.GenerateMahadashaTimelineForYears(
                    birthDasha.Planet,
                    birthDasha.RemainingYears,
                    birthUtc,
                    120);

                var currentMaha = mahadashaTimeline
                    .FirstOrDefault(m => nowUtc >= m.StartDate && nowUtc < m.EndDate)
                    ?? mahadashaTimeline.Last();

                var antardashaTimeline = _dashaService.GenerateAntardashaForTimelinePeriod(
                    currentMaha,
                    birthDasha.Planet,
                    birthDasha.RemainingYears);

                var currentAntar = antardashaTimeline
                    .FirstOrDefault(a => nowUtc >= a.StartDate && nowUtc < a.EndDate)
                    ?? antardashaTimeline.First();

                var pratyantardashaTimeline = _dashaService.GeneratePratyantardashaForTimelinePeriod(
                    currentMaha,
                    currentAntar,
                    birthDasha.Planet,
                    birthDasha.RemainingYears);

                var currentPratyantar = pratyantardashaTimeline
                    .FirstOrDefault(p => nowUtc >= p.StartDate && nowUtc < p.EndDate)
                    ?? pratyantardashaTimeline.FirstOrDefault();

                int currentMahaIndex = mahadashaTimeline.FindIndex(m => m == currentMaha);
                int currentAntarIndex = antardashaTimeline.FindIndex(a => a == currentAntar);
                int currentPratyantarIndex = currentPratyantar == null
                    ? 0
                    : Math.Max(0, pratyantardashaTimeline.FindIndex(p => p == currentPratyantar));
                var mahaPosition = planets.FirstOrDefault(p => p.Planet == currentMaha.Planet);
                var strength = mahaPosition == null
                    ? PlanetStrength.Neutral
                    : _planetStrengthService.GetStrength(mahaPosition.Planet, mahaPosition.Sign);
                var mahadashaStrength = BuildDashaLevelStrength("Mahadasha", currentMaha.Planet, planets);
                var antardashaStrength = BuildDashaLevelStrength("Antardasha", currentAntar.SubPlanet, planets);
                var pratyantardashaStrength = currentPratyantar == null
                    ? new DashaLevelStrengthDto { Level = "Pratyantardasha" }
                    : BuildDashaLevelStrength("Pratyantardasha", currentPratyantar.InnerPlanet, planets);

                int overallStrength = mahadashaStrength.OverallStrength;
                string overallResult = mahadashaStrength.OverallResult;
                var upcomingMaha = mahadashaTimeline.Skip(currentMahaIndex + 1).FirstOrDefault();
                var factLords = new List<Planet> { currentMaha.Planet, currentAntar.SubPlanet };
                if (currentPratyantar != null)
                {
                    factLords.Add(currentPratyantar.InnerPlanet);
                }

                var analysis = new MahadashaAnalysisDto
                {
                    Summary = new MahadashaSummaryDto
                    {
                        OverallScore = overallStrength,
                        OverallRating = Math.Max(1, (int)Math.Round(overallStrength / 20.0)),
                        OverallResult = overallResult,
                        CurrentMahadasha = currentMaha.Planet.ToString(),
                        CurrentAntardasha = currentAntar.SubPlanet.ToString(),
                        CurrentPratyantardasha = currentPratyantar?.InnerPlanet.ToString() ?? string.Empty,
                        RemainingDuration = FormatDuration(nowUtc, currentAntar.EndDate),
                        PratyantardashaRemainingDuration = currentPratyantar == null ? string.Empty : FormatDuration(nowUtc, currentPratyantar.EndDate),
                        CompletedPercentage = CompletedPercent(currentMaha.StartDate, currentMaha.EndDate, nowUtc),
                        StartDate = currentMaha.StartDate,
                        EndDate = currentMaha.EndDate
                    },
                    BirthChart = new BirthChartSummaryDto
                    {
                        Name = request.Name ?? string.Empty,
                        Gender = request.Gender ?? string.Empty,
                        BirthDateTime = request.BirthDateTime,
                        BirthPlace = request.BirthPlace ?? string.Empty,
                        Latitude = request.Latitude,
                        Longitude = request.Longitude,
                        Lagna = $"{(int)ascendant} - {ascendant}",
                        ChandraRashi = moonPos.Sign.ToString(),
                        SuryaRashi = sunPos.Sign.ToString(),
                        Nakshatra = panchang.Nakshatra,
                        Pada = panchang.Pada,
                        DashaBelongsTo = string.IsNullOrWhiteSpace(request.Name) ? "this native" : request.Name.Trim()
                    },
                    CurrentMahadasha = new CurrentMahadashaDto
                    {
                        Planet = currentMaha.Planet.ToString(),
                        PlanetIcon = currentMaha.Planet.ToString().ToLowerInvariant(),
                        PlanetColor = PlanetColor(currentMaha.Planet),
                        Nature = PlanetNature(currentMaha.Planet),
                        DurationYears = DashaYears(currentMaha.Planet),
                        StartDate = currentMaha.StartDate,
                        EndDate = currentMaha.EndDate,
                        CurrentYear = Math.Max(1, nowUtc.Year - currentMaha.StartDate.Year + 1),
                        Description = $"{currentMaha.Planet} Mahadasha {_predictionService.GetDashaPrediction(currentMaha.Planet)}",
                        Keywords = PlanetKeywords(currentMaha.Planet)
                    },
                    CurrentAntardasha = new CurrentAntardashaDto
                    {
                        Planet = currentAntar.SubPlanet.ToString(),
                        PlanetIcon = currentAntar.SubPlanet.ToString().ToLowerInvariant(),
                        PlanetColor = PlanetColor(currentAntar.SubPlanet),
                        Nature = PlanetNature(currentAntar.SubPlanet),
                        StartDate = currentAntar.StartDate,
                        EndDate = currentAntar.EndDate,
                        Description = $"{currentAntar.SubPlanet} Antardasha {_predictionService.GetDashaPrediction(currentAntar.SubPlanet)}",
                        CombinedEffect = _predictionService.GetCombinedDashaPrediction(currentMaha.Planet, currentAntar.SubPlanet)
                    },
                    CurrentPratyantardasha = currentPratyantar == null
                        ? new CurrentPratyantardashaDto()
                        : new CurrentPratyantardashaDto
                        {
                            Planet = currentPratyantar.InnerPlanet.ToString(),
                            PlanetIcon = currentPratyantar.InnerPlanet.ToString().ToLowerInvariant(),
                            PlanetColor = PlanetColor(currentPratyantar.InnerPlanet),
                            Nature = PlanetNature(currentPratyantar.InnerPlanet),
                            StartDate = currentPratyantar.StartDate,
                            EndDate = currentPratyantar.EndDate,
                            RemainingDuration = FormatDuration(nowUtc, currentPratyantar.EndDate)
                        },
                    Timeline = new DashaTimelineDto
                    {
                        CurrentMahadashaIndex = currentMahaIndex,
                        CurrentAntardashaIndex = currentAntarIndex,
                        CurrentPratyantardashaIndex = currentPratyantarIndex,
                        Mahadasha = mahadashaTimeline
                            .Select(m => new MahadashaTimelinePeriodDto
                            {
                                Planet = m.Planet.ToString(),
                                Start = m.StartDate.ToString("yyyy", CultureInfo.InvariantCulture),
                                End = m.EndDate.ToString("yyyy", CultureInfo.InvariantCulture),
                                Status = PeriodStatus(m.StartDate, m.EndDate, nowUtc),
                                Antardashas = _dashaService.GenerateAntardashaForTimelinePeriod(
                                        m,
                                        birthDasha.Planet,
                                        birthDasha.RemainingYears)
                                    .Select(a => BuildAntardashaTimelinePeriodDto(
                                        m,
                                        a,
                                        birthDasha.Planet,
                                        birthDasha.RemainingYears,
                                        nowUtc))
                                    .ToList()
                            })
                            .ToList(),
                        Antardasha = antardashaTimeline
                            .Select(a => BuildAntardashaTimelinePeriodDto(
                                currentMaha,
                                a,
                                birthDasha.Planet,
                                birthDasha.RemainingYears,
                                nowUtc))
                            .ToList(),
                        Pratyantardasha = pratyantardashaTimeline
                            .Select(p => new PratyantardashaTimelinePeriodDto
                            {
                                Planet = p.InnerPlanet.ToString(),
                                Start = p.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                                End = p.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                                Status = PeriodStatus(p.StartDate, p.EndDate, nowUtc)
                            })
                            .ToList()
                    },
                    LifeAreas = BuildLifeAreas(planets, ascendant, currentMaha.Planet, currentAntar.SubPlanet),
                    Strength = new DashaStrengthDto
                    {
                        OverallStrength = overallStrength,
                        Planet = currentMaha.Planet.ToString(),
                        PlanetStrength = StrengthLabel(strength),
                        Placement = mahaPosition == null ? "Chart placement unavailable" : FormatHousePlacement(mahaPosition.House),
                        Sign = mahaPosition?.Sign.ToString() ?? string.Empty,
                        AspectOnLagna = mahaPosition?.House == HouseNumber.Fifth || mahaPosition?.House == HouseNumber.Seventh || mahaPosition?.House == HouseNumber.Ninth,
                        SupportedByBenefics = mahadashaStrength.SupportedByBenefics,
                        SupportedBy = mahadashaStrength.SupportedBy,
                        AssociationTone = mahadashaStrength.AssociationTone,
                        AssociationSummary = mahadashaStrength.AssociationSummary,
                        AssociationDetails = mahadashaStrength.AssociationDetails,
                        OverallResult = overallResult,
                        Mahadasha = mahadashaStrength,
                        Antardasha = antardashaStrength,
                        Pratyantardasha = pratyantardashaStrength
                    },
                    PositiveInfluences = SelectFactTexts(planets, factLords, "Support"),
                    Challenges = SelectFactTexts(planets, factLords, "Pressure"),
                    Prediction = BuildPrediction(
                        planets,
                        ascendant,
                        currentAntar.SubPlanet,
                        currentAntar.StartDate,
                        currentAntar.EndDate,
                        antardashaTimeline.Skip(Math.Max(0, antardashaTimeline.IndexOf(currentAntar) + 1)).FirstOrDefault()?.SubPlanet,
                        currentPratyantar?.InnerPlanet,
                        currentPratyantar?.StartDate,
                        currentPratyantar?.EndDate),
                    RecommendedActions = BuildRecommendedActions(currentMaha.Planet, planets, factLords, ascendant),
                    ImportantDates = new List<ImportantDateDto>
                    {
                        new() { Title = "Mahadasha Start", Date = currentMaha.StartDate },
                        new() { Title = "Antardasha Start", Date = currentAntar.StartDate },
                        new() { Title = "Antardasha End", Date = currentAntar.EndDate },
                        new() { Title = "Mahadasha End", Date = currentMaha.EndDate }
                    },
                    TransitImpact = new TransitImpactDto
                    {
                        Summary = "Transit impact is interpreted alongside the active Mahadasha and Antardasha. Use this timing with current gochar analysis for final judgment.",
                        MajorTransits = new List<TransitImpactItemDto>
                        {
                            new() { Planet = currentMaha.Planet.ToString(), Impact = "Primary dasha influence" },
                            new() { Planet = currentAntar.SubPlanet.ToString(), Impact = "Immediate antardasha influence" }
                        }
                    },
                    Remedies = BuildRemedies(currentMaha.Planet, planets, ascendant),
                    PlanetReadings = BuildPlanetReadings(planets),
                    Gochar = BuildGochar(nowUtc, request.Latitude, request.Longitude, ascendant, moonPos.Sign),
                    UpcomingDasha = upcomingMaha == null
                        ? new UpcomingDashaDto()
                        : new UpcomingDashaDto
                        {
                            Planet = upcomingMaha.Planet.ToString(),
                            StartDate = upcomingMaha.StartDate,
                            DurationYears = DashaYears(upcomingMaha.Planet),
                            Summary = $"{upcomingMaha.Planet} Mahadasha will shift focus toward {string.Join(", ", PlanetKeywords(upcomingMaha.Planet).Take(3)).ToLowerInvariant()}."
                        }
                };

                if (mahaPosition != null)
                {
                    analysis.CurrentMahadasha.Description = _predictionService.GetDashaActivatedPrediction(mahaPosition, currentMaha.Planet, strength);
                }

                return Ok(analysis);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating mahadasha analysis: {ex.Message}");
            }
        }

        private AntardashaPeriodDto BuildAntardashaPeriodDto(
            MahadashaPeriod maha,
            AntardashaPeriod antar,
            Planet birthMahadashaPlanet,
            double birthMahadashaRemainingYears)
        {
            return new AntardashaPeriodDto
            {
                SubPlanet = antar.SubPlanet.ToString(),
                StartDate = antar.StartDate,
                EndDate = antar.EndDate,
                Pratyantardashas = _dashaService.GeneratePratyantardashaForTimelinePeriod(
                        maha,
                        antar,
                        birthMahadashaPlanet,
                        birthMahadashaRemainingYears)
                    .Select(p => new PratyantardashaPeriodDto
                    {
                        InnerPlanet = p.InnerPlanet.ToString(),
                        StartDate = p.StartDate,
                        EndDate = p.EndDate
                    })
                    .ToList()
            };
        }

        private AntardashaTimelinePeriodDto BuildAntardashaTimelinePeriodDto(
            MahadashaPeriod maha,
            AntardashaPeriod antar,
            Planet birthMahadashaPlanet,
            double birthMahadashaRemainingYears,
            DateTime nowUtc)
        {
            return new AntardashaTimelinePeriodDto
            {
                Planet = antar.SubPlanet.ToString(),
                Start = antar.StartDate.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                End = antar.EndDate.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                Status = PeriodStatus(antar.StartDate, antar.EndDate, nowUtc),
                Pratyantardashas = _dashaService.GeneratePratyantardashaForTimelinePeriod(
                        maha,
                        antar,
                        birthMahadashaPlanet,
                        birthMahadashaRemainingYears)
                    .Select(p => new PratyantardashaTimelinePeriodDto
                    {
                        Planet = p.InnerPlanet.ToString(),
                        Start = p.StartDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        End = p.EndDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        Status = PeriodStatus(p.StartDate, p.EndDate, nowUtc)
                    })
                    .ToList()
            };
        }
        private ZodiacSign CalculateAscendantSign(DateTime birthUtc, double latitude, double longitude)
        {
            var location = new GeoLocation(latitude, longitude);
            var ascendantLongitude = _ephemerisProvider.GetAscendant(birthUtc, location).Longitude;
            var ascendantIndex = (int)(ascendantLongitude / 30.0);
            return (ZodiacSign)((ascendantIndex % 12) + 1);
        }
        private static List<T> WindowAround<T>(List<T> source, int centerIndex, int size)
        {
            if (source.Count <= size)
                return source;

            int start = Math.Max(0, centerIndex - size / 2);
            start = Math.Min(start, source.Count - size);
            return source.Skip(start).Take(size).ToList();
        }

        private static string PeriodStatus(DateTime start, DateTime end, DateTime now)
        {
            if (now < start)
                return "upcoming";

            if (now >= end)
                return "completed";

            return "current";
        }

        private static int CompletedPercent(DateTime start, DateTime end, DateTime now)
        {
            if (end <= start)
                return 0;

            var total = (end - start).TotalSeconds;
            var elapsed = (now - start).TotalSeconds;
            return Math.Clamp((int)Math.Round(elapsed / total * 100), 0, 100);
        }

        private static string FormatDuration(DateTime start, DateTime end)
        {
            if (end <= start)
                return "0 Days";

            int years = end.Year - start.Year;
            int months = end.Month - start.Month;
            int days = end.Day - start.Day;

            if (days < 0)
            {
                months--;
                days += DateTime.DaysInMonth(start.Year, start.Month);
            }

            if (months < 0)
            {
                years--;
                months += 12;
            }

            var parts = new List<string>();
            if (years > 0) parts.Add($"{years} {(years == 1 ? "Year" : "Years")}");
            if (months > 0) parts.Add($"{months} {(months == 1 ? "Month" : "Months")}");
            if (days > 0 || parts.Count == 0) parts.Add($"{days} {(days == 1 ? "Day" : "Days")}");
            return string.Join(", ", parts);
        }

        private List<DashaPlanetReadingDto> BuildPlanetReadings(IReadOnlyCollection<PlanetPosition> planets)
        {
            return DashaRules.VimshottariOrder.Select(item =>
            {
                var position = planets.FirstOrDefault(planet => planet.Planet == item.Planet);
                var dignity = position == null
                    ? PlanetStrength.Neutral
                    : _planetStrengthService.GetStrength(position.Planet, position.Sign);
                var description = position == null
                    ? $"{item.Planet} dasha {_predictionService.GetDashaPrediction(item.Planet)}"
                    : _predictionService.GetDashaActivatedPrediction(position, item.Planet, dignity);

                return new DashaPlanetReadingDto
                {
                    Planet = item.Planet.ToString(),
                    Description = description,
                    House = position == null ? 0 : (int)position.House,
                    Strength = BuildDashaLevelStrength("Planet", item.Planet, planets),
                    Facts = BuildPeriodFacts(item.Planet, planets)
                };
            }).ToList();
        }

        private static int DashaYears(Planet planet)
        {
            return DashaRules.VimshottariOrder.First(d => d.Planet == planet).Years;
        }

        private static string PlanetColor(Planet planet)
        {
            return planet switch
            {
                Planet.Sun => "#EF4444",
                Planet.Moon => "#64748B",
                Planet.Mars => "#DC2626",
                Planet.Rahu => "#4F46E5",
                Planet.Jupiter => "#F59E0B",
                Planet.Saturn => "#4338CA",
                Planet.Mercury => "#16A34A",
                Planet.Ketu => "#7C3AED",
                Planet.Venus => "#DB2777",
                _ => "#2563EB"
            };
        }

        private static string PlanetNature(Planet planet)
        {
            return IsBenefic(planet) ? "Benefic" : "Challenging";
        }

        private static bool IsBenefic(Planet planet)
        {
            return planet is Planet.Jupiter or Planet.Venus or Planet.Mercury or Planet.Moon;
        }

        private static List<string> PlanetKeywords(Planet planet)
        {
            return planet switch
            {
                Planet.Sun => new() { "Leadership", "Authority", "Recognition", "Confidence", "Father", "Career" },
                Planet.Moon => new() { "Mind", "Emotions", "Home", "Mother", "Public", "Comfort" },
                Planet.Mars => new() { "Action", "Courage", "Competition", "Property", "Energy", "Discipline" },
                Planet.Rahu => new() { "Ambition", "Foreign", "Innovation", "Desire", "Visibility", "Sudden Change" },
                Planet.Jupiter => new() { "Wisdom", "Growth", "Finance", "Children", "Guidance", "Spirituality" },
                Planet.Saturn => new() { "Discipline", "Responsibility", "Work", "Delay", "Persistence", "Karma" },
                Planet.Mercury => new() { "Communication", "Business", "Learning", "Analysis", "Writing", "Networking" },
                Planet.Ketu => new() { "Detachment", "Spirituality", "Research", "Past Karma", "Insight", "Liberation" },
                Planet.Venus => new() { "Relationships", "Comfort", "Creativity", "Marriage", "Luxury", "Art" },
                _ => new() { "Growth", "Timing", "Learning" }
            };
        }

        private DashaLevelStrengthDto BuildDashaLevelStrength(string level, Planet planet, IReadOnlyCollection<PlanetPosition> planets)
        {
            var position = planets.FirstOrDefault(p => p.Planet == planet);
            var strength = position == null
                ? PlanetStrength.Neutral
                : _planetStrengthService.GetStrength(position.Planet, position.Sign);
            int score = StrengthToScore(strength, position);
            var association = BuildPlanetaryAssociation(planet, planets);

            return new DashaLevelStrengthDto
            {
                Level = level,
                Planet = planet.ToString(),
                OverallStrength = score,
                PlanetStrength = StrengthLabel(strength),
                Placement = position == null ? "Chart placement unavailable" : FormatHousePlacement(position.House),
                Sign = position?.Sign.ToString() ?? string.Empty,
                AspectOnLagna = position?.House == HouseNumber.Fifth || position?.House == HouseNumber.Seventh || position?.House == HouseNumber.Ninth,
                SupportedByBenefics = association.Tone is "Benefic" or "Mixed",
                SupportedBy = association.BeneficPlanets,
                AssociationTone = association.Tone,
                AssociationSummary = association.Summary,
                AssociationDetails = association.Details,
                OverallResult = ScoreToResult(score)
            };
        }

        private static DashaAssociationResult BuildPlanetaryAssociation(Planet targetPlanet, IReadOnlyCollection<PlanetPosition> planets)
        {
            var target = planets.FirstOrDefault(p => p.Planet == targetPlanet);

            if (target == null)
            {
                return new DashaAssociationResult("None", "None", new List<string>(), new List<string>());
            }

            var beneficHits = new List<string>();
            var maleficHits = new List<string>();
            var details = new List<string>();

            foreach (var source in planets.Where(p => p.Planet != targetPlanet))
            {
                var associationType = AssociationType(source, target);

                if (associationType == null)
                {
                    continue;
                }

                var hit = $"{source.Planet} {associationType}";
                var detail = associationType == "Conjunction"
                    ? $"{source.Planet} is conjunct {targetPlanet} in the {(int)target.House} house."
                    : $"{source.Planet} aspects {targetPlanet} from the {(int)source.House} house to the {(int)target.House} house.";

                if (IsBenefic(source.Planet))
                {
                    beneficHits.Add(hit);
                    details.Add($"Benefic: {detail}");
                }
                else
                {
                    maleficHits.Add(hit);
                    details.Add($"Malefic: {detail}");
                }
            }

            var tone = (beneficHits.Count, maleficHits.Count) switch
            {
                (> 0, > 0) => "Mixed",
                (> 0, 0) => "Benefic",
                (0, > 0) => "Malefic",
                _ => "None"
            };

            var summary = tone switch
            {
                "Benefic" => beneficHits.Count == 1 ? $"Benefic: {beneficHits[0]}" : "Benefic: Multiple",
                "Malefic" => maleficHits.Count == 1 ? $"Malefic: {maleficHits[0]}" : "Malefic: Multiple",
                "Mixed" => $"Mixed: {FirstPlanetName(beneficHits)} + {FirstPlanetName(maleficHits)}",
                _ => "None"
            };

            return new DashaAssociationResult(tone, summary, details, beneficHits.Select(FirstPlanetName).Distinct().ToList());
        }

        private static string? AssociationType(PlanetPosition source, PlanetPosition target)
        {
            if (source.House == target.House)
            {
                return "Conjunction";
            }

            return AspectOffsets(source.Planet).Contains(HouseDistance(source.House, target.House))
                ? "Aspect"
                : null;
        }

        private static int HouseDistance(HouseNumber from, HouseNumber to)
        {
            return (((int)to - (int)from + 12) % 12) + 1;
        }

        private static string FormatHousePlacement(HouseNumber house)
        {
            var number = (int)house;
            var suffix = number switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th"
            };

            return $"{number}{suffix} House";
        }

        private static IReadOnlyCollection<int> AspectOffsets(Planet planet)
        {
            return planet switch
            {
                Planet.Jupiter => new[] { 5, 7, 9 },
                Planet.Mars => new[] { 4, 7, 8 },
                Planet.Saturn => new[] { 3, 7, 10 },
                Planet.Rahu or Planet.Ketu => new[] { 5, 7, 9 },
                _ => new[] { 7 }
            };
        }

        private static string FirstPlanetName(string hit)
        {
            return hit.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? hit;
        }

        private static string FirstPlanetName(IEnumerable<string> hits)
        {
            return hits.Select(FirstPlanetName).FirstOrDefault() ?? string.Empty;
        }

        private sealed record DashaAssociationResult(string Tone, string Summary, List<string> Details, List<string> BeneficPlanets);
        private static int StrengthToScore(PlanetStrength strength, PlanetPosition? position)
        {
            int score = strength switch
            {
                PlanetStrength.Exalted => 92,
                PlanetStrength.OwnSign => 84,
                PlanetStrength.Neutral => 68,
                PlanetStrength.Debilitated => 42,
                PlanetStrength.NotApplicable => 62,
                _ => 60
            };

            if (position?.IsCombust == true)
                score -= 10;

            if (position?.IsRetrograde == true)
                score -= 4;

            return Math.Clamp(score, 25, 96);
        }

        private static string StrengthLabel(PlanetStrength strength)
        {
            return strength switch
            {
                PlanetStrength.Exalted => "Very Strong",
                PlanetStrength.OwnSign => "Strong",
                PlanetStrength.Debilitated => "Weak",
                PlanetStrength.NotApplicable => "Karmic",
                _ => "Moderate"
            };
        }

        private static string ScoreToResult(int score)
        {
            if (score >= 80) return "Very Favorable";
            if (score >= 65) return "Favorable";
            if (score >= 50) return "Mixed";
            return "Challenging";
        }

        private List<LifeAreaDto> BuildLifeAreas(IReadOnlyCollection<PlanetPosition> planets, ZodiacSign lagna, Planet maha, Planet antar)
        {
            var areas = new (string Name, string Icon, int[] Houses)[]
            {
                ("Career", "work", new[] { 10 }),
                ("Finance", "payments", new[] { 2, 11 }),
                ("Relationships", "favorite", new[] { 7 }),
                ("Marriage", "diversity_3", new[] { 7 }),
                ("Health", "health_and_safety", new[] { 1, 6 }),
                ("Education", "school", new[] { 5 }),
                ("Spirituality", "self_improvement", new[] { 9, 12 }),
                ("Travel", "flight", new[] { 3, 9, 12 })
            };

            return areas.Select(area => ScoreLifeArea(planets, lagna, maha, antar, area.Name, area.Icon, area.Houses)).ToList();
        }

        private LifeAreaDto ScoreLifeArea(
            IReadOnlyCollection<PlanetPosition> planets,
            ZodiacSign lagna,
            Planet maha,
            Planet antar,
            string name,
            string icon,
            IReadOnlyList<int> houses)
        {
            var lordScores = new List<int>();
            var facts = new List<string>();

            foreach (var house in houses)
            {
                var sign = HouseSign(lagna, house);
                var lord = SignLord(sign);
                var lordPosition = planets.FirstOrDefault(planet => planet.Planet == lord);
                var lordDignity = lordPosition == null
                    ? PlanetStrength.Neutral
                    : _planetStrengthService.GetStrength(lordPosition.Planet, lordPosition.Sign);
                lordScores.Add(StrengthToScore(lordDignity, lordPosition));
                facts.Add(lordPosition == null
                    ? $"{Ordinal(house)} lord {lord} has no placed chart position."
                    : $"{Ordinal(house)} lord {lord} is {StrengthLabel(lordDignity)} in the {FormatHousePlacement(lordPosition.House)}.");

                var occupant = planets.FirstOrDefault(planet => (int)planet.House == house && planet.Planet != lord);
                if (occupant != null)
                {
                    facts.Add($"{occupant.Planet} occupies the {Ordinal(house)}.");
                }
            }

            var score = (int)Math.Round(
                lordScores.DefaultIfEmpty(60).Average() * 0.5
                + PeriodLinkScore(planets, lagna, maha, houses) * 0.3
                + PeriodLinkScore(planets, lagna, antar, houses) * 0.2);
            score = Math.Clamp(score, 25, 96);

            return new LifeAreaDto
            {
                Name = name,
                Icon = icon,
                Score = score,
                Rating = Math.Max(1, (int)Math.Round(score / 20.0)),
                Status = ScoreToResult(score),
                Fact = string.Join(" ", facts.Take(2))
            };
        }

        private int PeriodLinkScore(IReadOnlyCollection<PlanetPosition> planets, ZodiacSign lagna, Planet planet, IReadOnlyList<int> houses)
        {
            var position = planets.FirstOrDefault(item => item.Planet == planet);
            var dignity = position == null
                ? PlanetStrength.Neutral
                : _planetStrengthService.GetStrength(position.Planet, position.Sign);
            var linked = houses.Any(house =>
                (position != null && (int)position.House == house)
                || SignLord(HouseSign(lagna, house)) == planet
                || (position != null && AspectOffsets(planet).Contains(HouseDistance(position.House, (HouseNumber)house))));

            return Math.Clamp(StrengthToScore(dignity, position) + (linked ? 8 : -6), 25, 96);
        }

        private static ZodiacSign HouseSign(ZodiacSign lagna, int house)
        {
            var index = ((int)lagna - 1 + house - 1) % 12;
            return (ZodiacSign)(index + 1);
        }

        private static Planet SignLord(ZodiacSign sign)
        {
            return PlanetStrengthRules.OwnSigns.FirstOrDefault(pair => pair.Value.Contains(sign)).Key;
        }

        private static string Ordinal(int house)
        {
            var suffix = house switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th"
            };
            return $"{house}{suffix}";
        }

        private List<string> SelectFactTexts(IReadOnlyCollection<PlanetPosition> planets, IEnumerable<Planet> lords, string tone)
        {
            return lords
                .SelectMany(lord => BuildPeriodFacts(lord, planets))
                .Where(fact => fact.Tone == tone)
                .Select(fact => fact.Text)
                .Distinct()
                .ToList();
        }

        private List<DashaPeriodFactDto> BuildPeriodFacts(Planet planet, IReadOnlyCollection<PlanetPosition> planets)
        {
            var position = planets.FirstOrDefault(item => item.Planet == planet);
            var dignity = position == null
                ? PlanetStrength.Neutral
                : _planetStrengthService.GetStrength(position.Planet, position.Sign);
            var score = StrengthToScore(dignity, position);
            var tone = score >= 65 ? "Support" : score < 50 ? "Pressure" : "Neutral";
            var text = position == null
                ? $"{planet} has no placed chart position. Chart score {score}."
                : $"{planet} is {StrengthLabel(dignity)} in the {FormatHousePlacement(position.House)} in {position.Sign}. Chart score {score}.";
            if (position?.IsCombust == true)
            {
                text += " Combust.";
            }
            if (position?.IsRetrograde == true)
            {
                text += " Retrograde.";
            }

            var facts = new List<DashaPeriodFactDto>
            {
                new()
                {
                    Planet = planet.ToString(),
                    Tone = tone,
                    Score = score,
                    Text = text
                }
            };

            foreach (var detail in BuildPlanetaryAssociation(planet, planets).Details)
            {
                var detailTone = detail.StartsWith("Benefic:", StringComparison.Ordinal)
                    ? "Support"
                    : detail.StartsWith("Malefic:", StringComparison.Ordinal) ? "Pressure" : "Neutral";
                var detailText = detail;
                if (detailText.StartsWith("Benefic: ", StringComparison.Ordinal))
                {
                    detailText = detailText["Benefic: ".Length..];
                }
                else if (detailText.StartsWith("Malefic: ", StringComparison.Ordinal))
                {
                    detailText = detailText["Malefic: ".Length..];
                }

                facts.Add(new DashaPeriodFactDto
                {
                    Planet = planet.ToString(),
                    Tone = detailTone,
                    Score = score,
                    Text = detailText
                });
            }

            return facts;
        }

        private PredictionAnalysisDto BuildPrediction(
            IReadOnlyCollection<PlanetPosition> planets,
            ZodiacSign lagna,
            Planet antar,
            DateTime start,
            DateTime end,
            Planet? nextAntar,
            Planet? pratyantar,
            DateTime? pratyantarStart,
            DateTime? pratyantarEnd)
        {
            DateTime firstEnd = start.AddTicks((end - start).Ticks / 3);
            DateTime secondEnd = start.AddTicks((end - start).Ticks * 2 / 3);
            var areaNames = LifeAreaLinks(planets, lagna, antar)
                .Select(link => link.Split(':')[0])
                .Distinct()
                .ToList();
            var opening = areaNames.Count > 0
                ? $"{JoinNames(areaNames)} become active in this opening third."
                : "This opening third carries the Antardasha lord without a direct life-area link.";
            var startPoints = new List<string> { opening };
            var middlePoints = new List<string>();
            var endPoints = new List<string>
            {
                nextAntar == null
                    ? $"This Antardasha ends on {end:yyyy-MM-dd}."
                    : $"Next Antardasha is {nextAntar}, after this one ends on {end:yyyy-MM-dd}."
            };

            AddPratyantarMarker(start, firstEnd, secondEnd, end, pratyantar, pratyantarStart, pratyantarEnd, startPoints, middlePoints, endPoints);
            if (middlePoints.Count == 0)
            {
                middlePoints.Add("No Pratyantardasha begins in this third.");
            }

            return new PredictionAnalysisDto
            {
                SelectedCategory = string.Empty,
                Categories = new List<PredictionCategoryDto>(),
                PhaseAnalysis = new Dictionary<string, PredictionPhaseDto>
                {
                    ["start"] = new() { Title = "Opening", Period = $"{start:MMM yyyy} - {firstEnd:MMM yyyy}", Points = startPoints },
                    ["middle"] = new() { Title = "Middle", Period = $"{firstEnd:MMM yyyy} - {secondEnd:MMM yyyy}", Points = middlePoints },
                    ["end"] = new() { Title = "Closing", Period = $"{secondEnd:MMM yyyy} - {end:MMM yyyy}", Points = endPoints }
                }
            };
        }

        private static void AddPratyantarMarker(
            DateTime start,
            DateTime firstEnd,
            DateTime secondEnd,
            DateTime end,
            Planet? pratyantar,
            DateTime? pratyantarStart,
            DateTime? pratyantarEnd,
            List<string> startPoints,
            List<string> middlePoints,
            List<string> endPoints)
        {
            if (pratyantar == null || pratyantarStart == null || pratyantarStart < start || pratyantarStart >= end)
            {
                return;
            }

            var line = pratyantarEnd == null
                ? $"{pratyantar} Pratyantardasha, from {pratyantarStart:yyyy-MM-dd}."
                : $"{pratyantar} Pratyantardasha, {pratyantarStart:yyyy-MM-dd} to {pratyantarEnd:yyyy-MM-dd}.";
            if (pratyantarStart < firstEnd) startPoints.Add(line);
            else if (pratyantarStart < secondEnd) middlePoints.Add(line);
            else endPoints.Add(line);
        }

        private static string JoinNames(IReadOnlyList<string> names)
        {
            if (names.Count == 1)
            {
                return names[0];
            }

            return $"{string.Join(", ", names.Take(names.Count - 1))} and {names[^1]}";
        }

        private static IEnumerable<string> LifeAreaLinks(IReadOnlyCollection<PlanetPosition> planets, ZodiacSign lagna, Planet planet)
        {
            var position = planets.FirstOrDefault(item => item.Planet == planet);
            var planetHouse = position == null ? 0 : (int)position.House;
            var areas = new (string Name, int[] Houses)[]
            {
                ("Career", new[] { 10 }),
                ("Finance", new[] { 2, 11 }),
                ("Health", new[] { 1, 6 }),
                ("Education", new[] { 5 }),
                ("Spirituality", new[] { 9, 12 }),
                ("Travel", new[] { 3, 9, 12 })
            };

            foreach (var area in areas)
            {
                foreach (var house in area.Houses)
                {
                    var link = planetHouse == house
                        ? "occupies"
                        : SignLord(HouseSign(lagna, house)) == planet
                            ? "rules"
                            : position != null && AspectOffsets(planet).Contains(HouseDistance(position.House, (HouseNumber)house))
                                ? "aspects"
                                : null;
                    if (link != null)
                    {
                        yield return $"{area.Name}: {Ordinal(house)} house, {link}.";
                    }
                }
            }
        }

        private List<DashaGocharPlanetDto> BuildGochar(DateTime nowUtc, double latitude, double longitude, ZodiacSign lagna, ZodiacSign moonSign)
        {
            return _chartService.GenerateChart(nowUtc, latitude, longitude)
                .Select(planet =>
                {
                    var fromLagna = (((int)planet.Sign - (int)lagna + 12) % 12) + 1;
                    return new DashaGocharPlanetDto
                    {
                        Planet = planet.Planet.ToString(),
                        Sign = planet.Sign.ToString(),
                        HouseFromLagna = fromLagna,
                        HouseFromMoon = (((int)planet.Sign - (int)moonSign + 12) % 12) + 1,
                        AspectedHouses = AspectOffsets(planet.Planet)
                            .Select(offset => ((fromLagna - 1 + offset - 1) % 12) + 1)
                            .ToList()
                    };
                })
                .ToList();
        }

        private RecommendedActionsDto BuildRecommendedActions(Planet planet, IReadOnlyCollection<PlanetPosition> planets, IEnumerable<Planet> lords, ZodiacSign lagna)
        {
            var remedies = BuildRemedies(planet, planets, lagna);
            var pressure = SelectFactTexts(planets, lords, "Pressure");
            var dos = new List<string>
            {
                $"Japa: {remedies.Mantra}. Beej of {planet}, on {remedies.Fast}.",
                $"Daan: {remedies.Donation}. {remedies.Charity}.",
                remedies.LifestyleAdvice
            };
            if (remedies.Gemstone != "Not advised")
            {
                dos.Add($"{remedies.Gemstone}. {remedies.Deity}");
            }

            return new RecommendedActionsDto
            {
                Dos = dos,
                Donts = pressure.Count > 0
                    ? pressure
                    : new List<string> { "No pressure fact is recorded for the running period lords." }
            };
        }

        private RemediesDto BuildRemedies(Planet planet, IReadOnlyCollection<PlanetPosition> planets, ZodiacSign lagna)
        {
            var (mantra, weekday, daan, recipient, ratna, color) = planet switch
            {
                Planet.Sun => ("Om Hraam Hreem Hraum Sah Suryaya Namah", "Sunday", "Wheat, jaggery, and copper", "a person in public service", "Ruby (Manikya)", "Red"),
                Planet.Moon => ("Om Shraam Shreem Shraum Sah Chandraya Namah", "Monday", "Rice, milk, and white cloth", "a caregiver", "Pearl (Mukta)", "White"),
                Planet.Mars => ("Om Kraam Kreem Kraum Sah Bhaumaya Namah", "Tuesday", "Red lentils and red cloth", "someone who can use them", "Red coral (Vidruma)", "Red"),
                Planet.Mercury => ("Om Braam Breem Braum Sah Budhaya Namah", "Wednesday", "Green gram and green cloth", "a student", "Emerald (Marakata)", "Green"),
                Planet.Jupiter => ("Om Graam Greem Graum Sah Gurave Namah", "Thursday", "Turmeric, chana, ghee, and yellow cloth", "a teacher or student", "Yellow sapphire (Pushyaraga)", "Yellow"),
                Planet.Venus => ("Om Draam Dreem Draum Sah Shukraya Namah", "Friday", "White cloth, rice, and sugar", "someone who can use them", "Diamond (Vajra)", "White"),
                Planet.Saturn => ("Om Praam Preem Praum Sah Shanaischaraya Namah", "Saturday", "Black sesame, oil, and a blanket", "a worker or elder", "Blue sapphire (Neelam)", "Dark blue"),
                Planet.Rahu => ("Om Bhraam Bhreem Bhraum Sah Rahave Namah", "Saturday", "Sesame, black cloth, and a blanket", "a person left without support", "Hessonite (Gomed)", "Smoky blue"),
                Planet.Ketu => ("Om Sraam Sreem Sraum Sah Ketave Namah", "Tuesday", "Sesame and a blanket", "a person left without support", "Cat's eye (Vaidurya)", "Grey"),
                _ => ("Om Namah Shivaya", "Monday", "Food", "someone who can use it", "Not named", "White")
            };

            var position = planets.FirstOrDefault(item => item.Planet == planet);
            var dignity = position == null ? PlanetStrength.Neutral : _planetStrengthService.GetStrength(position.Planet, position.Sign);
            var score = StrengthToScore(dignity, position);
            var role = GrahaRole(planet, lagna);
            var combust = position?.IsCombust == true;
            var strong = dignity is PlanetStrength.Exalted or PlanetStrength.OwnSign || score >= 65;
            var desirable = role is "benefic" or "yogakaraka";
            var blocked = !desirable || combust;
            var gemstone = blocked ? "Not advised" : ratna;
            var reason = role == "node"
                ? $"{planet} does not rule a house, so its ratna is not used to strengthen the period."
                : combust
                    ? $"{planet} is combust, so the ratna is not used to strengthen an afflicted graha."
                    : !desirable
                        ? $"{planet} is {RoleLabel(role)} for this lagna, so its ratna is not used to strengthen it."
                        : strong
                            ? "The graha is already strong, so the ratna is named and not added."
                            : $"Strengthening a weak {RoleLabel(role)} may be considered with a practitioner. This result does not prescribe wearing it.";
            var daanReason = combust || role is "malefic" or "maraka" or "node"
                ? "Given as shanti, to pacify the lord, not to increase it."
                : strong
                    ? "Given to sustain the significations of a lord that is already strong."
                    : "Given as shanti while the lord is not strong.";
            var conduct = combust
                ? $"Keep the matters of {planet} modest while it is combust."
                : role is "malefic" or "maraka" or "node"
                    ? $"Meet the duties of {planet} on time. Do not expand the houses this lord pressures."
                    : strong
                        ? $"Use the strength of {planet} in the houses it rules."
                        : $"Support {planet} by repetition and a steady weekday practice.";

            return new RemediesDto
            {
                Mantra = mantra,
                Fast = weekday,
                Gemstone = gemstone,
                Color = color,
                Donation = daan,
                Deity = $"{ratna} is the navaratna of {planet}. {reason} Natal score {score}.",
                Charity = $"On {weekday}, to {recipient}. {daanReason}",
                LifestyleAdvice = conduct
            };
        }

        private static string GrahaRole(Planet planet, ZodiacSign lagna)
        {
            if (planet is Planet.Rahu or Planet.Ketu)
            {
                return "node";
            }

            var houses = Enumerable.Range(1, 12).Where(house => SignLord(HouseSign(lagna, house)) == planet).ToList();
            var trikona = houses.Any(house => house is 1 or 5 or 9);
            var kendra = houses.Any(house => house is 4 or 7 or 10);
            var dusthana = houses.Any(house => house is 6 or 8 or 12);
            var maraka = houses.Any(house => house is 2 or 7);
            if (kendra && houses.Any(house => house is 5 or 9))
            {
                return "yogakaraka";
            }
            if (trikona)
            {
                return "benefic";
            }
            if (dusthana)
            {
                return "malefic";
            }
            if (maraka)
            {
                return "maraka";
            }

            return "neutral";
        }

        private static string RoleLabel(string role)
        {
            return role switch
            {
                "yogakaraka" => "yogakaraka",
                "benefic" => "a functional benefic",
                "malefic" => "a functional malefic",
                "maraka" => "a maraka",
                "node" => "a node",
                _ => "neutral by ownership"
            };
        }
    }
}















