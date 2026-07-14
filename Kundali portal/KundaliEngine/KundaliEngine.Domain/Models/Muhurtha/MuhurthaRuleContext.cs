using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models.Muhurtha;

public class MuhurthaRuleContext
{
    public MuhurthaRequest Request { get; set; } = new();

    public MuhurthaEventConfiguration EventConfiguration { get; set; } = new();

    public MuhurthaRegionalProfile RegionalProfile { get; set; } = new();

    public IReadOnlyList<MuhurthaRuleConfiguration> RuleConfigurations { get; set; } = Array.Empty<MuhurthaRuleConfiguration>();

    public bool HasHoroscope => Request.Horoscope?.BirthDateTime is not null
        && Request.Horoscope.Latitude is not null
        && Request.Horoscope.Longitude is not null;

    public bool ShouldEvaluatePersonalHoroscope =>
        Request.Filters.CheckPersonalHoroscope && HasHoroscope;
}

