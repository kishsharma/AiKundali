using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Domain.Enums;

namespace KundaliEngine.API.Services.Muhurtha.GrahaPravesha;

public interface IGrahaPraveshaBirthChartEvaluator
{
    IReadOnlyList<GrahaPraveshaEvidence> Evaluate(
        DateTime birthLocal,
        string birthTimezone,
        double birthLatitude,
        double birthLongitude,
        DateTimeOffset eventInstant,
        double eventLatitude,
        double eventLongitude,
        out int scoreDelta);
}

public sealed class ChartGrahaPraveshaBirthEvaluator : IGrahaPraveshaBirthChartEvaluator
{
    private readonly ChartService _chartService;
    private readonly IEphemerisProvider _ephemerisProvider;

    public ChartGrahaPraveshaBirthEvaluator(ChartService chartService, IEphemerisProvider ephemerisProvider)
    {
        _chartService = chartService;
        _ephemerisProvider = ephemerisProvider;
    }

    public IReadOnlyList<GrahaPraveshaEvidence> Evaluate(
        DateTime birthLocal,
        string birthTimezone,
        double birthLatitude,
        double birthLongitude,
        DateTimeOffset eventInstant,
        double eventLatitude,
        double eventLongitude,
        out int scoreDelta)
    {
        var birthOffset = GrahaPraveshaTime.GetOffset(birthTimezone, DateOnly.FromDateTime(birthLocal));
        var birthUtc = DateTime.SpecifyKind(birthLocal - birthOffset, DateTimeKind.Utc);
        var natal = _chartService.GenerateChart(birthUtc, birthLatitude, birthLongitude);
        var natalMoon = natal.First(planet => planet.Planet == Planet.Moon);
        var eventMoon = _ephemerisProvider.GetPlanet(
            Planet.Moon,
            DateTime.SpecifyKind(eventInstant.UtcDateTime, DateTimeKind.Utc),
            new GeoLocation(eventLatitude, eventLongitude));
        var tara = GrahaPraveshaPlanetaryScorer.EvaluateTara(natalMoon.Nakshatra, eventMoon.Nakshatra);
        var chandra = GrahaPraveshaPlanetaryScorer.EvaluateChandra(natalMoon.Sign, eventMoon.Sign);
        var eventChart = _chartService.GenerateChart(
            DateTime.SpecifyKind(eventInstant.UtcDateTime, DateTimeKind.Utc),
            eventLatitude,
            eventLongitude);
        var houseImpact = GrahaPraveshaPlanetaryScorer.ScoreEventHouses(
            eventChart.Select(planet => (planet.Planet, (int)planet.House)));

        scoreDelta = tara.ScoreImpact + chandra.ScoreImpact + houseImpact;
        return
        [
            new GrahaPraveshaEvidence
            {
                RuleId = "GP_TARA_BALA",
                Category = "PLANETARY",
                Input = tara.Name,
                Result = tara.Warning ? "WARNING" : "PASS",
                Severity = tara.Warning ? "SOFT" : "INFO",
                Message = $"Tara Bala is {tara.Name}. This is a soft factor and does not hard-reject Graha Pravesha.",
                Source = "Existing Tara Bala calculation",
                ScoreImpact = tara.ScoreImpact
            },
            new GrahaPraveshaEvidence
            {
                RuleId = "GP_CHANDRA_BALA",
                Category = "PLANETARY",
                Input = chandra.Position.ToString(),
                Result = chandra.Warning ? "WARNING" : "PASS",
                Severity = chandra.Warning ? "SOFT" : "INFO",
                Message = $"Muhurtha Moon is in position {chandra.Position} from the natal Moon.",
                Source = "Existing Chandra Bala calculation",
                ScoreImpact = chandra.ScoreImpact
            },
            new GrahaPraveshaEvidence
            {
                RuleId = "GP_EVENT_HOUSES",
                Category = "PLANETARY",
                Input = "4,8,11",
                Result = houseImpact < 0 ? "WARNING" : "PASS",
                Severity = houseImpact < 0 ? "SOFT" : "INFO",
                Message = "Benefics and malefics in Graha Pravesha houses 4, 8, and 11 were scored as a soft factor.",
                Source = "event-types.json GruhaPravesha importantHouses",
                ScoreImpact = houseImpact
            }
        ];
    }
}
