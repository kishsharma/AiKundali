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

        public DashaController(
            DashaService dashaService,
            ChartService chartService,
            PlanetStrengthService planetStrengthService,
            PanchangService panchangService,
            IEphemerisProvider ephemerisProvider)
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
                        .Select(a => new AntardashaPeriodDto { SubPlanet = a.SubPlanet.ToString(), StartDate = a.StartDate, EndDate = a.EndDate })
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

                int currentMahaIndex = mahadashaTimeline.FindIndex(m => m == currentMaha);
                int currentAntarIndex = antardashaTimeline.FindIndex(a => a == currentAntar);
                var mahaPosition = planets.FirstOrDefault(p => p.Planet == currentMaha.Planet);
                var strength = mahaPosition == null
                    ? PlanetStrength.Neutral
                    : _planetStrengthService.GetStrength(mahaPosition.Planet, mahaPosition.Sign);

                int overallStrength = StrengthToScore(strength, mahaPosition);
                string overallResult = ScoreToResult(overallStrength);
                var upcomingMaha = mahadashaTimeline.Skip(currentMahaIndex + 1).FirstOrDefault();

                var analysis = new MahadashaAnalysisDto
                {
                    Summary = new MahadashaSummaryDto
                    {
                        OverallScore = overallStrength,
                        OverallRating = Math.Max(1, (int)Math.Round(overallStrength / 20.0)),
                        OverallResult = overallResult,
                        CurrentMahadasha = currentMaha.Planet.ToString(),
                        CurrentAntardasha = currentAntar.SubPlanet.ToString(),
                        RemainingDuration = FormatDuration(nowUtc, currentAntar.EndDate),
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
                    Timeline = new DashaTimelineDto
                    {
                        CurrentMahadashaIndex = currentMahaIndex,
                        CurrentAntardashaIndex = currentAntarIndex,
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
                                    .Select(a => new AntardashaTimelinePeriodDto
                                    {
                                        Planet = a.SubPlanet.ToString(),
                                        Start = a.StartDate.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                                        End = a.EndDate.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                                        Status = PeriodStatus(a.StartDate, a.EndDate, nowUtc)
                                    })
                                    .ToList()
                            })
                            .ToList(),
                        Antardasha = antardashaTimeline
                            .Select(a => new AntardashaTimelinePeriodDto
                            {
                                Planet = a.SubPlanet.ToString(),
                                Start = a.StartDate.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                                End = a.EndDate.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                                Status = PeriodStatus(a.StartDate, a.EndDate, nowUtc)
                            })
                            .ToList()
                    },
                    LifeAreas = BuildLifeAreas(overallStrength, currentMaha.Planet, currentAntar.SubPlanet),
                    Strength = new DashaStrengthDto
                    {
                        OverallStrength = overallStrength,
                        Planet = currentMaha.Planet.ToString(),
                        PlanetStrength = StrengthLabel(strength),
                        Placement = mahaPosition == null ? "Chart placement unavailable" : $"{(int)mahaPosition.House} House",
                        Sign = mahaPosition?.Sign.ToString() ?? string.Empty,
                        AspectOnLagna = mahaPosition?.House == HouseNumber.Fifth || mahaPosition?.House == HouseNumber.Seventh || mahaPosition?.House == HouseNumber.Ninth,
                        SupportedByBenefics = IsBenefic(currentAntar.SubPlanet),
                        SupportedBy = IsBenefic(currentAntar.SubPlanet) ? new List<string> { currentAntar.SubPlanet.ToString() } : new List<string>(),
                        OverallResult = overallResult
                    },
                    PositiveInfluences = BuildPositiveInfluences(currentMaha.Planet, currentAntar.SubPlanet, mahaPosition, overallResult),
                    Challenges = BuildChallenges(currentMaha.Planet, currentAntar.SubPlanet),
                    Prediction = BuildPrediction(currentMaha.Planet, currentAntar.SubPlanet, currentAntar.StartDate, currentAntar.EndDate, overallStrength),
                    RecommendedActions = BuildRecommendedActions(currentMaha.Planet),
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
                    Remedies = BuildRemedies(currentMaha.Planet),
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

        private static List<LifeAreaDto> BuildLifeAreas(int baseScore, Planet maha, Planet antar)
        {
            var areas = new[]
            {
                ("Career", "work", maha is Planet.Sun or Planet.Saturn or Planet.Mars or Planet.Jupiter ? 8 : 0),
                ("Finance", "payments", maha is Planet.Jupiter or Planet.Venus or Planet.Mercury ? 8 : 0),
                ("Relationships", "favorite", maha is Planet.Venus or Planet.Moon or Planet.Jupiter ? 8 : -2),
                ("Marriage", "diversity_3", antar is Planet.Venus or Planet.Jupiter or Planet.Moon ? 8 : -2),
                ("Health", "health_and_safety", maha is Planet.Mars or Planet.Saturn or Planet.Rahu or Planet.Ketu ? -8 : 0),
                ("Education", "school", maha is Planet.Jupiter or Planet.Mercury or Planet.Ketu ? 8 : 0),
                ("Spirituality", "self_improvement", maha is Planet.Jupiter or Planet.Ketu or Planet.Saturn ? 8 : 0),
                ("Travel", "flight", maha is Planet.Rahu or Planet.Moon or Planet.Mercury ? 8 : 0)
            };

            return areas
                .Select(area =>
                {
                    int score = Math.Clamp(baseScore + area.Item3, 35, 96);
                    return new LifeAreaDto
                    {
                        Name = area.Item1,
                        Icon = area.Item2,
                        Score = score,
                        Rating = Math.Max(1, (int)Math.Round(score / 20.0)),
                        Status = ScoreToResult(score)
                    };
                })
                .ToList();
        }

        private static List<string> BuildPositiveInfluences(Planet maha, Planet antar, PlanetPosition? mahaPosition, string result)
        {
            var placement = mahaPosition == null
                ? $"{maha} activates its natural dasha themes."
                : $"{maha} activates matters of the {(int)mahaPosition.House} house in {mahaPosition.Sign}.";

            return new List<string>
            {
                placement,
                $"{antar} Antardasha shapes the immediate results of this Mahadasha.",
                $"{result} timing is indicated by the current dasha strength.",
                "Use this period for conscious planning, disciplined action, and timely decisions.",
                "Results improve when choices align with the active planetary themes."
            };
        }

        private static List<string> BuildChallenges(Planet maha, Planet antar)
        {
            return new List<string>
            {
                $"Avoid overdoing the lower expression of {maha}.",
                $"{antar} can bring short-term fluctuations within the larger Mahadasha.",
                "Do not judge results from dasha alone; confirm with chart strength and transits.",
                "Keep routines steady during major period transitions.",
                "Avoid impulsive decisions near Antardasha start and end dates."
            };
        }

        private static PredictionAnalysisDto BuildPrediction(Planet maha, Planet antar, DateTime start, DateTime end, int score)
        {
            DateTime firstEnd = start.AddTicks((end - start).Ticks / 3);
            DateTime secondEnd = start.AddTicks((end - start).Ticks * 2 / 3);

            return new PredictionAnalysisDto
            {
                SelectedCategory = "Career",
                Categories = BuildLifeAreas(score, maha, antar)
                    .Select(area => new PredictionCategoryDto
                    {
                        Name = area.Name,
                        Score = area.Score,
                        Prediction = $"{area.Name} gives {area.Status.ToLowerInvariant()} results during {maha} / {antar}."
                    })
                    .ToList(),
                PhaseAnalysis = new Dictionary<string, PredictionPhaseDto>
                {
                    ["start"] = new()
                    {
                        Title = "Start Phase",
                        Period = $"{start:MMM yyyy} - {firstEnd:MMM yyyy}",
                        Points = new()
                        {
                            "The Antardasha themes begin to surface.",
                            "Observe new responsibilities, people, and opportunities.",
                            "Start with measured decisions rather than sudden commitments."
                        }
                    },
                    ["middle"] = new()
                    {
                        Title = "Middle Phase",
                        Period = $"{firstEnd:MMM yyyy} - {secondEnd:MMM yyyy}",
                        Points = new()
                        {
                            "The period becomes more active and visible.",
                            "Results improve through consistent effort.",
                            "Use feedback from early events to adjust direction."
                        }
                    },
                    ["end"] = new()
                    {
                        Title = "End Phase",
                        Period = $"{secondEnd:MMM yyyy} - {end:MMM yyyy}",
                        Points = new()
                        {
                            "The Antardasha starts preparing the next transition.",
                            "Complete pending work and reduce avoidable risk.",
                            "Reflect on lessons before the next sub-period begins."
                        }
                    }
                }
            };
        }

        private static RecommendedActionsDto BuildRecommendedActions(Planet planet)
        {
            var remedies = BuildRemedies(planet);
            return new RecommendedActionsDto
            {
                Dos = new List<string>
                {
                    $"Respect the constructive qualities of {planet}.",
                    $"Recite '{remedies.Mantra}' with steady intention.",
                    $"Use {remedies.Color.ToLowerInvariant()} thoughtfully on supportive days.",
                    $"Practice charity through {remedies.Charity.ToLowerInvariant()}.",
                    "Keep decisions practical and documented."
                },
                Donts = new List<string>
                {
                    "Avoid fear-based decisions.",
                    "Avoid shortcuts in career, money, and relationships.",
                    "Avoid starting conflicts during emotional highs.",
                    "Do not rely on remedies without disciplined action.",
                    "Avoid ignoring health and routine."
                }
            };
        }

        private static RemediesDto BuildRemedies(Planet planet)
        {
            return planet switch
            {
                Planet.Sun => new RemediesDto { Mantra = "Om Suryaya Namah", Fast = "Sunday", Gemstone = "Ruby", Color = "Red", Donation = "Wheat, jaggery", Deity = "Surya", Charity = "Support father figures or public service", LifestyleAdvice = "Practice humility while accepting responsibility." },
                Planet.Moon => new RemediesDto { Mantra = "Om Somaya Namah", Fast = "Monday", Gemstone = "Pearl", Color = "White", Donation = "Rice, milk", Deity = "Shiva", Charity = "Support mothers and caregivers", LifestyleAdvice = "Stabilize sleep, food, and emotional routine." },
                Planet.Mars => new RemediesDto { Mantra = "Om Mangalaya Namah", Fast = "Tuesday", Gemstone = "Red Coral", Color = "Red", Donation = "Lentils", Deity = "Hanuman", Charity = "Support soldiers, athletes, or emergency workers", LifestyleAdvice = "Channel anger into disciplined effort." },
                Planet.Rahu => new RemediesDto { Mantra = "Om Rahave Namah", Fast = "Saturday", Gemstone = "Hessonite", Color = "Smoky Blue", Donation = "Blankets, sesame", Deity = "Durga", Charity = "Help marginalized people", LifestyleAdvice = "Avoid obsession and verify unusual opportunities." },
                Planet.Jupiter => new RemediesDto { Mantra = "Om Brim Brihaspataye Namah", Fast = "Thursday", Gemstone = "Yellow Sapphire", Color = "Yellow", Donation = "Turmeric, yellow clothes", Deity = "Vishnu", Charity = "Donate books to students", LifestyleAdvice = "Respect teachers, keep learning, and avoid ego." },
                Planet.Saturn => new RemediesDto { Mantra = "Om Sham Shanicharaya Namah", Fast = "Saturday", Gemstone = "Blue Sapphire", Color = "Dark Blue", Donation = "Black sesame, oil", Deity = "Shani", Charity = "Serve workers, elders, and the poor", LifestyleAdvice = "Be punctual, patient, and consistent." },
                Planet.Mercury => new RemediesDto { Mantra = "Om Bum Budhaya Namah", Fast = "Wednesday", Gemstone = "Emerald", Color = "Green", Donation = "Green gram", Deity = "Vishnu", Charity = "Support education and communication access", LifestyleAdvice = "Think clearly, write things down, and avoid nervous haste." },
                Planet.Ketu => new RemediesDto { Mantra = "Om Ketave Namah", Fast = "Tuesday", Gemstone = "Cat's Eye", Color = "Grey", Donation = "Multi-colored cloth", Deity = "Ganesha", Charity = "Support spiritual or animal welfare causes", LifestyleAdvice = "Meditate and detach from unnecessary control." },
                Planet.Venus => new RemediesDto { Mantra = "Om Shukraya Namah", Fast = "Friday", Gemstone = "Diamond", Color = "White", Donation = "White sweets, clothes", Deity = "Lakshmi", Charity = "Support women, artists, or marriage aid", LifestyleAdvice = "Balance pleasure with responsibility." },
                _ => new RemediesDto { Mantra = "Om Namah Shivaya", Fast = "Monday", Gemstone = "Consult astrologer", Color = "White", Donation = "Food", Deity = "Shiva", Charity = "Serve those in need", LifestyleAdvice = "Keep life simple and disciplined." }
            };
        }
    }
}





