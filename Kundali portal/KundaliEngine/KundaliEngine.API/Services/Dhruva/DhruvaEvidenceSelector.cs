using System.Globalization;
using System.Text.Json.Nodes;

namespace KundaliEngine.API.Services.Dhruva;

public static class DhruvaEvidenceSelector
{
    private static readonly string[] Priority =
    [
        "timingWindows",
        "houses",
        "planets",
        "yogas",
        "doshas",
        "dashaPeriods",
    ];

    public static string Trim(string factsJson, int evidenceLimit)
    {
        if (string.IsNullOrWhiteSpace(factsJson))
        {
            return factsJson;
        }

        if (JsonNode.Parse(factsJson) is not JsonObject root)
        {
            return factsJson;
        }

        var limit = Math.Max(1, evidenceLimit);
        var picked = new List<(string Key, JsonNode Clone)>();
        foreach (var key in Priority)
        {
            if (root[key] is not JsonArray array)
            {
                continue;
            }

            foreach (var item in array)
            {
                if (item is null)
                {
                    continue;
                }

                picked.Add((key, item.DeepClone()));
                if (picked.Count >= limit)
                {
                    break;
                }
            }

            if (picked.Count >= limit)
            {
                break;
            }
        }

        foreach (var key in Priority)
        {
            if (root[key] is null)
            {
                continue;
            }

            var kept = picked.Where(item => item.Key == key).Select(item => item.Clone).ToArray();
            if (kept.Length == 0)
            {
                root.Remove(key);
            }
            else
            {
                root[key] = new JsonArray(kept);
            }
        }

        return root.ToJsonString();
    }

    public static string OmitPastDates(string factsJson, DateTime today)
    {
        if (string.IsNullOrWhiteSpace(factsJson) || JsonNode.Parse(factsJson) is not JsonObject root)
        {
            return factsJson;
        }

        RemovePastDates(root, today.Date);
        return root.ToJsonString();
    }

    private static void RemovePastDates(JsonNode node, DateTime today)
    {
        if (node is JsonObject obj)
        {
            var remove = new List<string>();
            foreach (var pair in obj.ToList())
            {
                if (pair.Value is JsonValue value
                    && value.TryGetValue<string>(out var text)
                    && IsPastDate(text, today))
                {
                    remove.Add(pair.Key);
                }
                else if (pair.Value is not null)
                {
                    RemovePastDates(pair.Value, today);
                }
            }

            foreach (var key in remove)
            {
                obj.Remove(key);
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                if (item is not null)
                {
                    RemovePastDates(item, today);
                }
            }
        }
    }

    private static bool IsPastDate(string text, DateTime today)
    {
        var value = text.Trim();
        return value.Length >= 10
            && DateTime.TryParseExact(value.AsSpan(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            && parsed.Date < today;
    }
}
