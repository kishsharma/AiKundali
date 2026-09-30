namespace KundaliEngine.API.Services.Muhurtha.GrahaPravesha;

public sealed class GrahaPraveshaRuleEngine
{
    private static readonly string[] AcceptableCategories = ["TITHI", "NAKSHATRA", "YOGA", "KARANA", "VARA"];

    public GrahaPraveshaRuleOutcome Evaluate(GrahaPraveshaFacts facts, GrahaPraveshaProfile profile)
    {
        var evidence = new List<GrahaPraveshaEvidence>();
        var hardRejected = false;
        var score = profile.Scoring.BaseScore;
        var blocksRecommended = false;
        var kalashaEligible = false;

        foreach (var rule in profile.Rules.Where(rule => rule.Enabled).OrderBy(rule => rule.Priority))
        {
            if (string.Equals(rule.Condition.Type, "KALASHA_IN_RANGES", StringComparison.OrdinalIgnoreCase))
            {
                var eligible = KalashaChakraEvaluator.IsEligible(facts.KalashaCount, profile.Kalasha.FavorableRanges);
                kalashaEligible = eligible;
                if (eligible)
                {
                    score += rule.ScoreImpact;
                    evidence.Add(Build(rule, facts, "PASS", "INFO", rule.PassExplanation, rule.ScoreImpact));
                }
                else
                {
                    score += rule.ScoreImpactWhenUnmatched;
                    if (rule.BlocksRecommendedWhenUnmatched)
                    {
                        blocksRecommended = true;
                    }

                    evidence.Add(Build(rule, facts, "WARNING", "SOFT", rule.UnmatchedExplanation, rule.ScoreImpactWhenUnmatched));
                }

                continue;
            }

            if (string.Equals(rule.Condition.Type, "VASTU_TABLE_MATCH", StringComparison.OrdinalIgnoreCase))
            {
                if (facts.VastuTableMatched)
                {
                    score += rule.ScoreImpact;
                    evidence.Add(Build(rule, facts, "PASS", "INFO", rule.Explanation, rule.ScoreImpact));
                }
                else
                {
                    evidence.Add(Build(rule, facts, "PASS", "INFO", rule.UnmatchedExplanation, 0));
                }

                continue;
            }

            var matched = Matches(rule, facts, profile);
            if (string.Equals(rule.HardOrSoft, "HARD_EXCLUSION", StringComparison.OrdinalIgnoreCase))
            {
                if (matched)
                {
                    hardRejected = true;
                    evidence.Add(Build(rule, facts, "FAIL", "HARD", rule.Explanation, 0));
                }
                else
                {
                    evidence.Add(Build(rule, facts, "PASS", "INFO", rule.PassExplanation, 0));
                }

                continue;
            }

            if (matched)
            {
                score += rule.ScoreImpact;
                evidence.Add(Build(rule, facts, "PASS", "INFO", FirstNonEmpty(rule.PassExplanation, rule.Explanation), rule.ScoreImpact));
            }
        }

        foreach (var rule in profile.Rules.Where(rule => !rule.Enabled).OrderBy(rule => rule.Priority))
        {
            evidence.Add(new GrahaPraveshaEvidence
            {
                RuleId = rule.RuleId,
                Category = rule.Category,
                Input = "disabled",
                Result = "SKIPPED",
                Severity = "INFO",
                Message = FirstNonEmpty(rule.Notes, rule.Explanation),
                Source = rule.Source
            });
        }

        AddAcceptableFallbacks(facts, evidence);
        score = Math.Clamp(score, 0, 100);

        return new GrahaPraveshaRuleOutcome
        {
            Status = ResolveStatus(profile, score, hardRejected, blocksRecommended),
            Score = score,
            HardRejected = hardRejected,
            KalashaEligible = kalashaEligible,
            BlocksRecommended = blocksRecommended,
            Evidence = evidence
        };
    }

    public static string ResolveStatus(GrahaPraveshaProfile profile, int score, bool hardRejected, bool blocksRecommended)
    {
        if (hardRejected)
        {
            return "REJECTED";
        }

        if (!blocksRecommended && score >= profile.Scoring.RecommendedMinimum)
        {
            return "RECOMMENDED";
        }

        if (score >= profile.Scoring.SuitableMinimum)
        {
            return "SUITABLE";
        }

        return "ACCEPTABLE";
    }

    private static void AddAcceptableFallbacks(GrahaPraveshaFacts facts, List<GrahaPraveshaEvidence> evidence)
    {
        foreach (var category in AcceptableCategories)
        {
            var categoryEvidence = evidence.Where(item => string.Equals(item.Category, category, StringComparison.OrdinalIgnoreCase)).ToList();
            if (categoryEvidence.Any(item => item.Result is "FAIL" or "WARNING"))
            {
                continue;
            }

            if (categoryEvidence.Any(item => item.Result == "PASS" && item.ScoreImpact > 0))
            {
                continue;
            }

            if (categoryEvidence.Any(item => item.RuleId.EndsWith("_ACCEPTABLE", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var input = InputFor(category, facts);
            evidence.Add(new GrahaPraveshaEvidence
            {
                RuleId = $"GP_{category}_ACCEPTABLE",
                Category = category,
                Input = input,
                Result = "PASS",
                Severity = "INFO",
                Message = $"{input} is acceptable for Graha Pravesha. It is not on a verified favorable or prohibited list.",
                Source = "Graha Pravesha Rule Profile"
            });
        }
    }

    private static bool Matches(GrahaPraveshaRule rule, GrahaPraveshaFacts facts, GrahaPraveshaProfile profile)
    {
        var values = rule.Condition.Values;
        return rule.Condition.Type.ToUpperInvariant() switch
        {
            "TITHI_IN" => Contains(values, facts.Tithi),
            "NAKSHATRA_IN" => Contains(values, facts.Nakshatra),
            "YOGA_IN" => Contains(values, facts.Yoga),
            "KARANA_IN" => Contains(values, facts.Karana),
            "LAGNA_IN" => Contains(values, facts.Lagna),
            "VARA_IN" => Contains(values, facts.Vara),
            "OVERLAPS_WINDOW" => values.Any(value => facts.OverlappingWindows.Contains(value, StringComparer.OrdinalIgnoreCase)),
            "KALASHA_IN_RANGES" => KalashaChakraEvaluator.IsEligible(facts.KalashaCount, profile.Kalasha.FavorableRanges),
            "VASTU_TABLE_MATCH" => facts.VastuTableMatched,
            _ => false
        };
    }

    private static GrahaPraveshaEvidence Build(
        GrahaPraveshaRule rule,
        GrahaPraveshaFacts facts,
        string result,
        string severity,
        string message,
        int scoreImpact)
    {
        return new GrahaPraveshaEvidence
        {
            RuleId = rule.RuleId,
            Category = rule.Category,
            Input = InputFor(rule.Category, facts),
            Result = result,
            Severity = severity,
            Message = string.IsNullOrWhiteSpace(message) ? rule.Explanation : message,
            Source = rule.Source,
            ScoreImpact = scoreImpact
        };
    }

    private static string InputFor(string category, GrahaPraveshaFacts facts)
    {
        return category.ToUpperInvariant() switch
        {
            "TITHI" => facts.Tithi,
            "NAKSHATRA" => facts.Nakshatra,
            "YOGA" => facts.Yoga,
            "KARANA" => facts.Karana,
            "VARA" => facts.Vara,
            "LAGNA" => facts.Lagna,
            "KALASHA" => facts.KalashaCount.ToString(),
            "VASTU" => facts.VastuTableMatched
                ? $"{facts.Paksha} {facts.Tithi} {facts.Vara} {facts.MoonSign} {facts.VastuApplicability} {facts.VastuAhuti}".Trim()
                : $"{facts.Paksha} {facts.Tithi} {facts.Vara} {facts.MoonSign}".Trim(),
            "RAHU_KALAM" => "rahu_kalam",
            "YAMAGANDA" => "yamaganda",
            "GULIKA" => "gulika_kalam",
            "ECLIPSE" => facts.OverlappingWindows.FirstOrDefault(window => window.Contains("eclipse", StringComparison.OrdinalIgnoreCase)) ?? "none",
            _ => facts.Tithi
        };
    }

    private static bool Contains(IEnumerable<string> values, string actual)
    {
        return values.Any(value => string.Equals(Normalize(value), Normalize(actual), StringComparison.OrdinalIgnoreCase));
    }

    private static string Normalize(string value)
    {
        return value.Trim().Replace(" ", string.Empty, StringComparison.Ordinal);
    }

    private static string FirstNonEmpty(string preferred, string fallback)
    {
        return string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
    }
}
