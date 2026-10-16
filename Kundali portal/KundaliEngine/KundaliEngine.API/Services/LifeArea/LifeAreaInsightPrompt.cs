using KundaliEngine.API.DTOs.LifeArea;
using KundaliEngine.API.Services.Auth;

namespace KundaliEngine.API.Services.LifeArea;

public static class LifeAreaInsightPrompt
{
    public const string SystemPrompt =
        """
        You explain one HoraLife life-area reading to the person it belongs to.
        Use only the facts in the user message. Do not calculate astrology.
        Do not invent planets, houses, yogas, dates, remedies, or scores.
        Do not change the supplied verdict.
        Do not promise an outcome.
        Write plain sentences only. Do not use markdown, asterisks, headings, or HTML.
        """;

    public static string BuildUserMessage(LifeAreaReportDto report, string? locale)
    {
        var language = PreferredLanguageHelper.Normalize(locale) ?? "en";
        var difficult = string.Equals(report.Verdict, "needsCare", StringComparison.OrdinalIgnoreCase)
            || report.Factors.Any(factor => string.Equals(factor.Tone, "pressure", StringComparison.OrdinalIgnoreCase));
        var factors = report.Factors.Count == 0
            ? "None supplied."
            : string.Join("\n", report.Factors.Select(factor =>
                $"- {factor.Subject} | birth chart: {factor.NatalSign}, {factor.NatalDignity} | {factor.SpecialistChart}: {factor.SpecialistSign}, {factor.SpecialistDignity} | tone: {factor.Tone} | {factor.Reading}"));
        var note = string.IsNullOrWhiteSpace(report.Note) ? "None." : report.Note.Trim();
        var periods = report.Periods.Count == 0
            ? "None supplied."
            : string.Join("\n", report.Periods.Select(period =>
                $"- {period.Label} | {period.Planet} | {period.Tone} | {period.Reading}"));
        var difficultRule = difficult
            ? """
              This reading is difficult. Name the strain plainly, then point to any support that is still workable. Sound steady and consulting. Leave the person with a clear next stance such as patience or preparation. Do not frighten them, do not say this area is ruined, and do not rescue it with everything will be fine.
              """
            : "Keep the same verdict. Mention both help and strain when the facts include both.";

        return $"""
            You are a consulting astrologer speaking to this person about one life area.
            Response language: {LanguageName(language)}
            {LanguageInstruction(language)}
            Write two short paragraphs. The first says what this means in this person's life, in everyday words for this area. The second uses only the months listed under Periods when they are supplied, and says whether each one supports or needs care. Do not invent a month. When no months are supplied, use only the timing sentence.
            A chart code may appear once, then use plain words such as career chart.
            Do not promise an outcome. Do not say you will, guaranteed, certainly, or a percentage. Speak in terms of leaning, support, strain, and care.
            {difficultRule}
            If the note says this is not a medical diagnosis, say this is traditional interpretation, not a medical diagnosis.
            If the note mentions D60 or birth time, say the fine-detail chart depends on an exact birth time.

            Life area: {report.AreaName}
            Verdict: {report.Verdict}
            Verdict sentence: {report.VerdictText}
            Factors:
            {factors}
            Cross-check: {report.CrossValidation}
            Cross-check facts:
            {CrossCheckFacts(report)}
            Timing: {report.Timing}
            Periods:
            {periods}
            Note: {note}
            """;
    }

    private static string CrossCheckFacts(LifeAreaReportDto report)
    {
        if (report.CrossChecks.Count == 0)
        {
            return "None supplied.";
        }

        return string.Join("\n", report.CrossChecks.Select(check =>
            $"- {check.Planet} | D1 house {check.NatalHouse}, {check.NatalSign} {check.NatalDegree}°, {check.NatalDignity}{(check.NatalRetrograde ? ", retrograde" : "")} | {check.SpecialistChart} house {check.SpecialistHouse}, {check.SpecialistSign} {check.SpecialistDegree}°, {check.SpecialistDignity} | {(string.IsNullOrWhiteSpace(check.Relation) ? "birth chart only" : check.Relation)}"));
    }

    private static string LanguageName(string language) => language switch
    {
        "hi" => "Hindi",
        "kn" => "Kannada",
        _ => "English"
    };

    private static string LanguageInstruction(string language) => language switch
    {
        "hi" => "Write the entire answer in Hindi using Devanagari script. Do not mix languages.",
        "kn" => "Write the entire answer in Kannada using Kannada script. Do not mix languages.",
        _ => "Write the entire answer in English. Do not mix languages."
    };
}
