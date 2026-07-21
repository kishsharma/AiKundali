using KundaliEngine.API.DTOs.Requests;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Dasha;
using KundaliEngine.Astrology.Services.House;
using KundaliEngine.Astrology.Services.KundaliDosha;
using KundaliEngine.Astrology.Services.KundaliYoga;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.Astrology.Services.Scoring;
using KundaliEngine.Astrology.Services.SudarshanChakra;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.Domain.Rules;
using KundaliEngine.KnowledgeBase.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using KundaliEngine.API.Helper_functions;
using KundaliEngine.API.Services.Recommendations;
using KundaliEngine.API.Services.AdvancedStrength;
using KundaliEngine.API.Services.Audit;
using KundaliEngine.API.Services.YogaDoshaActivation;
using System.Text.Json;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using KundaliEngine.Infrastructure.Data;
using KundaliEngine.Domain.Entities;
using KundaliEngine.Astrology.Services.DivisionalCharts.Calculators;
using Microsoft.AspNetCore.Authorization;

namespace KundaliEngine.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KundaliController : ControllerBase
    {
        private readonly ChartService _chartService;
        private readonly PanchangService _panchangService;
        private readonly PlanetScoreService _planetScoreService;
        private readonly HouseScoreService _houseScoreservice;
        private readonly DashaService _dashaService;
        private readonly OverallKundaliScoreService _overallScoreService;
        private readonly AspectService _aspectService;
        private readonly YogaDetectionService _yogaDetectionService;
        private readonly DoshaDetectionService _doshaDetectionService;
        private readonly YogaMetadataService _yogaMetadataService;
        private readonly DoshaMetadataService _doshaMetadataService;
        private readonly NakshatraMetadataService _nakshatraMetadataService;
        private readonly RashiMetadataService _rashiMetadataService;
        private readonly IEphemerisProvider _ephemerisProvider;
        private readonly RemediesRecommendationService _remediesRecommendationService;
        private readonly SudarshanChakraService _sudarshanChakraService;
        private readonly YogaDoshaActivationService _yogaDoshaActivationService;
        private readonly AdvancedStrengthTableService _advancedStrengthTableService;
        private readonly KundaliDbContext _dbContext;
        private readonly IAuditLogService _auditLogService;
        public KundaliController(ChartService chartService, PanchangService panchangService, PlanetScoreService planetScoreService, 
            HouseScoreService houseScoreService, DashaService dashaService,
            OverallKundaliScoreService overallScoreService,
            AspectService aspectService,
            YogaDetectionService yogaDetectionService,
            DoshaDetectionService doshaDetectionService,
            YogaMetadataService yogaMetadataService,
            DoshaMetadataService doshaMetadataService,
            NakshatraMetadataService nakshatraMetadataService,
            RashiMetadataService rashiMetadataService,
            IEphemerisProvider ephemerisProvider,
            RemediesRecommendationService remediesRecommendationService,
            SudarshanChakraService sudarshanChakraService,
            YogaDoshaActivationService yogaDoshaActivationService,
            AdvancedStrengthTableService advancedStrengthTableService,
            KundaliDbContext dbContext,
            IAuditLogService auditLogService
            )
        {
            _chartService = chartService;
            _panchangService = panchangService;
            _planetScoreService = planetScoreService;
            _houseScoreservice = houseScoreService;
            _dashaService = dashaService;
            _overallScoreService = overallScoreService;
            _aspectService = aspectService;
            _yogaDetectionService = yogaDetectionService;
            _doshaDetectionService = doshaDetectionService;
            _yogaMetadataService = yogaMetadataService;
            _doshaMetadataService = doshaMetadataService;
            _nakshatraMetadataService = nakshatraMetadataService;
            _rashiMetadataService = rashiMetadataService;
            _ephemerisProvider = ephemerisProvider;
            _remediesRecommendationService = remediesRecommendationService;
            _sudarshanChakraService = sudarshanChakraService;
            _yogaDoshaActivationService = yogaDoshaActivationService;
            _advancedStrengthTableService = advancedStrengthTableService;
            _dbContext = dbContext;
            _auditLogService = auditLogService;
        }
        [Authorize]
        [HttpPost("generate")]
        public ActionResult<GenerateKundaliResponse> Generate([FromBody] GenerateKundaliRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            // Generate planet positions (Kundali chart)
            TimeZoneInfo indiaZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

            // Assume the input is in IST (India Standard Time)
            var birthLocal = DateTime.SpecifyKind(request.BirthDateTime, DateTimeKind.Unspecified);
            request.BirthDateTime = birthLocal;
            DateTime birthUtc = TimeZoneInfo.ConvertTimeToUtc(request.BirthDateTime, indiaZone);

            request.BirthDateTime= birthUtc;
            var planets = _chartService.GenerateChart(request.BirthDateTime, request.Latitude, request.Longitude );
            // Panchang
            double sunLongitude = planets.First(p => p.Planet == Planet.Sun).Longitude;
            double moonLongitude = planets.First(p => p.Planet == Planet.Moon).Longitude;
            var panchang = _panchangService.Calculate(request.BirthDateTime, sunLongitude, moonLongitude);
            panchang.Akshara = _nakshatraMetadataService.GetPadaAkshara(panchang.Nakshatra, panchang.Pada);

            // Dasha
            var moonPos = planets.First(p => p.Planet == Planet.Moon);
            var birthDasha = _dashaService.GetBirthMahadasha(moonPos);
            var rashiInsight = BuildRashiInsight(moonPos.Sign.ToString());
            var nakshatraInsight = BuildNakshatraInsight(panchang.Nakshatra, panchang.Pada);

            // Ascendant
            var location = new GeoLocation(request.Latitude, request.Longitude);
            double ascendantLongitude = _ephemerisProvider.GetAscendant(request.BirthDateTime, location).Longitude;
            int ascendantIndex = (int)(ascendantLongitude / 30.0);
            ZodiacSign ascendantSign = (ZodiacSign)((ascendantIndex % 12) + 1);
            double ascendantDegreeInSign = NormalizeDegreeInSign(ascendantLongitude);
            var d9Chart = BuildD9Chart(planets, ascendantSign, ascendantDegreeInSign);

            // Houses and house scores
            var aspectResult = _aspectService.CalculateGrahaDrishti(planets);
            var houseScores = _houseScoreservice.CalculateAllHouseScores(ascendantSign, planets, aspectResult);
            var sudarshanChakra = _sudarshanChakraService.Calculate(planets, ascendantSign);

            // Compute house signs (house N sign = ascendantSign + (N-1))
            var houseDtos = new List<DTOs.Responses.HouseDto>();
            for (int i = 1; i <= 12; i++)
            {
                var houseNumber = (HouseNumber)i;
                var signIndex = (((int)ascendantSign - 1) + (i - 1)) % 12;
                var houseSign = (ZodiacSign)(signIndex + 1);
                var houseOwner = HelperData.GetSignOwner(houseSign);

                var houseAspects = aspectResult.HouseAspects
                    .Where(a => a.ToHouse == i)
                    .ToList();
                var aspectedBy = houseAspects
                    .Select(a => a.FromPlanet.ToString())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var occupants = planets.Where(p => (int)p.House == i).Select(p => p.Planet.ToString()).ToList();
                var scoreResult = houseScores.First(h => (int)h.HouseNumber == i);


                houseDtos.Add(new DTOs.Responses.HouseDto
                {
                    HouseNumber = i,
                    Sign = houseSign.ToString(),
                    HouseOwner = houseOwner,
                    HouseSwamy = houseOwner,
                    Occupants = occupants,
                    HasBeneficAspect = houseAspects.Any(a => HelperData.IsNaturalBenefic(a.FromPlanet)),
                    HasMaleficAspect = houseAspects.Any(a => HelperData.IsNaturalMaleficOrShadow(a.FromPlanet)),
                    AspectedBy = aspectedBy,
                    Score = scoreResult.Score,
                    Factors = scoreResult.Factors,
                    DominantThemes = scoreResult.DominantThemes,
                    HouseName = HouseMetadataService.GetHouseName(i),
                    LifeAreas = HouseMetadataService.GetLifeAreas(i),
                });
            }

            // map the new ScoreBreakdown from domain result into DTO
            var overall = _overallScoreService.Calculate(ascendantSign, planets, aspectResult, null);

            var overallDto = new OverallScoreDto
            {
                Overall = overall.OverallScore,
                Stability = overall.StabilityScore,
                Growth = overall.GrowthScore,
                Challenge = overall.ChallengeScore,
                DominantStrengths = overall.DominantStrengths,
                DominantWeaknesses = overall.DominantWeaknesses,
                OverallFactors = overall.OverallFactors,
                StabilityFactors = overall.StabilityFactors,
                GrowthFactors = overall.GrowthFactors,
                ChallengeFactors = overall.ChallengeFactors,
                ScoreBreakdown = new ScoreBreakdownDto
                {
                    Overall = overall.ScoreBreakdown.Overall.Select(c => new ScoreComponentDto { Label = c.Label, Score = c.Score, Weight = c.Weight, Contribution = c.Contribution }).ToList(),
                    Stability = overall.ScoreBreakdown.Stability.Select(c => new ScoreComponentDto { Label = c.Label, Score = c.Score, Weight = c.Weight, Contribution = c.Contribution }).ToList(),
                    Growth = overall.ScoreBreakdown.Growth.Select(c => new ScoreComponentDto { Label = c.Label, Score = c.Score, Weight = c.Weight, Contribution = c.Contribution }).ToList(),
                    Challenge = overall.ScoreBreakdown.Challenge.Select(c => new ScoreComponentDto { Label = c.Label, Score = c.Score, Weight = c.Weight, Contribution = c.Contribution }).ToList()
                }
            };

            var currentUtc = DateTime.UtcNow;
            var dashaCalculation = _dashaService.CalculateCurrentDasha(
                birthDasha.Planet,
                birthDasha.RemainingYears,
                request.BirthDateTime,
                currentUtc);

            var currentDashaDto = new CurrentDashaDto
            {
                Mahadasha = dashaCalculation.CurrentMahadasha.Planet.ToString(),
                Antardasha = dashaCalculation.CurrentAntardasha.SubPlanet.ToString(),
                StartDate = dashaCalculation.CurrentMahadasha.StartDate,
                EndDate = dashaCalculation.CurrentMahadasha.EndDate,
                AntardashaStartDate = dashaCalculation.CurrentAntardasha.StartDate,
                AntardashaEndDate = dashaCalculation.CurrentAntardasha.EndDate,
                Summary = string.Empty
            };
            var transitInsight = BuildTransitInsight(request, ascendantSign, currentDashaDto, currentUtc, planets, houseDtos);
            var remediesRecommendation = _remediesRecommendationService.Recommend(
                ascendantSign,
                moonPos.Sign,
                planets,
                currentDashaDto);
            var houseDomainDtos = houseDtos.Select(h => new Domain.Models.HouseDto
            {
                HouseNumber = (HouseNumber)h.HouseNumber,
                Sign = Enum.Parse<ZodiacSign>(h.Sign),
                Occupants = h.Occupants.Select(o => Enum.Parse<Planet>(o)).ToList()
            }).ToList();

            var mahadashaTimeline = _dashaService.GenerateMahadashaTimelineForYears(
                birthDasha.Planet,
                birthDasha.RemainingYears,
                request.BirthDateTime,
                120);

            var activationTimeline = mahadashaTimeline
                .Select(mahadasha => new MahadashaActivationPeriod
                {
                    Planet = mahadasha.Planet,
                    StartDate = mahadasha.StartDate,
                    EndDate = mahadasha.EndDate,
                    Antardashas = _dashaService.GenerateAntardashaForTimelinePeriod(
                        mahadasha,
                        birthDasha.Planet,
                        birthDasha.RemainingYears)
                })
                .ToList();

            var mahadashaPeriodDtos = activationTimeline
                .Select(mahadasha => new MahadashaPeriodDto
                {
                    Planet = mahadasha.Planet.ToString(),
                    Start = mahadasha.StartDate,
                    End = mahadasha.EndDate,
                    Antardashas = mahadasha.Antardashas
                        .Select(antardasha => new AntardashaPeriodDto
                        {
                            SubPlanet = antardasha.SubPlanet.ToString(),
                            StartDate = antardasha.StartDate,
                            EndDate = antardasha.EndDate
                        })
                        .ToList()
                })
                .ToList();

            // Detect yogas
            var detectedYogas = _yogaDetectionService.DetectYogas(planets, houseDomainDtos);

            // Map to YogaDto
            var yogas = detectedYogas
                .Select(match => new { Match = match, Meta = _yogaMetadataService.GetYoga(match.Key) })
                .Where(item => item.Meta != null)
                .Select(item => new YogaDto
                {
                    Key = item.Meta!.Key,
                    Name = item.Meta.Name,
                    SanskritName = item.Meta.SanskritName,
                    Category = item.Meta.Category,
                    Nature = item.Meta.Nature,
                    Description = item.Meta.Description,
                    Impact = item.Meta.Impact,
                    ImpactAreas = item.Meta.ImpactAreas
                        .Select(area => new YogaImpactAreaDto
                        {
                            Category = area.Category,
                            Statements = area.Statements
                        })
                        .ToList(),
                    References = item.Meta.References,
                    Strength = item.Match.Strength,
                    InvolvedPlanets = item.Match.InvolvedPlanets,
                    InvolvedHouses = item.Match.InvolvedHouses,
                    Reason = item.Match.Reason,
                    ActivationPeriods = _yogaDoshaActivationService.CalculateActivationPeriods(
                        item.Meta!.Key,
                        item.Meta.Name,
                        "Yoga",
                        item.Match.InvolvedPlanets,
                        activationTimeline,
                        currentUtc)
                })
                .ToList();

            var doshas = _doshaDetectionService.DetectDoshas(planets, houseDomainDtos)
                .Select(match => new { Match = match, Meta = _doshaMetadataService.GetDosha(match.Key) })
                .Select(item => new DoshaDto
                {
                    Key = item.Match.Key,
                    Name = item.Meta?.Name ?? item.Match.Key,
                    Category = item.Meta?.Category ?? string.Empty,
                    Severity = item.Match.Severity,
                    IsPresent = item.Match.IsPresent,
                    Description = item.Meta?.Description ?? string.Empty,
                    Reason = item.Match.Reason,
                    InvolvedPlanets = item.Match.InvolvedPlanets,
                    InvolvedHouses = item.Match.InvolvedHouses,
                    ImpactAreas = item.Meta?.ImpactAreas
                        .Select(area => new DoshaImpactAreaDto
                        {
                            Category = area.Category,
                            Statements = area.Statements
                        })
                        .ToList() ?? new(),
                    Remedies = item.Meta?.Remedies ?? new(),
                    References = item.Meta?.References ?? new(),
                    ActivationPeriods = _yogaDoshaActivationService.CalculateActivationPeriods(
                        item.Match.Key,
                        item.Meta?.Name ?? item.Match.Key,
                        "Dosha",
                        item.Match.InvolvedPlanets,
                        activationTimeline,
                        currentUtc)
                })
                .ToList();

            // Build response (mapped)
            var response = new GenerateKundaliResponse
            {
                Summary = new KundaliSummaryDto
                {
                    Name = request.Name,
                    Ascendant = $"{(int)ascendantSign} - {ascendantSign}",
                    MoonSign = moonPos.Sign.ToString(),
                    SunSign = planets.First(p => p.Planet == Planet.Sun).Sign.ToString(),
                    Nakshatra = panchang.Nakshatra,
                    Tithi = panchang.Tithi,
                    CurrentMahadasha = currentDashaDto.Mahadasha,
                    CurrentAntardasha = currentDashaDto.Antardasha,
                    Summary = "Kundali generated successfully"
                },
                Panchang = new PanchangDto
                {
                    Tithi = panchang.Tithi,
                    Paksha = panchang.Paksha,
                    Nakshatra = panchang.Nakshatra,
                    Pada = panchang.Pada,
                    Akshara = panchang.Akshara,
                    Yoga = panchang.Yoga,
                    Karana = panchang.Karana,
                    Vaar = panchang.Vaar,
                    Maas = panchang.Maas,
                    Shake=panchang.Shake,
                    Samvat=panchang.Samvat

                },
                Planets = planets.Select(p => MapPlanetDto(p, aspectResult)).ToList(),
                Houses = houseDtos,
                Scores = overallDto,
                CurrentDasha = currentDashaDto,
                RashiInsight = rashiInsight,
                NakshatraInsight = nakshatraInsight,
                TransitInsight = transitInsight,
                RemediesRecommendation = remediesRecommendation,
                MahadashaPeriods = mahadashaPeriodDtos,
                Yogas = yogas,
                Doshas = doshas,
                D9Chart = d9Chart,
                SudarshanChakra = MapSudarshanChakraDto(sudarshanChakra),
                AdvancedStrengthTables = _advancedStrengthTableService.Build(planets, houseDtos, aspectResult)
            };

            return Ok(response);
        }

        [Authorize]
        [HttpPost("save")]
        public async Task<ActionResult<SavedKundaliDetailDto>> Save([FromBody] SaveKundaliRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request is required");
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Name is required");
            }

            if (string.IsNullOrWhiteSpace(request.BirthPlace))
            {
                return BadRequest("Birth place is required");
            }

            if (request.ChartData.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                return BadRequest("Generated kundali data is required before saving");
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var kundali = new Kundali
            {
                UserId = userId.Value,
                FullName = request.Name.Trim(),
                Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim(),
                DateOfBirth = DateOnly.FromDateTime(request.BirthDateTime),
                TimeOfBirth = TimeOnly.FromDateTime(request.BirthDateTime),
                BirthPlace = request.BirthPlace.Trim(),
                Latitude = Convert.ToDecimal(request.Latitude),
                Longitude = Convert.ToDecimal(request.Longitude),
                TimeZone = FormatTimeZoneOffset(request.TimeZoneOffset),
                ChartDataJson = request.ChartData.GetRawText(),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Kundalis.Add(kundali);
            await _dbContext.SaveChangesAsync();
            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "KundaliCreated",
                Category = "DataChange",
                EntityType = "Kundali",
                EntityId = kundali.Id.ToString(),
                Description = $"Kundali saved for {kundali.FullName}.",
                Status = "Success"
            });

            return CreatedAtAction(nameof(GetById), new { id = kundali.Id }, MapSavedKundaliDetail(kundali));
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<List<SavedKundaliListItemDto>>> GetAll()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var kundalis = await _dbContext.Kundalis
                .AsNoTracking()
                .Where(k => k.UserId == userId.Value)
                .OrderByDescending(k => k.CreatedAt)
                .Select(k => new SavedKundaliListItemDto
                {
                    Id = k.Id,
                    FullName = k.FullName,
                    DateOfBirth = k.DateOfBirth,
                    TimeOfBirth = k.TimeOfBirth,
                    BirthPlace = k.BirthPlace,
                    CreatedAt = k.CreatedAt,
                    DisplayName = k.FullName + " - " + k.DateOfBirth.ToString("yyyy-MM-dd") + " - " + k.BirthPlace
                })
                .ToListAsync();

            return Ok(kundalis);
        }

        [Authorize]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<SavedKundaliDetailDto>> GetById(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var kundali = await _dbContext.Kundalis
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId.Value);

            if (kundali == null)
            {
                return NotFound();
            }

            return Ok(MapSavedKundaliDetail(kundali));
        }


        [Authorize]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var kundali = await _dbContext.Kundalis
                .FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId.Value);

            if (kundali == null)
            {
                return NotFound();
            }

            var reports = await _dbContext.KundaliReports
                .Where(report => report.KundaliId == id)
                .ToListAsync();

            _dbContext.KundaliReports.RemoveRange(reports);
            _dbContext.Kundalis.Remove(kundali);
            await _dbContext.SaveChangesAsync();
            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "KundaliDeleted",
                Category = "DataChange",
                EntityType = "Kundali",
                EntityId = id.ToString(),
                Description = $"Kundali deleted for {kundali.FullName}.",
                Status = "Success"
            });

            return NoContent();
        }
        private Guid? GetCurrentUserId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claimValue, out var userId) ? userId : null;
        }

        private static SavedKundaliDetailDto MapSavedKundaliDetail(Kundali kundali)
        {
            return new SavedKundaliDetailDto
            {
                Id = kundali.Id,
                FullName = kundali.FullName,
                Gender = kundali.Gender,
                DateOfBirth = kundali.DateOfBirth,
                TimeOfBirth = kundali.TimeOfBirth,
                BirthPlace = kundali.BirthPlace,
                Latitude = kundali.Latitude,
                Longitude = kundali.Longitude,
                TimeZone = kundali.TimeZone,
                ChartData = JsonSerializer.Deserialize<JsonElement>(kundali.ChartDataJson),
                CreatedAt = kundali.CreatedAt
            };
        }

        private static string FormatTimeZoneOffset(double offset)
        {
            var sign = offset < 0 ? "-" : "+";
            var absolute = Math.Abs(offset);
            var hours = (int)Math.Truncate(absolute);
            var minuteValue = absolute - hours;
            var minutes = (int)Math.Round(minuteValue * 100);

            if (minutes > 59)
            {
                minutes = (int)Math.Round(minuteValue * 60);
            }

            return $"UTC{sign}{hours:00}:{minutes:00}";
        }
        private RashiInsightDto? BuildRashiInsight(string moonSign)
        {
            var rashi = _rashiMetadataService.GetRashiBySign(moonSign);
            if (rashi == null)
            {
                return null;
            }

            return new RashiInsightDto
            {
                Name = rashi.Name,
                EnglishName = rashi.EnglishName,
                Symbol = rashi.Symbol,
                Ruler = rashi.Ruler,
                Element = rashi.Element,
                Strength = rashi.Strengths.FirstOrDefault() ?? string.Empty,
                Weakness = rashi.Weaknesses.FirstOrDefault() ?? string.Empty,
                Gemstone = rashi.Gemstone,
                Metal = rashi.Metal,
                Color = rashi.Color,
                LuckyNumbers = rashi.LuckyNumbers,
                FavorableDay = rashi.FavorableDay,
                FavorableDirection = rashi.FavorableDirection
            };
        }

        private NakshatraInsightDto? BuildNakshatraInsight(string nakshatraName, int pada)
        {
            var nakshatra = _nakshatraMetadataService.GetNakshatra(nakshatraName);
            if (nakshatra == null)
            {
                return null;
            }

            return new NakshatraInsightDto
            {
                Name = nakshatraName,
                Pada = pada,
                Lord = nakshatra.Lord,
                Symbol = nakshatra.Symbol,
                Gana = nakshatra.Gana,
                Yoni = nakshatra.Yoni,
                Nadi = nakshatra.Nadi,
                Varna = nakshatra.Varna,
                Vashya = string.Join(", ", nakshatra.Vashya),
                Dosha = GetDominantDosha(nakshatra.Dosha),
                FastingDay = nakshatra.FastingDay,
                BusinessSuitability = nakshatra.BusinessSuitability.FirstOrDefault() ?? string.Empty
            };
        }

        private static string GetDominantDosha(Dictionary<string, string?>? dosha)
        {
            if (dosha == null || dosha.Count == 0)
            {
                return string.Empty;
            }

            var highDosha = dosha.FirstOrDefault(item =>
                string.Equals(item.Value, "High", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(highDosha.Key))
            {
                return $"{ToTitleCase(highDosha.Key)}: {highDosha.Value}";
            }

            var rankedDosha = dosha
                .Where(item => !string.IsNullOrWhiteSpace(item.Value))
                .OrderByDescending(item => GetDoshaRank(item.Value!))
                .FirstOrDefault();

            return string.IsNullOrWhiteSpace(rankedDosha.Key)
                ? string.Empty
                : $"{ToTitleCase(rankedDosha.Key)}: {rankedDosha.Value}";
        }

        private static int GetDoshaRank(string value)
        {
            return value.Trim().ToLowerInvariant() switch
            {
                "high" => 3,
                "medium" => 2,
                "low" => 1,
                _ => 0
            };
        }

        private static string ToTitleCase(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
        }

        private TransitInsightDto BuildTransitInsight(
            GenerateKundaliRequest request,
            ZodiacSign natalAscendantSign,
            CurrentDashaDto currentDasha,
            DateTime currentUtc,
            List<PlanetPosition> natalPlanets,
            List<DTOs.Responses.HouseDto> natalHouses)
        {
            var transitPlanets = _chartService.GenerateChart(currentUtc, request.Latitude, request.Longitude);

            foreach (var planet in transitPlanets)
            {
                planet.House = CalculateHouseFromAscendant(planet.Sign, natalAscendantSign);
            }

            var transitAspectResult = _aspectService.CalculateGrahaDrishti(transitPlanets);
            var activations = transitPlanets
                .Select(planet => BuildTransitActivation(planet, natalPlanets, natalHouses, currentDasha))
                .OrderByDescending(GetTransitPriority)
                .ThenBy(item => item.NatalHouse)
                .ToList();

            return new TransitInsightDto
            {
                CalculatedAtUtc = currentUtc,
                LocationMode = "BirthLocationNatalHouses",
                Place = request.BirthPlace ?? string.Empty,
                NatalAscendant = $"{(int)natalAscendantSign} - {natalAscendantSign}",
                Planets = transitPlanets.Select(planet => MapPlanetDto(planet, transitAspectResult)).ToList(),
                Activations = activations
            };
        }

        private static TransitActivationDto BuildTransitActivation(
            PlanetPosition transitPlanet,
            List<PlanetPosition> natalPlanets,
            List<DTOs.Responses.HouseDto> natalHouses,
            CurrentDashaDto currentDasha)
        {
            var natalHouse = (int)transitPlanet.House;
            var natalPlanetsInHouse = natalPlanets
                .Where(planet => (int)planet.House == natalHouse)
                .Select(planet => planet.Planet.ToString())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var houseLord = natalHouses
                .FirstOrDefault(house => house.HouseNumber == natalHouse)
                ?.HouseOwner ?? string.Empty;

            var activation = new TransitActivationDto
            {
                Planet = transitPlanet.Planet.ToString(),
                Sign = transitPlanet.Sign.ToString(),
                NatalHouse = natalHouse,
                FocusArea = GetTransitFocusArea(natalHouse),
                NatalPlanetsInHouse = natalPlanetsInHouse,
                NatalHouseLord = houseLord,
                IsMahadashaPlanet = IsDashaPlanet(transitPlanet.Planet.ToString(), currentDasha.Mahadasha),
                IsAntardashaPlanet = IsDashaPlanet(transitPlanet.Planet.ToString(), currentDasha.Antardasha),
                ActivatesNatalMahadashaPlanet = natalPlanetsInHouse.Any(planet => IsDashaPlanet(planet, currentDasha.Mahadasha)),
                ActivatesNatalAntardashaPlanet = natalPlanetsInHouse.Any(planet => IsDashaPlanet(planet, currentDasha.Antardasha)),
                IsHouseLordMahadashaPlanet = IsDashaPlanet(houseLord, currentDasha.Mahadasha),
                IsHouseLordAntardashaPlanet = IsDashaPlanet(houseLord, currentDasha.Antardasha),
                IsRetrograde = transitPlanet.IsRetrograde
            };

            activation.InterpretationHint = BuildTransitInterpretationHint(activation);
            return activation;
        }

        private static bool IsDashaPlanet(string? planet, string? dashaPlanet)
        {
            return !string.IsNullOrWhiteSpace(planet)
                && !string.IsNullOrWhiteSpace(dashaPlanet)
                && string.Equals(planet.Trim(), dashaPlanet.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static int GetTransitPriority(TransitActivationDto activation)
        {
            var score = 0;

            if (activation.IsMahadashaPlanet)
            {
                score += 100;
            }

            if (activation.IsAntardashaPlanet)
            {
                score += 90;
            }

            if (activation.ActivatesNatalMahadashaPlanet || activation.ActivatesNatalAntardashaPlanet)
            {
                score += 80;
            }

            if (activation.IsHouseLordMahadashaPlanet || activation.IsHouseLordAntardashaPlanet)
            {
                score += 65;
            }

            score += activation.Planet switch
            {
                "Saturn" => 35,
                "Jupiter" => 32,
                "Rahu" => 30,
                "Ketu" => 30,
                "Moon" => 18,
                _ => 0
            };

            return score;
        }

        private static string BuildTransitInterpretationHint(TransitActivationDto activation)
        {
            var natalActivation = activation.NatalPlanetsInHouse.Count > 0
                ? $" activating natal {string.Join(", ", activation.NatalPlanetsInHouse)}"
                : string.Empty;
            var dashaLink = activation.IsMahadashaPlanet || activation.IsAntardashaPlanet ||
                            activation.ActivatesNatalMahadashaPlanet || activation.ActivatesNatalAntardashaPlanet ||
                            activation.IsHouseLordMahadashaPlanet || activation.IsHouseLordAntardashaPlanet
                ? " with an active Dasha link"
                : string.Empty;

            return $"{activation.Planet} is transiting your {activation.NatalHouse} house{natalActivation}, bringing focus to {activation.FocusArea}{dashaLink}.";
        }

        private static string GetTransitFocusArea(int houseNumber)
        {
            return houseNumber switch
            {
                1 => "Self, vitality and direction",
                2 => "Money, family and speech",
                3 => "Communication, courage and effort",
                4 => "Home, peace and emotional base",
                5 => "Learning, creativity and children",
                6 => "Work, health and problem solving",
                7 => "Partnerships and agreements",
                8 => "Transformation, research and shared matters",
                9 => "Fortune, dharma and guidance",
                10 => "Career, karma and public role",
                11 => "Gains, networks and ambitions",
                12 => "Rest, expenses and spiritual release",
                _ => string.Empty
            };
        }

        private static DivisionalChartDto BuildD9Chart(
            List<PlanetPosition> d1Planets,
            ZodiacSign d1AscendantSign,
            double ascendantDegreeInSign)
        {
            var navamsaCalculator = new NavamsaCalculator();
            var d9Planets = navamsaCalculator.Calculate(d1Planets);
            var d9AscendantSign = NavamsaCalculator.CalculateNavamsaSign(d1AscendantSign, ascendantDegreeInSign);

            foreach (var planet in d9Planets)
            {
                planet.House = CalculateHouseFromAscendant(planet.Sign, d9AscendantSign);
            }

            var houses = BuildDivisionalHouseDtos(d9AscendantSign, d9Planets);

            return new DivisionalChartDto
            {
                ChartType = DivisionalChartType.D9.ToString(),
                Ascendant = $"{(int)d9AscendantSign} - {d9AscendantSign}",
                Planets = d9Planets.Select(planet => MapPlanetDto(planet)).ToList(),
                Houses = houses,
                Description = "Navamsa divisional chart"
            };
        }

        private static List<DTOs.Responses.HouseDto> BuildDivisionalHouseDtos(
            ZodiacSign ascendantSign,
            List<PlanetPosition> planets)
        {
            var houses = new List<DTOs.Responses.HouseDto>();

            for (int i = 1; i <= 12; i++)
            {
                var signIndex = (((int)ascendantSign - 1) + (i - 1)) % 12;
                var houseSign = (ZodiacSign)(signIndex + 1);
                var houseOwner = HelperData.GetSignOwner(houseSign);
                var occupants = planets
                    .Where(p => (int)p.House == i)
                    .Select(p => p.Planet.ToString())
                    .ToList();

                houses.Add(new DTOs.Responses.HouseDto
                {
                    HouseNumber = i,
                    Sign = houseSign.ToString(),
                    HouseOwner = houseOwner,
                    HouseSwamy = houseOwner,
                    Occupants = occupants,
                    HasBeneficAspect = false,
                    HasMaleficAspect = false,
                    AspectedBy = new List<string>(),
                    Score = 0,
                    Factors = new List<string>(),
                    DominantThemes = new List<string>(),
                    HouseName = HouseMetadataService.GetHouseName(i),
                    LifeAreas = HouseMetadataService.GetLifeAreas(i)
                });
            }

            return houses;
        }

        private static PlanetDto MapPlanetDto(PlanetPosition p, AspectCalculationResult? aspectResult = null)
        {
            var aspectedBy = aspectResult?.PlanetAspects
                .Where(aspect => aspect.ToPlanet == p.Planet)
                .Select(aspect => aspect.FromPlanet.ToString())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();
            var hasBeneficAspect = aspectResult?.PlanetAspects
                .Any(aspect => aspect.ToPlanet == p.Planet && HelperData.IsNaturalBenefic(aspect.FromPlanet)) ?? false;
            var hasMaleficAspect = aspectResult?.PlanetAspects
                .Any(aspect => aspect.ToPlanet == p.Planet && HelperData.IsNaturalMaleficOrShadow(aspect.FromPlanet)) ?? false;

            return new PlanetDto
            {
                Planet = p.Planet.ToString(),
                Sign = p.Sign.ToString(),
                Degree = p.Degree,
                Minute = p.Minute,
                House = (int)p.House,
                Longitude = p.Longitude,
                Latitude = p.Latitude,
                IsCombust = p.IsCombust,
                IsRetrograde = p.IsRetrograde,
                DetailedStrength = p.DetailedStrength,
                AspectedBy = aspectedBy,
                HasBeneficAspect = hasBeneficAspect,
                HasMaleficAspect = hasMaleficAspect,
                ConditionTone = GetPlanetConditionTone(p, hasBeneficAspect, hasMaleficAspect),
                Nakshatra = p.Nakshatra.ToString(),
                Pada = p.Pada
            };
        }

        private static SudarshanChakraDto MapSudarshanChakraDto(SudarshanChakraResult result)
        {
            return new SudarshanChakraDto
            {
                Name = result.Name,
                Purpose = result.Purpose,
                Usage = result.Usage,
                CalculationMethod = result.CalculationMethod,
                OverallScore = result.OverallScore,
                OverallPrediction = result.OverallPrediction,
                HouseEmphasis = result.HouseEmphasis.Select(item => new SudarshanHouseEmphasisDto
                {
                    HouseNumber = item.HouseNumber,
                    Theme = item.Theme,
                    LagnaPlanetCount = item.LagnaPlanetCount,
                    MoonPlanetCount = item.MoonPlanetCount,
                    SunPlanetCount = item.SunPlanetCount,
                    TotalPlanetCount = item.TotalPlanetCount,
                    IsRepeatedAcrossReferences = item.IsRepeatedAcrossReferences,
                    Prediction = item.Prediction
                }).ToList(),
                Interpretations = result.Interpretations.Select(item => new SudarshanChakraInterpretationDto
                {
                    Area = item.Area,
                    Score = item.Score,
                    Tone = item.Tone,
                    Factors = item.Factors,
                    Prediction = item.Prediction
                }).ToList()
            };
        }
        private static string GetPlanetConditionTone(
            PlanetPosition planet,
            bool hasBeneficAspect,
            bool hasMaleficAspect)
        {
            var strength = planet.DetailedStrength ?? string.Empty;
            var isWeak = strength.Contains("Weak", StringComparison.OrdinalIgnoreCase)
                || strength.Contains("Enemy", StringComparison.OrdinalIgnoreCase)
                || strength.Contains("Debilitated", StringComparison.OrdinalIgnoreCase);
            var isStrong = strength.Contains("Own", StringComparison.OrdinalIgnoreCase)
                || strength.Contains("Exalted", StringComparison.OrdinalIgnoreCase)
                || strength.Contains("Friendly", StringComparison.OrdinalIgnoreCase);

            if (planet.IsCombust || isWeak || (hasMaleficAspect && !hasBeneficAspect))
            {
                return "Challenging";
            }

            if (planet.IsRetrograde)
            {
                return "Special";
            }

            if (isStrong || (hasBeneficAspect && !hasMaleficAspect))
            {
                return "Supportive";
            }

            if (hasBeneficAspect && hasMaleficAspect)
            {
                return "Mixed";
            }

            return "Neutral";
        }

        private static HouseNumber CalculateHouseFromAscendant(ZodiacSign sign, ZodiacSign ascendantSign)
        {
            var house = (((int)sign - (int)ascendantSign + 12) % 12) + 1;
            return (HouseNumber)house;
        }

        private static double NormalizeDegreeInSign(double longitude)
        {
            longitude %= 360.0;
            if (longitude < 0)
            {
                longitude += 360.0;
            }

            return longitude % 30.0;
        }

        [HttpPost("planet-positions")]
        public ActionResult<List<PlanetDto>> PlanetPositions([FromBody] GenerateKundaliRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            var planets = _chartService.GenerateChart(request.BirthDateTime, request.Latitude, request.Longitude);

            var result = planets.Select(p => new PlanetDto
            {
                Planet = p.Planet.ToString(),
                Sign = p.Sign.ToString(),
                Degree = p.Degree,
                Minute = p.Minute,
                House = (int)p.House,
                Longitude = p.Longitude,
                Latitude = p.Latitude,
                IsCombust = p.IsCombust,
                IsRetrograde = p.IsRetrograde,
                DetailedStrength = p.DetailedStrength,
                Nakshatra = p.Nakshatra.ToString(),
                Pada = p.Pada
            }).ToList();

            return Ok(result);
        }

        [Authorize]
        [HttpPost("sudarshan-chakra")]
        public ActionResult<SudarshanChakraDto> SudarshanChakra([FromBody] GenerateKundaliRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            TimeZoneInfo indiaZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var birthLocal = DateTime.SpecifyKind(request.BirthDateTime, DateTimeKind.Unspecified);
            var birthUtc = TimeZoneInfo.ConvertTimeToUtc(birthLocal, indiaZone);
            var planets = _chartService.GenerateChart(birthUtc, request.Latitude, request.Longitude);
            var location = new GeoLocation(request.Latitude, request.Longitude);
            var ascendantLongitude = _ephemerisProvider.GetAscendant(birthUtc, location).Longitude;
            var ascendantIndex = (int)(ascendantLongitude / 30.0);
            var ascendantSign = (ZodiacSign)((ascendantIndex % 12) + 1);
            var result = _sudarshanChakraService.Calculate(planets, ascendantSign);

            return Ok(MapSudarshanChakraDto(result));
        }
        [HttpPost("houses")]
        public ActionResult<List<DTOs.Responses.HouseDto>> Houses([FromBody] GenerateKundaliRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            var planets = _chartService.GenerateChart(request.BirthDateTime, request.Latitude, request.Longitude);

            // Placeholder: return empty house DTOs for Phase 1
            var result = Enumerable.Range(1, 12).Select(i => new DTOs.Responses.HouseDto()).ToList();

            return Ok(result);
        }

        [HttpPost("summary")]
        public ActionResult<KundaliSummaryDto> Summary([FromBody] GenerateKundaliRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            var planets = _chartService.GenerateChart(request.BirthDateTime, request.Latitude, request.Longitude);

            var dto = new KundaliSummaryDto();

            return Ok(dto);
        }

        [HttpPost("score")]
        public ActionResult<OverallScoreDto> Score([FromBody] GenerateKundaliRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            var planets = _chartService.GenerateChart(request.BirthDateTime, request.Latitude, request.Longitude);

            // Phase 1: lightweight aggregate using planet base scores
            var planetScores = _planetScoreService.CalculateMany(planets);
            int avgPlanetScore = (int)Math.Round(planetScores.Average(p => p.Score));

            var overall = new OverallScoreDto();

            return Ok(overall);
        }


    }
}








