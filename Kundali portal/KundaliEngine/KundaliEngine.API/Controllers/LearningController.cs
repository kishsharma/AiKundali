using KundaliEngine.API.Services.Learning;
using KundaliEngine.Domain.Rules;
using KundaliEngine.KnowledgeBase.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;

namespace KundaliEngine.API.Controllers;

[ApiController]
[Authorize]
[Route("api/learning")]
public sealed class LearningController : ControllerBase
{
    private readonly ILearningContentService _learningContentService;
    private readonly ILogger<LearningController> _logger;
    private readonly YogaMetadataService _yogaMetadataService;
    private readonly DoshaMetadataService _doshaMetadataService;
    private readonly DivisionalChartMetadataService _divisionalChartMetadataService;

    public LearningController(
        ILearningContentService learningContentService,
        ILogger<LearningController> logger,
        YogaMetadataService yogaMetadataService,
        DoshaMetadataService doshaMetadataService,
        DivisionalChartMetadataService divisionalChartMetadataService)
    {
        _learningContentService = learningContentService;
        _logger = logger;
        _yogaMetadataService = yogaMetadataService;
        _doshaMetadataService = doshaMetadataService;
        _divisionalChartMetadataService = divisionalChartMetadataService;
    }

    [HttpGet("levels")]
    public async Task<IActionResult> GetLevels(CancellationToken cancellationToken)
    {
        var levels = await _learningContentService.GetLevelsAsync(cancellationToken);
        return Ok(levels);
    }

    [HttpGet("levels/{levelCode}")]
    public async Task<IActionResult> GetLevel(string levelCode, CancellationToken cancellationToken)
    {
        var level = await _learningContentService.GetLevelAsync(levelCode, cancellationToken);
        return level is null
            ? NotFound(new { message = $"Learning level '{levelCode}' was not found." })
            : Ok(level);
    }

    [HttpGet("gyan-kosha")]
    public async Task<IActionResult> GetKnowledgeMap([FromQuery] string? language, CancellationToken cancellationToken)
    {
        var map = await _learningContentService.GetKnowledgeMapAsync(language, cancellationToken);
        return map is null
            ? NotFound(new { message = "Gyan Kosha knowledge map was not found." })
            : Ok(map);
    }

    [HttpGet("gyan-kosha/details")]
    public IActionResult GetKnowledgeReferenceDetails()
    {
        var predictionAreas = _divisionalChartMetadataService.GetAreas()
            .ToDictionary(item => item.Key, item => item.Value);

        return Ok(new
        {
            yogas = _yogaMetadataService.GetAllYogas()
                .OrderBy(item => item.Category)
                .ThenBy(item => item.Name),
            doshas = _doshaMetadataService.GetAllDoshas()
                .OrderBy(item => item.Category)
                .ThenBy(item => item.Name),
            divisionalCharts = _divisionalChartMetadataService.GetCharts()
                .OrderBy(item => item.Division),
            predictionAreas,
            rashis = GetRashiReferences(),
            nakshatras = GetNakshatraReferences(),
            grahas = GetGrahaReferences(),
            vimshottari = DashaRules.VimshottariOrder.Select((item, index) => new
            {
                order = index + 1,
                planet = item.Planet.ToString(),
                years = item.Years
            }),
            vimshottariTotalYears = DashaRules.VimshottariOrder.Sum(item => item.Years)
        });
    }

    private static string DataPath(params string[] parts) => Path.Combine(AppContext.BaseDirectory, "Data", Path.Combine(parts));

    private static string Text(JsonNode? node, string property) => NodeText(node?[property]);

    private static string NodeText(JsonNode? node)
    {
        if (node is null) return string.Empty;
        if (node is JsonValue value && value.TryGetValue<string>(out var text)) return text;
        return node is JsonObject or JsonArray ? node.ToJsonString() : node.ToString();
    }

    private static string[] TextArray(JsonNode? node, string property) =>
        node?[property] is JsonArray values ? values.Select(NodeText).Where(value => value.Length > 0).ToArray() : Array.Empty<string>();

    private static object[] GetNakshatraReferences()
    {
        var files = Directory.GetFiles(DataPath("Nakshatra"), "*.json");
        return files.Select(file => JsonNode.Parse(System.IO.File.ReadAllText(file)))
            .Where(item => item is not null)
            .OrderBy(item => item!["id"]?.GetValue<int>() ?? int.MaxValue)
            .Where(item => (item!["id"]?.GetValue<int>() ?? 0) <= 27)
            .Select(item =>
            {
                var padas = item!["padas"] as JsonArray ?? new JsonArray();
                return (object)new
                {
                    id = item["id"]?.GetValue<int>() ?? 0,
                    name = Text(item, "name"),
                    sanskritName = Text(item, "sanskritName"),
                    lord = Text(item, "lord"),
                    rashi = Text(item, "rashi"),
                    deity = Text(item, "deity"),
                    symbol = Text(item, "symbol"),
                    gana = Text(item, "gana"),
                    yoni = Text(item, "yoni"),
                    nadi = Text(item, "nadi"),
                    varna = Text(item, "varna"),
                    vashya = Text(item, "vashya"),
                    animalSymbol = Text(item, "animalSymbol"),
                    guna = Text(item, "guna"),
                    tatva = Text(item, "tatva"),
                    gender = Text(item, "gender"),
                    motivation = Text(item, "motivation"),
                    nature = Text(item, "nature"),
                    direction = Text(item, "direction"),
                    fastingDay = Text(item, "fastingDay"),
                    bodyParts = TextArray(item, "bodyParts"),
                    positiveTraits = TextArray(item, "positiveTraits"),
                    negativeTraits = TextArray(item, "negativeTraits"),
                    strengths = TextArray(item, "strengths"),
                    weaknesses = TextArray(item, "weaknesses"),
                    dosha = item["dosha"] is JsonObject doshaNode ? new
                    {
                        vata = Text(doshaNode, "vata"), pitta = Text(doshaNode, "pitta"), kapha = Text(doshaNode, "kapha")
                    } : null,
                    padas = padas.Select(pada => new
                    {
                        number = pada?["number"]?.GetValue<int>() ?? 0,
                        rashiId = pada?["rashiId"]?.GetValue<int>() ?? 0,
                        startDegree = Text(pada, "startDegree"),
                        endDegree = Text(pada, "endDegree"),
                        navamsa = Text(pada, "navamsa"),
                        syllable = Text(pada, "syllable"),
                        akshara = Text(pada, "akshara")
                    }).ToArray()
                };
            }).ToArray();
    }

    private static object[] GetRashiReferences()
    {
        var nakshatraFiles = Directory.GetFiles(DataPath("Nakshatra"), "*.json");
        var allPadas = nakshatraFiles.Select(file => JsonNode.Parse(System.IO.File.ReadAllText(file)))
            .Where(item => item is not null && (item["id"]?.GetValue<int>() ?? 0) <= 27)
            .SelectMany(item => (item!["padas"] as JsonArray ?? new JsonArray()).Select(pada => new
            {
                nakshatraId = item["id"]?.GetValue<int>() ?? 0,
                nakshatra = Text(item, "name"),
                number = pada?["number"]?.GetValue<int>() ?? 0,
                rashiId = pada?["rashiId"]?.GetValue<int>() ?? 0,
                akshara = Text(pada, "akshara"),
                syllable = Text(pada, "syllable"),
                startDegree = Text(pada, "startDegree"),
                endDegree = Text(pada, "endDegree"),
                navamsa = Text(pada, "navamsa")
            })).ToArray();

        return Directory.GetFiles(DataPath("Rashi"), "*.json")
            .Select(file => JsonNode.Parse(System.IO.File.ReadAllText(file)))
            .Where(item => item is not null)
            .OrderBy(item => item!["id"]?.GetValue<int>() ?? int.MaxValue)
            .Select(item =>
            {
                var id = item!["id"]?.GetValue<int>() ?? 0;
                var charans = allPadas.Where(pada => pada.rashiId == id)
                    .OrderBy(pada => pada.nakshatraId).ThenBy(pada => pada.number).ToArray();
                return (object)new
                {
                    id,
                    name = Text(item, "name"),
                    englishName = Text(item, "englishName"),
                    symbol = Text(item, "symbol"),
                    element = Text(item, "element"),
                    modality = Text(item, "modality"),
                    gender = Text(item, "gender"),
                    nature = Text(item, "nature"),
                    ruler = Text(item, "ruler"),
                    direction = Text(item, "direction"),
                    bodyPart = Text(item, "bodyPart"),
                    quality = Text(item, "quality"),
                    personality = Text(item, "personality"),
                    relationships = Text(item, "relationships"),
                    health = Text(item, "health"),
                    strengths = TextArray(item, "strengths"),
                    weaknesses = TextArray(item, "weaknesses"),
                    career = TextArray(item, "career"),
                    color = Text(item, "color"),
                    gemstone = Text(item, "gemstone"),
                    metal = Text(item, "metal"),
                    luckyNumbers = item["luckyNumbers"]?.ToJsonString(),
                    favorableDirection = Text(item, "favorableDirection"),
                    favorableDay = Text(item, "favorableDay"),
                    charans
                };
            }).ToArray();
    }

    private static object[] GetGrahaReferences()
    {
        var root = JsonNode.Parse(System.IO.File.ReadAllText(DataPath("Planets", "planets.json")));
        return (root?["planets"] as JsonArray ?? new JsonArray()).Select(planet => (object)new
        {
            name = Text(planet, "name"),
            sanskritName = Text(planet, "sanskritName"),
            nature = Text(planet, "nature"),
            classification = Text(planet, "classification"),
            gender = Text(planet, "gender"),
            tattva = Text(planet, "tattva"),
            guna = Text(planet, "guna"),
            karakatva = TextArray(planet, "karakatva"),
            ownSigns = TextArray(planet, "ownSigns"),
            exaltation = Text(planet, "exaltation"),
            debilitation = Text(planet, "debilitation"),
            moolatrikona = Text(planet, "moolatrikona"),
            friends = TextArray(planet, "friends"),
            enemies = TextArray(planet, "enemies"),
            neutral = TextArray(planet, "neutral")
        }).ToArray();
    }

    [HttpGet("sessions/{sessionGuid:guid}")]
    public async Task<IActionResult> GetSession(
        Guid sessionGuid,
        [FromQuery] string? language,
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";

        try
        {
            var response = await _learningContentService.GetSessionAsync(sessionGuid, language, cancellationToken);
            return response is null
                ? NotFound(new { message = "Learning session was not found." })
                : Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Learning session {SessionGuid} could not be returned.", sessionGuid);
            return BadRequest(new { message = ex.Message });
        }
    }
}
