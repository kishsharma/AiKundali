using KundaliEngine.API.DTOs.Responses;

namespace KundaliEngine.API.Services.Horoscope;

/// <summary>
/// Phase-2 scaffolding: hierarchical TP rules are declared so v1 responses stay honest
/// about what is not yet computed, while enforcing "lower ⊆ upper" as a product rule.
/// </summary>
public static class HoroscopeTithiPraveshaGuard
{
    public static HoroscopeTithiPraveshaStatusDto BuildStatus(HoroscopeHorizon horizon)
    {
        var status = new HoroscopeTithiPraveshaStatusDto
        {
            Enabled = false,
            Status = "NotEnabled",
            Message = horizon switch
            {
                HoroscopeHorizon.Daily =>
                    "Daily Tithi Pravesha (Sun–Moon +12° from birth) and Hora-lord day ruler are not enabled yet. Day reading uses panchanga + fast gochara + soft pratyantardasha.",
                HoroscopeHorizon.Weekly =>
                    "Annual Tithi Pravesha dasha slices for the week are not enabled yet. Week reading uses MD/AD + slow/fast gochara + light panchanga.",
                HoroscopeHorizon.Monthly =>
                    "Annual Tithi Pravesha month lords are not enabled yet. Month reading uses MD/AD continuity + slow gochara (Saturn/Jupiter/nodes).",
                _ => "Tithi Pravesha charts are reserved for phase 2."
            }
        };

        return status;
    }

    public static IReadOnlyList<string> BuildLimitations(HoroscopeHorizon horizon)
    {
        var list = new List<string>
        {
            "v1 does not cast annual or daily Tithi Pravesha charts.",
            "A lower time-scale reading must not claim results the natal chart and running Vimshottari deny."
        };

        if (horizon == HoroscopeHorizon.Daily)
        {
            list.Add("Natal pratyantardasha is used only as soft corroboration (birth-time sensitive).");
        }

        if (horizon is HoroscopeHorizon.Weekly or HoroscopeHorizon.Monthly)
        {
            list.Add("Prefer annual TP once available instead of subdividing natal dasha into week/month precision.");
        }

        return list;
    }

    /// <summary>
    /// Soft clamp: if natal/dasha tone is Challenging, daily Supportive claims are capped to Mixed
    /// until TP hierarchy can prove otherwise.
    /// </summary>
    public static string ApplyHierarchicalToneLimit(string proposedTone, string natalDashaTone)
    {
        if (string.Equals(natalDashaTone, "Challenging", StringComparison.OrdinalIgnoreCase)
            && string.Equals(proposedTone, "Supportive", StringComparison.OrdinalIgnoreCase))
        {
            return "Mixed";
        }

        return proposedTone;
    }
}
