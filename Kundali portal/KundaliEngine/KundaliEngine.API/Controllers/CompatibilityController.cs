using KundaliEngine.API.DTOs.Requests;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Services;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Compatibility;
using KundaliEngine.Astrology.Services.House;
using KundaliEngine.Astrology.Services.KundaliDosha;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.Astrology.Services.Scoring;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.API.Helper_functions;
using KundaliEngine.KnowledgeBase.Services;
using KundaliEngine.Infrastructure.Data;
using KundaliEngine.Domain.Entities;
using KundaliEngine.API.Services.Audit;
using KundaliEngine.API.Services.Entitlements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompatibilityController : ControllerBase
    {
        private readonly ChartService _chartService;
        private readonly PanchangService _panchangService;
        private readonly AspectService _aspectService;
        private readonly CompatibilityScoreService _compatibilityService;
        private readonly CompatibilityDoshaAnalysisService _compatibilityDoshaAnalysisService;
        private readonly DoshaDetectionService _doshaDetectionService;
        private readonly DoshaMetadataService _doshaMetadataService;
        private readonly HouseScoreService _houseScoreservice;
        private readonly NakshatraMetadataService _nakshatraMetadataService;
        private readonly IEphemerisProvider _ephemerisProvider;
        private readonly KundaliDbContext _dbContext;
        private readonly IAuditLogService _auditLogService;
        private readonly IEntitlementService _entitlementService;
        public CompatibilityController(
            ChartService chartService,
            PanchangService panchangService,
            AspectService aspectService,
            CompatibilityScoreService compatibilityService,
            CompatibilityDoshaAnalysisService compatibilityDoshaAnalysisService,
            DoshaDetectionService doshaDetectionService,
            DoshaMetadataService doshaMetadataService,
            HouseScoreService houseScoreservice,
            NakshatraMetadataService nakshatraMetadataService,
            IEphemerisProvider ephemerisProvider,
            KundaliDbContext dbContext,
            IAuditLogService auditLogService,
            IEntitlementService entitlementService)
        {
            _chartService = chartService;
            _panchangService = panchangService;
            _aspectService = aspectService;
            _compatibilityService = compatibilityService;
            _compatibilityDoshaAnalysisService = compatibilityDoshaAnalysisService;
            _doshaDetectionService = doshaDetectionService;
            _doshaMetadataService = doshaMetadataService;
            _houseScoreservice = houseScoreservice;
            _nakshatraMetadataService = nakshatraMetadataService;
            _ephemerisProvider = ephemerisProvider;
            _dbContext = dbContext;
            _auditLogService = auditLogService;
            _entitlementService = entitlementService;
        }

        [HttpPost("match")]
        public async Task<ActionResult<CompatibilityResult>> Match([FromBody] CompatibilityRequestDto request)
        {
            if (request == null)
                return BadRequest("Request is required");

            if (request.PersonA == null || request.PersonB == null)
                return BadRequest("Both PersonA and PersonB are required");

            try
            {
                var profileA = BuildProfile(request.PersonA, request.PersonA.Name ?? "Person A");
                var profileB = BuildProfile(request.PersonB, request.PersonB.Name ?? "Person B");

                var result = _compatibilityService.CalculateCompatibility(profileA, profileB);
                var personADoshas = _doshaDetectionService.DetectDoshas(
                    profileA.Chart.PlanetPositions,
                    profileA.Chart.Houses);
                var personBDoshas = _doshaDetectionService.DetectDoshas(
                    profileB.Chart.PlanetPositions,
                    profileB.Chart.Houses);

                result.DoshaAnalysis = _compatibilityDoshaAnalysisService.Analyze(
                    profileA.Name,
                    profileB.Name,
                    personADoshas,
                    personBDoshas,
                    _doshaMetadataService);

                if (!await IsCurrentUserPaidAsync())
                {
                    StripCompatibilityDetails(result);
                }

                return Ok(result);
            }
            catch (ArgumentException aex)
            {
                return BadRequest(aex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Compatibility calculation failed: {ex.Message}");
            }
        }


        [Authorize]
        [HttpPost("save")]
        public async Task<ActionResult<SavedCompatibilityDetailDto>> Save([FromBody] SaveCompatibilityRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request is required");
            }

            if (request.Request?.PersonA == null || request.Request.PersonB == null)
            {
                return BadRequest("Both PersonA and PersonB are required");
            }

            if (request.Result.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                return BadRequest("Compatibility result is required before saving");
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId.Value);
            if (user == null || !user.IsActive)
            {
                return Unauthorized();
            }

            var savedMatchCount = await _dbContext.SavedCompatibilities.CountAsync(x => x.UserId == userId.Value);
            var matchSaveLimit = _entitlementService.GetMatchSaveLimit(user);
            if (savedMatchCount >= matchSaveLimit)
            {
                return BadRequest(new
                {
                    message = $"Saved match limit reached. This plan can save up to {matchSaveLimit} matches.",
                    matchSaveLimit
                });
            }

            var personA = request.Request.PersonA;
            var personB = request.Request.PersonB;

            if (string.IsNullOrWhiteSpace(personA.Name) || string.IsNullOrWhiteSpace(personB.Name))
            {
                return BadRequest("Both names are required");
            }

            if (string.IsNullOrWhiteSpace(personA.BirthPlace) || string.IsNullOrWhiteSpace(personB.BirthPlace))
            {
                return BadRequest("Both birth places are required");
            }

            var savedCompatibility = new SavedCompatibility
            {
                UserId = userId.Value,
                PersonAName = personA.Name.Trim(),
                PersonAGender = string.IsNullOrWhiteSpace(personA.Gender) ? null : personA.Gender.Trim(),
                PersonADateOfBirth = DateOnly.FromDateTime(personA.BirthDateTime),
                PersonATimeOfBirth = TimeOnly.FromDateTime(personA.BirthDateTime),
                PersonABirthPlace = personA.BirthPlace.Trim(),
                PersonALatitude = Convert.ToDecimal(personA.Latitude),
                PersonALongitude = Convert.ToDecimal(personA.Longitude),
                PersonBName = personB.Name.Trim(),
                PersonBGender = string.IsNullOrWhiteSpace(personB.Gender) ? null : personB.Gender.Trim(),
                PersonBDateOfBirth = DateOnly.FromDateTime(personB.BirthDateTime),
                PersonBTimeOfBirth = TimeOnly.FromDateTime(personB.BirthDateTime),
                PersonBBirthPlace = personB.BirthPlace.Trim(),
                PersonBLatitude = Convert.ToDecimal(personB.Latitude),
                PersonBLongitude = Convert.ToDecimal(personB.Longitude),
                OverallCompatibilityScore = TryGetInt(request.Result, "overallCompatibilityScore"),
                AshtakootaScore = TryGetDecimal(request.Result, "ashtakoota", "totalScore"),
                RequestJson = JsonSerializer.Serialize(request.Request),
                ResultJson = request.Result.GetRawText(),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.SavedCompatibilities.Add(savedCompatibility);
            await _dbContext.SaveChangesAsync();
            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "CompatibilitySaved",
                Category = "DataChange",
                EntityType = "SavedCompatibility",
                EntityId = savedCompatibility.Id.ToString(),
                Description = $"Compatibility saved for {savedCompatibility.PersonAName} and {savedCompatibility.PersonBName}.",
                Status = "Success"
            });

            return CreatedAtAction(nameof(GetById), new { id = savedCompatibility.Id }, MapSavedCompatibilityDetail(savedCompatibility));
        }

        [Authorize]
        [HttpGet("history")]
        public async Task<ActionResult<List<SavedCompatibilityListItemDto>>> GetHistory()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var items = await _dbContext.SavedCompatibilities
                .AsNoTracking()
                .Where(item => item.UserId == userId.Value)
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new SavedCompatibilityListItemDto
                {
                    Id = item.Id,
                    PersonAName = item.PersonAName,
                    PersonADateOfBirth = item.PersonADateOfBirth,
                    PersonABirthPlace = item.PersonABirthPlace,
                    PersonBName = item.PersonBName,
                    PersonBDateOfBirth = item.PersonBDateOfBirth,
                    PersonBBirthPlace = item.PersonBBirthPlace,
                    OverallCompatibilityScore = item.OverallCompatibilityScore,
                    AshtakootaScore = item.AshtakootaScore,
                    CreatedAt = item.CreatedAt,
                    DisplayName = item.PersonAName + " & " + item.PersonBName + " - " + item.OverallCompatibilityScore + "%"
                })
                .ToListAsync();

            return Ok(items);
        }

        [Authorize]
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<SavedCompatibilityDetailDto>> GetById(Guid id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var savedCompatibility = await _dbContext.SavedCompatibilities
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId.Value);

            if (savedCompatibility == null)
            {
                return NotFound();
            }

            return Ok(MapSavedCompatibilityDetail(savedCompatibility));
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

            var savedCompatibility = await _dbContext.SavedCompatibilities
                .FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId.Value);

            if (savedCompatibility == null)
            {
                return NotFound();
            }

            var notes = await _dbContext.PortalNotes
                .Where(note => note.UserId == userId.Value
                    && note.ContextType == "Compatibility"
                    && note.ContextId == id)
                .ToListAsync();

            var voiceRecordings = await _dbContext.PortalNoteVoiceRecordings
                .Where(recording => recording.UserId == userId.Value
                    && recording.ContextType == "Compatibility"
                    && recording.ContextId == id)
                .ToListAsync();

            _dbContext.PortalNotes.RemoveRange(notes);
            _dbContext.PortalNoteVoiceRecordings.RemoveRange(voiceRecordings);
            _dbContext.SavedCompatibilities.Remove(savedCompatibility);
            await _dbContext.SaveChangesAsync();
            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "CompatibilityDeleted",
                Category = "DataChange",
                EntityType = "SavedCompatibility",
                EntityId = id.ToString(),
                Description = $"Compatibility deleted for {savedCompatibility.PersonAName} and {savedCompatibility.PersonBName}.",
                Status = "Success"
            });

            return NoContent();
        }
        private CompatibilityProfile BuildProfile(GenerateKundaliRequest req, string name)
        {
            if (req == null)
                throw new ArgumentNullException(nameof(req));
            // Generate planet positions (Kundali chart)
            TimeZoneInfo indiaZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

            // Assume the input is in IST (India Standard Time)
            var birthLocal = DateTime.SpecifyKind(req.BirthDateTime, DateTimeKind.Unspecified);
            req.BirthDateTime = birthLocal;
            DateTime birthUtc = TimeZoneInfo.ConvertTimeToUtc(req.BirthDateTime, indiaZone);

            req.BirthDateTime = birthUtc;
            // Chart (planet positions)
            var planetPositions = _chartService.GenerateChart(req.BirthDateTime, req.Latitude, req.Longitude);

            // Ascendant
            var location = new GeoLocation(req.Latitude, req.Longitude);
            double ascendantLongitude = _ephemerisProvider.GetAscendant(req.BirthDateTime, location).Longitude;
            int ascendantIndex = (int)(ascendantLongitude / 30.0);

            ZodiacSign ascendantSign = (ZodiacSign)((ascendantIndex % 12) + 1);


            // Houses and house scores
            var aspectResult = _aspectService.CalculateGrahaDrishti(planetPositions);
            var houseScores = _houseScoreservice.CalculateAllHouseScores(ascendantSign, planetPositions, aspectResult);
            // Compute house signs (house N sign = ascendantSign + (N-1))
            var houseDtos = new List<DTOs.Responses.HouseDto>();
            for (int i = 1; i <= 12; i++)
            {
                var houseNumber = (HouseNumber)i;
                var signIndex = (((int)ascendantSign - 1) + (i - 1)) % 12;
                var houseSign = (ZodiacSign)(signIndex + 1);
                var houseOwner =HelperData.GetSignOwner(houseSign);

                var houseAspects = aspectResult.HouseAspects
                    .Where(a => a.ToHouse == i)
                    .ToList();
                var occupants = planetPositions.Where(p => (int)p.House == i).Select(p => p.Planet.ToString()).ToList();
                var scoreResult = houseScores.First(h => (int)h.HouseNumber == i);


                houseDtos.Add(new DTOs.Responses.HouseDto
                {
                    HouseNumber = i,
                    Sign = houseSign.ToString(),
                    HouseOwner = houseOwner,
                    HouseSwamy = houseOwner,
                    Occupants = occupants,
                    HasBeneficAspect = houseAspects.Any(a =>HelperData.IsNaturalBenefic(a.FromPlanet)),
                    HasMaleficAspect = houseAspects.Any(a =>HelperData.IsNaturalMaleficOrShadow(a.FromPlanet)),
                    Score = scoreResult.Score,
                    Factors = scoreResult.Factors,
                    DominantThemes = scoreResult.DominantThemes,
                    HouseName = HouseMetadataService.GetHouseName(i),
                    LifeAreas = HouseMetadataService.GetLifeAreas(i),
                });
            }

            // Panchang
            double sunLongitude = planetPositions.First(p => p.Planet == Planet.Sun).Longitude;
            double moonLongitude = planetPositions.First(p => p.Planet == Planet.Moon).Longitude;
            var panchang = _panchangService.Calculate(req.BirthDateTime, sunLongitude, moonLongitude);
            panchang.Akshara = _nakshatraMetadataService.GetPadaAkshara(panchang.Nakshatra, panchang.Pada);

            // Aspects
            //var aspectResult = _aspectService.CalculateGrahaDrishti(planetPositions);

            // Build KundaliChart
            var kundaliChart = new KundaliChart
            {
                GeneratedAtUtc = DateTime.UtcNow,
                BirthDetails = new BirthDetails
                {
                    BirthDateTimeLocal = req.BirthDateTime,
                    Latitude = req.Latitude,
                    Longitude = req.Longitude,
                    TimeZoneOffsetHours = req.TimeZoneOffset
                },
                AscendantDegree = (int)(ascendantLongitude % 30),
                AscendantMinute = (int)((ascendantLongitude % 1) * 60),
                AscendantSign = ascendantSign,
                AscendantSignAndNumber = $"{(int)ascendantSign} - {ascendantSign}",
                PlanetPositions = planetPositions.Select(x => new PlanetPosition
                {
                    Degree = x.Degree,
                    DegreeInSign = x.DegreeInSign,
                    DetailedStrength = x.DetailedStrength,
                    House = x.House,
                    IsCombust = x.IsCombust,
                    IsRetrograde = x.IsRetrograde,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    Minute = x.Minute,
                    Nakshatra = x.Nakshatra,
                    Pada = x.Pada,
                    Planet = x.Planet,
                    PlanetName = x.Planet.ToString(),
                    Sign = x.Sign



                }
                ).ToList(),
                Aspects = aspectResult.PlanetAspects,
                Panchang = panchang,
                Houses = houseDtos.Select(h => new Domain.Models.HouseDto
                {
                    HouseNumber = (HouseNumber)h.HouseNumber,
                    Sign = Enum.Parse<ZodiacSign>(h.Sign),
                    HouseOwner = h.HouseOwner,
                    HouseSwamy = h.HouseSwamy,
                    Occupants = h.Occupants.Select(o => Enum.Parse<Planet>(o)).ToList(),
                    HasBeneficAspect = h.HasBeneficAspect,
                    HasMaleficAspect = h.HasMaleficAspect,
                    Score = h.Score,
                    Factors = h.Factors,
                    DominantThemes = h.DominantThemes,
                    HouseName = h.HouseName,
                    LifeAreas = h.LifeAreas
                }).ToList()
            };

            return new CompatibilityProfile(name, kundaliChart);
        }

        private Guid? GetCurrentUserId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claimValue, out var userId) ? userId : null;
        }

        private async Task<bool> IsCurrentUserPaidAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return false;
            }

            var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId.Value);
            return user != null && user.IsActive && _entitlementService.IsPaid(user);
        }

        private static void StripCompatibilityDetails(CompatibilityResult result)
        {
            result.Summary = string.Empty;
            result.DominantStrengths = new List<string>();
            result.DominantChallenges = new List<string>();
            result.Factors = new List<string>();

            foreach (var dimension in result.Dimensions)
            {
                dimension.Summary = string.Empty;
                dimension.Factors = new List<string>();
                dimension.ScoreBreakdown = new List<ScoreComponent>();
            }

            result.DoshaAnalysis.DoshaFactors = new List<string>();
            result.DoshaAnalysis.DoshaRemedies = new List<string>();
            result.DoshaAnalysis.ScoreBreakdown = new List<ScoreComponent>();
            result.DoshaAnalysis.Items = result.DoshaAnalysis.Items.Select(item => new CompatibilityDoshaItem
            {
                Key = item.Key,
                Name = item.Name,
                Category = item.Category,
                Status = item.Status,
                CompatibilityScore = item.CompatibilityScore,
                RiskScore = item.RiskScore,
                Severity = item.Severity,
                Tone = item.Tone
            }).ToList();
        }

        private static SavedCompatibilityDetailDto MapSavedCompatibilityDetail(SavedCompatibility savedCompatibility)
        {
            return new SavedCompatibilityDetailDto
            {
                Id = savedCompatibility.Id,
                PersonAName = savedCompatibility.PersonAName,
                PersonAGender = savedCompatibility.PersonAGender,
                PersonADateOfBirth = savedCompatibility.PersonADateOfBirth,
                PersonATimeOfBirth = savedCompatibility.PersonATimeOfBirth,
                PersonABirthPlace = savedCompatibility.PersonABirthPlace,
                PersonALatitude = savedCompatibility.PersonALatitude,
                PersonALongitude = savedCompatibility.PersonALongitude,
                PersonBName = savedCompatibility.PersonBName,
                PersonBGender = savedCompatibility.PersonBGender,
                PersonBDateOfBirth = savedCompatibility.PersonBDateOfBirth,
                PersonBTimeOfBirth = savedCompatibility.PersonBTimeOfBirth,
                PersonBBirthPlace = savedCompatibility.PersonBBirthPlace,
                PersonBLatitude = savedCompatibility.PersonBLatitude,
                PersonBLongitude = savedCompatibility.PersonBLongitude,
                OverallCompatibilityScore = savedCompatibility.OverallCompatibilityScore,
                AshtakootaScore = savedCompatibility.AshtakootaScore,
                Request = JsonSerializer.Deserialize<JsonElement>(savedCompatibility.RequestJson),
                Result = JsonSerializer.Deserialize<JsonElement>(savedCompatibility.ResultJson),
                CreatedAt = savedCompatibility.CreatedAt
            };
        }

        private static int TryGetInt(JsonElement element, string propertyName)
        {
            return element.ValueKind == JsonValueKind.Object
                && element.TryGetProperty(propertyName, out var property)
                && property.TryGetInt32(out var value)
                    ? value
                    : 0;
        }

        private static decimal? TryGetDecimal(JsonElement element, string parentPropertyName, string propertyName)
        {
            if (element.ValueKind != JsonValueKind.Object
                || !element.TryGetProperty(parentPropertyName, out var parent)
                || parent.ValueKind != JsonValueKind.Object
                || !parent.TryGetProperty(propertyName, out var property))
            {
                return null;
            }

            return property.TryGetDecimal(out var value) ? value : null;
        }
    }
}
