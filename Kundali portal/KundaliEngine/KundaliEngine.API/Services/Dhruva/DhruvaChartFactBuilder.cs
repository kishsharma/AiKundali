using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace KundaliEngine.API.Services.Dhruva;

public static class DhruvaChartFactBuilder
{
    private static readonly TopicRule[] Topics =
    [
        new("marriage", ["marriage", "marry", "spouse", "wedding", "partner"], [7], ["Venus", "Jupiter"]),
        new("career", ["career", "job", "profession", "promotion", "work"], [10], ["Sun", "Saturn", "Mercury"]),
        new("income", ["income", "gain", "gains"], [11], ["Jupiter", "Venus", "Mercury"]),
        new("savings", ["saving", "savings", "wealth"], [2], ["Jupiter", "Venus", "Mercury"]),
        new("property", ["property", "home", "land"], [4], ["Mars", "Moon"]),
        new("study", ["study", "education", "exam"], [5, 4], ["Mercury", "Jupiter"]),
        new("children", ["child", "children", "baby"], [5], ["Jupiter"]),
        new("family", ["family"], [4, 2], ["Moon", "Jupiter"]),
        new("health", ["health", "vitality"], [1], ["Sun", "Moon"]),
    ];

    public static string Build(JsonElement chart, string mode, string? question)
    {
        var facts = new JsonObject();
        var native = ReadNative(chart);
        var normalizedMode = (mode ?? "qa").Trim().ToLowerInvariant();
        if (normalizedMode != "janma_story")
        {
            native.Remove("birthDate");
            native.Remove("birthTime");
            native.Remove("birthPlace");
        }

        if (native.Count > 0)
        {
            facts["native"] = native;
        }

        Set(facts, "lagna", ReadSummaryText(chart, "ascendant", "lagna"));
        Set(facts, "moonSign", ReadSummaryText(chart, "moonSign", "moon"));
        Set(facts, "sunSign", ReadSummaryText(chart, "sunSign", "sun"));
        var currentDasha = ReadCurrentDasha(chart);
        if (currentDasha.Count > 0)
        {
            facts["currentDasha"] = currentDasha;
        }

        if (normalizedMode == "janma_story")
        {
            Set(facts, "planets", ReadPlanets(chart, null));
            Set(facts, "yogas", ReadResults(chart, "yogas", null));
            Set(facts, "doshas", ReadResults(chart, "doshas", null));
        }
        else if (normalizedMode == "dasha_focus")
        {
            Set(facts, "mahadasha", ReadOpenMahadashas(chart));
        }
        else
        {
                var topic = MatchTopic(question);
                if (topic is null)
                {
                    Set(facts, "planets", ReadPlanets(chart, null));
                    Set(facts, "houses", ReadHouses(chart, null, scoresOnly: true));
                }
                else
                {
                    var planetNames = topic.Planets
                        .Concat(ReadHousePlanetNames(chart, topic.Houses))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    facts["topic"] = topic.Name;
                    Set(facts, "planets", ReadPlanets(chart, planetNames));
                    Set(facts, "houses", ReadHouses(chart, topic.Houses, scoresOnly: false));
                    Set(facts, "yogas", ReadResults(chart, "yogas", topic, planetNames));
                    Set(facts, "doshas", ReadResults(chart, "doshas", topic, planetNames));
                }
            }

        AttachDashaInfluence(chart, facts, question);
        return facts.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    private static TopicRule? MatchTopic(string? question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return null;
        }

        var text = question.ToLowerInvariant();
        foreach (var topic in Topics)
        {
            if (topic.Keywords.Any(keyword => text.Contains(keyword, StringComparison.Ordinal)))
            {
                return topic;
            }
        }

        return null;
    }

    private static JsonObject ReadNative(JsonElement chart)
    {
        var native = Find(chart, "native");
        var nativeFacts = new JsonObject();
        Set(nativeFacts, "name", FirstText(chart, native, "name", "fullName", "personName"));
        Set(nativeFacts, "birthDate", FirstText(chart, native, "birthDate", "dateOfBirth"));
        Set(nativeFacts, "birthTime", FirstText(chart, native, "birthTime", "timeOfBirth"));
        Set(nativeFacts, "birthPlace", FirstText(chart, native, "birthPlace", "place"));
        return nativeFacts;
    }

    private static string? FirstText(JsonElement chart, JsonElement? native, params string[] names)
    {
        var fromNative = native is { } nativeElement ? ReadText(nativeElement, names) : null;
        if (!string.IsNullOrWhiteSpace(fromNative))
        {
            return fromNative;
        }

        var fromSummary = ReadSummaryText(chart, names);
        if (!string.IsNullOrWhiteSpace(fromSummary))
        {
            return fromSummary;
        }

        return ReadText(chart, names);
    }

    private static string? ReadSummaryText(JsonElement chart, params string[] names)
    {
        var summary = Find(chart, "summary");
        return summary is { } summaryElement ? ReadText(summaryElement, names) : null;
    }

    private static JsonObject ReadCurrentDasha(JsonElement chart)
    {
        var current = Find(chart, "currentDasha");
        var summary = Find(chart, "summary");
        var dasha = new JsonObject();
        Set(dasha, "mahadasha", ReadText(current, "mahadasha", "currentMahadasha") ?? ReadText(summary, "currentMahadasha"));
        Set(dasha, "antardasha", ReadText(current, "antardasha", "currentAntardasha") ?? ReadText(summary, "currentAntardasha"));
        Set(dasha, "mahadashaStart", ReadDateText(current, "startDate", "start"));
        Set(dasha, "mahadashaEnd", ReadDateText(current, "endDate", "end"));
        Set(dasha, "antardashaStart", ReadDateText(current, "antardashaStartDate"));
        Set(dasha, "antardashaEnd", ReadDateText(current, "antardashaEndDate"));
        return dasha;
    }

    private static JsonArray ReadPlanets(JsonElement chart, IReadOnlyCollection<string>? allowedNames)
    {
        var planets = new JsonArray();
        foreach (var planet in Enumerate(Find(chart, "planets")))
        {
            var name = ReadText(planet, "planet", "name");
            if (allowedNames is not null && !allowedNames.Any(allowed => string.Equals(allowed, name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var item = new JsonObject();
            Set(item, "planet", name);
            Set(item, "sign", ReadText(planet, "sign"));
            SetNode(item, "house", ReadNumberOrText(planet, "house", "houseNumber"));
            SetNode(item, "degree", ReadNumberOrText(planet, "degree"));
            SetNode(item, "minute", ReadNumberOrText(planet, "minute"));
            Set(item, "nakshatra", ReadText(planet, "nakshatra"));
            SetNode(item, "pada", ReadNumberOrText(planet, "pada"));
            Set(item, "strength", ReadText(planet, "detailedStrength", "strength"));
            Set(item, "condition", ReadText(planet, "conditionTone", "condition"));
            if (ReadBool(planet, "isRetrograde") == true)
            {
                Set(item, "isRetrograde", true);
            }

            if (ReadBool(planet, "isCombust") == true)
            {
                Set(item, "isCombust", true);
            }

            AddAspects(item, planet);
            planets.Add(item);
        }

        return planets;
    }

    private static JsonArray ReadHouses(JsonElement chart, IReadOnlyCollection<int>? allowedHouses, bool scoresOnly)
    {
        var houses = new JsonArray();
        foreach (var house in Enumerate(Find(chart, "houses")))
        {
            var number = ReadInt(house, "houseNumber", "house");
            if (allowedHouses is not null && (number is null || !allowedHouses.Contains(number.Value)))
            {
                continue;
            }

            var item = new JsonObject();
            Set(item, "house", number);
            Set(item, "sign", ReadText(house, "sign"));
            SetNode(item, "score", ReadNumberOrText(house, "score"));
            if (!scoresOnly)
            {
                Set(item, "houseName", ReadText(house, "houseName", "name"));
                Set(item, "lifeAreas", ReadTextOrList(house, "lifeAreas"));
                Set(item, "lord", ReadText(house, "houseOwner", "houseSwamy", "lord"));
                Set(item, "occupants", ReadTextOrList(house, "occupants"));
                AddAspects(item, house);
            }

            houses.Add(item);
        }

        return houses;
    }

    private static JsonArray ReadResults(JsonElement chart, string propertyName, TopicRule? topic, IReadOnlyCollection<string>? planetNames = null)
    {
        var results = new JsonArray();
        foreach (var item in Enumerate(Find(chart, propertyName)))
        {
            var name = ReadText(item, "name");
            var description = FirstSentence(ReadText(item, "description", "summary"));
            if (topic is not null && !MatchesTopic(item, name, description, topic, planetNames))
            {
                continue;
            }

            var result = new JsonObject();
            Set(result, "name", name);
            Set(result, "description", description);
            results.Add(result);
        }

        return results;
    }

    private static IEnumerable<string> ReadHousePlanetNames(JsonElement chart, IReadOnlyCollection<int> houses)
    {
        foreach (var house in Enumerate(Find(chart, "houses")))
        {
            var number = ReadInt(house, "houseNumber", "house");
            if (number is null || !houses.Contains(number.Value))
            {
                continue;
            }

            foreach (var name in new[] { ReadText(house, "houseOwner", "houseSwamy", "lord") })
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    yield return name;
                }
            }

            foreach (var occupant in ReadList(house, "occupants"))
            {
                yield return occupant;
            }

            foreach (var aspect in ReadList(house, "aspectedBy"))
            {
                yield return aspect;
            }
        }
    }

    private static readonly HashSet<string> BeneficPlanets = new(StringComparer.OrdinalIgnoreCase)
    {
        "Moon", "Venus", "Jupiter",
    };

    private static readonly HashSet<string> MaleficPlanets = new(StringComparer.OrdinalIgnoreCase)
    {
        "Sun", "Mars", "Saturn", "Rahu", "Ketu",
    };

    private static void AddAspects(JsonObject item, JsonElement source)
    {
        var names = ReadList(source, "aspectedBy");
        Set(item, "beneficAspects", JoinMatching(names, BeneficPlanets));
        Set(item, "maleficAspects", JoinMatching(names, MaleficPlanets));
    }

    private static string? JoinMatching(IEnumerable<string> names, HashSet<string> group)
    {
        var matched = names.Where(group.Contains).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return matched.Length == 0 ? null : string.Join(", ", matched);
    }

    private static bool MatchesTopic(JsonElement item, string? name, string? description, TopicRule topic, IReadOnlyCollection<string>? planetNames)
    {
        var text = $"{name} {description}".ToLowerInvariant();
        if (topic.Keywords.Any(keyword => text.Contains(keyword, StringComparison.Ordinal)))
        {
            return true;
        }

        var houses = Find(item, "involvedHouses", "houses");
        foreach (var house in Enumerate(houses))
        {
            var number = house.ValueKind == JsonValueKind.Number
                ? (int?)house.GetInt32()
                : ReadInt(house, "houseNumber", "house");
            if (number is not null && topic.Houses.Contains(number.Value))
            {
                return true;
            }
        }

        return ReadList(item, "involvedPlanets").Any(planet =>
            topic.Planets.Any(allowed => string.Equals(allowed, planet, StringComparison.OrdinalIgnoreCase))
            || (planetNames?.Any(allowed => string.Equals(allowed, planet, StringComparison.OrdinalIgnoreCase)) ?? false));
    }

    private static readonly int[] SupportiveHouses = [1, 4, 5, 7, 9, 10];
    private static readonly int[] ChallengingHouses = [6, 8, 12];

    private static bool AsksForTiming(string? question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return false;
        }

        var text = question.ToLowerInvariant();
        return text.Contains("when", StringComparison.Ordinal)
            || text.Contains("period", StringComparison.Ordinal)
            || text.Contains("suitable", StringComparison.Ordinal)
            || text.Contains("timing", StringComparison.Ordinal)
            || text.Contains("which year", StringComparison.Ordinal)
            || text.Contains("how soon", StringComparison.Ordinal)
            || text.Contains("good time", StringComparison.Ordinal);
    }

    private static void AttachDashaInfluence(JsonElement chart, JsonObject facts, string? question)
    {
        var scores = BuildPlanetScores(chart);
        if (facts["currentDasha"] is JsonObject current)
        {
            var mahaScore = ScoreOf(scores, JsonString(current, "mahadasha"));
            var antarScore = ScoreOf(scores, JsonString(current, "antardasha"));
            Set(current, "mahadashaInfluence", InfluenceWord(mahaScore));
            Set(current, "antardashaInfluence", InfluenceWord(Blend(antarScore, mahaScore)));
            Set(current, "periodInfluence", InfluenceWord(Blend(antarScore, mahaScore)));
        }

        if (facts["mahadasha"] is JsonArray openPeriods)
        {
            StampPeriodInfluence(openPeriods, scores);
        }

        var periods = ReadUpcomingPeriods(chart);
        StampPeriodInfluence(periods, scores);
        Set(facts, "dashaPeriods", periods);
        if (AsksForTiming(question))
        {
            Set(facts, "timingWindows", BuildTimingWindows(periods));
        }
    }

    private static JsonArray BuildTimingWindows(JsonArray periods)
    {
        var rows = new List<TimingWindow>();
        var today = DateTime.UtcNow.Date;
        foreach (var node in periods)
        {
            if (node is not JsonObject mahadasha || mahadasha["antardashas"] is not JsonArray antardashas)
            {
                continue;
            }

            var mahaPlanet = JsonString(mahadasha, "planet") ?? string.Empty;
            foreach (var antarNode in antardashas)
            {
                if (antarNode is not JsonObject antardasha)
                {
                    continue;
                }

                var start = ParseFactDate(JsonString(antardasha, "startDate"));
                var end = ParseFactDate(JsonString(antardasha, "endDate"));
                if (start is null || end is null || end.Value.Date < today)
                {
                    continue;
                }

                var highly = antardasha["highlySupportive"] is JsonValue flag
                    && flag.TryGetValue<bool>(out var isHighly)
                    && isHighly;
                var influence = highly
                    ? "highlySupportive"
                    : JsonString(antardasha, "influence");
                if (string.IsNullOrWhiteSpace(influence))
                {
                    continue;
                }

                rows.Add(new TimingWindow(
                    start.Value.Date,
                    end.Value.Date,
                    JsonString(antardasha, "planet") ?? string.Empty,
                    mahaPlanet,
                    influence));
            }
        }

        rows.Sort((left, right) => left.Start.CompareTo(right.Start));
        var nearest = rows.FirstOrDefault(row => row.End >= today);
        if (nearest is null)
        {
            return new JsonArray();
        }

        var windows = new JsonArray { ToWindow("nearest", nearest) };
        var stronger = rows.FirstOrDefault(row => row.Start >= nearest.End && InfluenceRank(row.Influence) > InfluenceRank(nearest.Influence));
        if (stronger is not null)
        {
            windows.Add(ToWindow("stronger", stronger));
        }

        return windows;
    }

    private static JsonObject ToWindow(string role, TimingWindow window)
    {
        var item = new JsonObject();
        Set(item, "role", role);
        Set(item, "mahadasha", window.Mahadasha);
        Set(item, "antardasha", window.Antardasha);
        Set(item, "startDate", window.Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Set(item, "endDate", window.End.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        Set(item, "influence", window.Influence);
        return item;
    }

    private static int InfluenceRank(string influence) => influence switch
    {
        "highlySupportive" => 3,
        "supportive" => 2,
        "mixed" => 1,
        _ => 0,
    };

    private static DateTime? ParseFactDate(string? value) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : null;

    private sealed record TimingWindow(DateTime Start, DateTime End, string Antardasha, string Mahadasha, string Influence);

    private static void StampPeriodInfluence(JsonArray periods, IReadOnlyDictionary<string, int> scores)
    {
        foreach (var node in periods)
        {
            if (node is not JsonObject row)
            {
                continue;
            }

            var mahaScore = ScoreOf(scores, JsonString(row, "planet"));
            Set(row, "influence", InfluenceWord(mahaScore));
            if (row["antardashas"] is not JsonArray antardashas)
            {
                continue;
            }

            foreach (var antarNode in antardashas)
            {
                if (antarNode is not JsonObject antardasha)
                {
                    continue;
                }

                var antarScore = ScoreOf(scores, JsonString(antardasha, "planet"));
                Set(antardasha, "influence", InfluenceWord(Blend(antarScore, mahaScore)));
                if (mahaScore >= 65 && antarScore >= 65)
                {
                    antardasha["highlySupportive"] = true;
                }
            }
        }
    }

    private static Dictionary<string, int> BuildPlanetScores(JsonElement chart)
    {
        var totals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var shadbala = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var tables = Find(chart, "advancedStrengthTables");
        foreach (var row in Enumerate(Find(tables, "planetaryStrengths")))
        {
            var planet = ReadText(row, "planet");
            if (!string.IsNullOrWhiteSpace(planet) && ReadInt(row, "totalScore") is int total)
            {
                totals[planet] = Math.Clamp(total, 0, 100);
            }
        }

        foreach (var row in Enumerate(Find(tables, "shadbala")))
        {
            var planet = ReadText(row, "planet");
            if (!string.IsNullOrWhiteSpace(planet) && ReadInt(row, "percent") is int percent)
            {
                shadbala[planet] = Math.Clamp(percent, 0, 100);
            }
        }

        var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var planet in totals.Keys.Concat(shadbala.Keys).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var parts = new List<int>();
            if (totals.TryGetValue(planet, out var total))
            {
                parts.Add(total);
            }

            if (shadbala.TryGetValue(planet, out var percent))
            {
                parts.Add(percent);
            }

            scores[planet] = (int)Math.Round(parts.Average());
        }

        foreach (var planet in Enumerate(Find(chart, "planets")))
        {
            var name = ReadText(planet, "planet", "name");
            if (string.IsNullOrWhiteSpace(name) || scores.ContainsKey(name))
            {
                continue;
            }

            if (FallbackPlanetScore(planet) is int fallback)
            {
                scores[name] = fallback;
            }
        }

        return scores;
    }

    private static int? FallbackPlanetScore(JsonElement planet)
    {
        var score = 50;
        var used = false;
        var dignity = (ReadText(planet, "detailedStrength", "strength") ?? string.Empty).ToLowerInvariant();
        if (dignity.Contains("exalted", StringComparison.Ordinal) && dignity.Contains("strong", StringComparison.Ordinal))
        {
            score += 25;
            used = true;
        }
        else if (dignity.Contains("exalted", StringComparison.Ordinal))
        {
            score += 15;
            used = true;
        }
        else if (dignity.Contains("moolatrikona", StringComparison.Ordinal) || dignity.Contains("moola", StringComparison.Ordinal))
        {
            score += 20;
            used = true;
        }
        else if (dignity.Contains("own", StringComparison.Ordinal))
        {
            score += 18;
            used = true;
        }
        else if (dignity.Contains("friend", StringComparison.Ordinal))
        {
            score += 10;
            used = true;
        }
        else if (dignity.Contains("debil", StringComparison.Ordinal))
        {
            score -= 25;
            used = true;
        }
        else if (dignity.Contains("enemy", StringComparison.Ordinal))
        {
            score -= 10;
            used = true;
        }
        else if (dignity.Contains("neutral", StringComparison.Ordinal) || dignity.Contains("notapplicable", StringComparison.Ordinal))
        {
            used = true;
        }

        var house = ReadInt(planet, "house", "houseNumber");
        if (house is > 0)
        {
            used = true;
            if (SupportiveHouses.Contains(house.Value))
            {
                score += 10;
            }
            else if (ChallengingHouses.Contains(house.Value))
            {
                score -= 10;
            }
        }

        if (ReadBool(planet, "isCombust") == true)
        {
            score -= 15;
            used = true;
        }

        var benefic = ReadBool(planet, "hasBeneficAspect") == true;
        var malefic = ReadBool(planet, "hasMaleficAspect") == true;
        if (benefic && !malefic)
        {
            score += 8;
            used = true;
        }
        else if (malefic && !benefic)
        {
            score -= 8;
            used = true;
        }
        else if (benefic && malefic)
        {
            used = true;
        }

        return used ? Math.Clamp(score, 0, 100) : null;
    }

    private static int? ScoreOf(IReadOnlyDictionary<string, int> scores, string? planet) =>
        !string.IsNullOrWhiteSpace(planet) && scores.TryGetValue(planet, out var score) ? score : null;

    private static int? Blend(int? primary, int? companion)
    {
        if (primary is null && companion is null)
        {
            return null;
        }

        if (primary is null)
        {
            return companion;
        }

        if (companion is null)
        {
            return primary;
        }

        return (int)Math.Round((primary.Value + companion.Value) / 2.0);
    }

    private static string? InfluenceWord(int? score) => score switch
    {
        null => null,
        >= 65 => "supportive",
        < 50 => "challenging",
        _ => "mixed",
    };

    private static string? JsonString(JsonObject obj, string key) =>
        obj[key] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;

    private static JsonArray ReadUpcomingPeriods(JsonElement chart)
    {
        var rows = ReadOpenMahadashas(chart);
        while (rows.Count > 2)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        return rows;
    }

    private static JsonArray ReadOpenMahadashas(JsonElement chart)
    {
        var rows = new JsonArray();
        var today = DateTime.UtcNow.Date;
        foreach (var period in Enumerate(Find(chart, "mahadashaPeriods")))
        {
            var end = ReadDate(period, "endDate", "end");
            if (end is not null && end.Value.Date < today)
            {
                continue;
            }

            var row = new JsonObject();
            Set(row, "planet", ReadText(period, "planet", "name"));
            Set(row, "startDate", ReadDateText(period, "startDate", "start"));
            Set(row, "endDate", ReadDateText(period, "endDate", "end"));
            var antardashas = new JsonArray();
            foreach (var antardasha in Enumerate(Find(period, "antardashas")))
            {
                var antardashaEnd = ReadDate(antardasha, "endDate", "end");
                if (antardashaEnd is not null && antardashaEnd.Value.Date < today)
                {
                    continue;
                }

                var item = new JsonObject();
                Set(item, "planet", ReadText(antardasha, "subPlanet", "planet", "name"));
                Set(item, "startDate", ReadDateText(antardasha, "startDate", "start"));
                Set(item, "endDate", ReadDateText(antardasha, "endDate", "end"));
                antardashas.Add(item);
            }

            if (antardashas.Count > 0)
            {
                row["antardashas"] = antardashas;
            }

            rows.Add(row);
        }

        return rows;
    }

    private static string? ReadDateText(JsonElement? element, params string[] names)
    {
        if (element is not { } found)
        {
            return null;
        }

        var parsed = ReadDate(found, names);
        return parsed?.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static void SetNode(JsonObject target, string key, JsonNode? value)
    {
        if (value is not null)
        {
            target[key] = value;
        }
    }

    private static void Set(JsonObject target, string key, JsonArray? value)
    {
        if (value is null || value.Count == 0)
        {
            return;
        }

        target[key] = value;
    }

    private static void Set(JsonObject target, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            target[key] = value;
        }
    }

    private static void Set(JsonObject target, string key, bool? value)
    {
        if (value is not null)
        {
            target[key] = value.Value;
        }
    }

    private static void Set(JsonObject target, string key, int? value)
    {
        if (value is not null)
        {
            target[key] = value.Value;
        }
    }

    private static string? ReadTextOrList(JsonElement element, params string[] names)
    {
        var values = ReadList(element, names);
        return values.Count == 0 ? ReadText(element, names) : string.Join(", ", values);
    }

    private static List<string> ReadList(JsonElement element, params string[] names)
    {
        var value = Find(element, names);
        if (value is not { } found)
        {
            return [];
        }

        if (found.ValueKind == JsonValueKind.String)
        {
            var text = found.GetString();
            return string.IsNullOrWhiteSpace(text) ? [] : [text];
        }

        return Enumerate(found)
            .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!)
            .ToList();
    }

    private static string? FirstSentence(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return description;
        }

        var stop = description.IndexOfAny(['.', '।']);
        return stop >= 0 ? description[..(stop + 1)].Trim() : description.Trim();
    }

    private static IEnumerable<JsonElement> Enumerate(JsonElement? element)
    {
        if (element is not { ValueKind: JsonValueKind.Array } array)
        {
            yield break;
        }

        foreach (var item in array.EnumerateArray())
        {
            yield return item;
        }
    }

    private static JsonElement? Find(JsonElement? element, params string[] names)
    {
        if (element is not { ValueKind: JsonValueKind.Object } obj)
        {
            return null;
        }

        foreach (var property in obj.EnumerateObject())
        {
            if (names.Any(name => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)))
            {
                return property.Value.ValueKind == JsonValueKind.Null ? null : property.Value;
            }
        }

        return null;
    }

    private static string? ReadText(JsonElement? element, params string[] names)
    {
        var value = Find(element, names);
        if (value is not { } found || found.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return found.ValueKind switch
        {
            JsonValueKind.String => found.GetString(),
            JsonValueKind.Number => found.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => found.GetRawText(),
        };
    }

    private static JsonNode? ReadNumberOrText(JsonElement element, params string[] names)
    {
        var value = Find(element, names);
        if (value is not { } found)
        {
            return null;
        }

        if (found.ValueKind == JsonValueKind.Number && found.TryGetInt32(out var integer))
        {
            return JsonValue.Create(integer);
        }

        if (found.ValueKind == JsonValueKind.Number && found.TryGetDecimal(out var number))
        {
            return JsonValue.Create(number);
        }

        return ReadText(element, names);
    }

    private static bool? ReadBool(JsonElement element, params string[] names)
    {
        var value = Find(element, names);
        return value is { ValueKind: JsonValueKind.True or JsonValueKind.False } found
            ? found.GetBoolean()
            : null;
    }

    private static int? ReadInt(JsonElement element, params string[] names)
    {
        var value = Find(element, names);
        if (value is not { } found)
        {
            return null;
        }

        if (found.ValueKind == JsonValueKind.Number && found.TryGetInt32(out var number))
        {
            return number;
        }

        return found.ValueKind == JsonValueKind.String
            && int.TryParse(found.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out number)
            ? number
            : null;
    }

    private static DateTime? ReadDate(JsonElement element, params string[] names)
    {
        var text = ReadText(element, names);
        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
            ? parsed
            : null;
    }

    private sealed record TopicRule(string Name, string[] Keywords, int[] Houses, string[] Planets);
}
