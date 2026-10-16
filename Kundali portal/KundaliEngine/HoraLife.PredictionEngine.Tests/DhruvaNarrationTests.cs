using System.Net;
using System.Text;
using System.Text.Json;
using KundaliEngine.API.Services.Dhruva;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

namespace HoraLife.PredictionEngine.Tests;

public class DhruvaNarrationTests
{
    private const string SampleChart = """
        {
          "summary": {
            "name": "Asha",
            "ascendant": "Leo",
            "moonSign": "Cancer",
            "sunSign": "Aries",
            "currentMahadasha": "Jupiter",
            "currentAntardasha": "Venus"
          },
          "native": {
            "birthDate": "1992-04-11",
            "birthTime": "06:20",
            "birthPlace": "Pune"
          },
          "planets": [
            { "planet": "Venus", "sign": "Libra", "house": 7, "degree": 12, "detailedStrength": "Own sign", "conditionTone": "Supportive", "aspectedBy": ["Jupiter"] },
            { "planet": "Jupiter", "sign": "Cancer", "house": 12, "degree": 8, "detailedStrength": "Exalted", "conditionTone": "Supportive" },
            { "planet": "Saturn", "sign": "Capricorn", "house": 6, "degree": 3, "detailedStrength": "Own sign", "conditionTone": "Challenging" },
            { "planet": "Mars", "sign": "Aries", "house": 1, "degree": 4, "detailedStrength": "Exalted" }
          ],
          "houses": [
            { "houseNumber": 1, "sign": "Leo", "score": 40 },
            { "houseNumber": 7, "sign": "Virgo", "score": 76, "houseName": "Marriage & Partnership", "houseOwner": "Mercury", "occupants": ["Venus"], "aspectedBy": ["Jupiter", "Saturn"] }
          ],
          "yogas": [
            { "name": "Marriage support", "description": "Venus supports marriage. Extra detail stays out." }
          ],
          "advancedStrengthTables": {
            "shadbala": [{ "planet": "Mars", "total": 9 }]
          }
        }
        """;

    [Theory]
    [InlineData("शादी कब होगी?", "hindi")]
    [InlineData("When is marriage likely?", "english")]
    [InlineData("ಮದುವೆ ಯಾವಾಗ?", "other")]
    public void QuestionLanguage_SplitsHindiEnglishAndOther(string question, string language)
    {
        Assert.Equal(language, DhruvaQuestionLanguage.Classify(question));
    }

    [Fact]
    public void MarriageQuestion_KeepsHouse7_AndDropsShadbala()
    {
        using var document = JsonDocument.Parse(SampleChart);
        var facts = DhruvaChartFactBuilder.Build(document.RootElement, "qa", "When is marriage likely?");

        Assert.Contains("\"house\":7", facts);
        Assert.Contains("Venus", facts);
        Assert.Contains("Mercury", facts);
        Assert.Contains("Own sign", facts);
        Assert.Contains("beneficAspects", facts);
        Assert.Contains("Jupiter", facts);
        Assert.Contains("maleficAspects", facts);
        Assert.Contains("Saturn", facts);
        Assert.DoesNotContain("null", facts, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Shadbala", facts, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Mars", facts);
        Assert.DoesNotContain("\"house\":1,", facts);
        Assert.DoesNotContain("\"house\":1}", facts);
        Assert.DoesNotContain("upcomingPeriods", facts);
    }

    [Fact]
    public void JobTimingQuestion_IncludesTheNextDatedWindow()
    {
        const string chart = """
            {
              "currentDasha": {
                "mahadasha": "Rahu",
                "antardasha": "Saturn",
                "startDate": "2018-06-01T00:00:00",
                "endDate": "2036-06-01T00:00:00",
                "antardashaStartDate": "2024-01-01T00:00:00",
                "antardashaEndDate": "2030-04-01T00:00:00"
              },
              "mahadashaPeriods": [
                {
                  "planet": "Rahu",
                  "start": "2018-06-01T00:00:00",
                  "end": "2036-06-01T00:00:00",
                  "antardashas": [
                    { "subPlanet": "Ketu", "startDate": "2010-01-01T00:00:00", "endDate": "2011-01-01T00:00:00" },
                    { "subPlanet": "Saturn", "startDate": "2024-01-01T00:00:00", "endDate": "2030-04-01T00:00:00" },
                    { "subPlanet": "Mercury", "startDate": "2030-04-01T00:00:00", "endDate": "2032-08-01T00:00:00" }
                  ]
                },
                {
                  "planet": "Jupiter",
                  "start": "2036-06-01T00:00:00",
                  "end": "2052-06-01T00:00:00",
                  "antardashas": [
                    { "subPlanet": "Jupiter", "startDate": "2036-06-01T00:00:00", "endDate": "2038-07-01T00:00:00" }
                  ]
                },
                {
                  "planet": "Saturn",
                  "start": "2052-06-01T00:00:00",
                  "end": "2071-06-01T00:00:00",
                  "antardashas": [
                    { "subPlanet": "Saturn", "startDate": "2052-06-01T00:00:00", "endDate": "2055-06-01T00:00:00" }
                  ]
                }
              ],
              "planets": [
                { "planet": "Saturn", "sign": "Aries", "house": 8, "detailedStrength": "Debilitated" },
                { "planet": "Mercury", "sign": "Virgo", "house": 10, "detailedStrength": "Exalted" },
                { "planet": "Jupiter", "sign": "Cancer", "house": 1, "detailedStrength": "Exalted" }
              ],
              "houses": [
                { "houseNumber": 10, "sign": "Sagittarius", "houseOwner": "Jupiter", "score": 60 }
              ]
            }
            """;

        using var document = JsonDocument.Parse(chart);
        var timing = DhruvaChartFactBuilder.Build(document.RootElement, "qa", "When can I change my job? What is a suitable period?");
        var career = DhruvaChartFactBuilder.Build(document.RootElement, "qa", "How is my career?");

        Assert.Contains("\"house\":10", timing);
        Assert.Contains("antardashaEnd\":\"2030-04-01", timing);
        Assert.Contains("periodInfluence\":\"challenging", timing);
        Assert.Contains("\"role\":\"nearest\"", timing);
        Assert.Contains("\"role\":\"stronger\"", timing);
        Assert.Contains("\"antardasha\":\"Saturn\"", timing);
        Assert.Contains("\"antardasha\":\"Mercury\"", timing);
        Assert.Contains("Mercury", timing);
        Assert.Contains("\"influence\":\"supportive", timing);
        Assert.Contains("2030-04-01", timing);
        Assert.Contains("2032-08-01", timing);
        Assert.Contains("Jupiter", timing);
        Assert.DoesNotContain("Ketu", timing);
        Assert.DoesNotContain("2071-06-01", timing);
        Assert.DoesNotContain("2055-06-01", timing);
        Assert.Contains("dashaPeriods", career);
        Assert.Contains("periodInfluence\":\"challenging", career);
        Assert.DoesNotContain("timingWindows", career);
    }

    [Fact]
    public async Task Narration_SendsTrimmedChart_AndAppendsDisclaimer()
    {
        var handler = new CaptureHandler("The chart shows Venus in the 7th house.");
        var promptPath = Path.Combine(Path.GetTempPath(), $"dhruva-prompt-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(promptPath, "Narrate the chart.");
        try
        {
            var service = new DhruvaNarrationService(
                new HttpClient(handler),
                Options.Create(new DhruvaNarrationOptions
                {
                    BaseUrl = "http://127.0.0.1:11434/v1",
                    Model = "dhruva",
                    SystemPromptPath = promptPath,
                    Temperature = 0.4,
                    TimeoutSeconds = 180,
                }),
                new TestHostEnvironment());

            using var document = JsonDocument.Parse(SampleChart);
            var response = await service.NarrateAsync(new DhruvaNarrateRequestDto
            {
                Mode = "qa",
                Question = "When is marriage likely?",
                Locale = "en",
                Chart = document.RootElement.Clone(),
            }, CancellationToken.None);

            using var sent = JsonDocument.Parse(handler.Body);
            var systemMessage = sent.RootElement.GetProperty("messages")[0].GetProperty("content").GetString() ?? string.Empty;
            var userMessage = sent.RootElement.GetProperty("messages")[1].GetProperty("content").GetString() ?? string.Empty;
            Assert.Equal("dhruva", sent.RootElement.GetProperty("model").GetString());
            Assert.Equal(400, sent.RootElement.GetProperty("max_completion_tokens").GetInt32());
            Assert.False(sent.RootElement.TryGetProperty("reasoning_effort", out _));
            Assert.Contains("Response language: English", userMessage);
            Assert.Contains("Write the entire answer in English.", userMessage);
            Assert.Contains("When is marriage likely?", userMessage);
            Assert.Contains("EVIDENCE:", userMessage);
            Assert.Contains("Selected tone: friendly", userMessage);
            Assert.DoesNotContain("feel:", userMessage);
            Assert.Contains("\"house\":7", userMessage);
            Assert.Contains("warm and approachable guide", systemMessage);
            Assert.Contains("direct_answer", systemMessage);
            Assert.DoesNotContain("{{TONE_PROFILE}}", systemMessage);
            Assert.DoesNotContain("Shadbala", userMessage, StringComparison.OrdinalIgnoreCase);
            Assert.EndsWith(DhruvaNarrationService.Disclaimer, response.Narration);
            Assert.Equal(1, CountOf(response.Narration, DhruvaNarrationService.Disclaimer));
        }
        finally
        {
            File.Delete(promptPath);
        }
    }

    [Theory]
    [InlineData("friendly", "warm and approachable guide", "direct_answer", "at most 4 evidence points", "house_analysis")]
    [InlineData("astrologer", "experienced Vedic astrologer", "house_analysis", "at most 10 evidence points", "executive_summary")]
    [InlineData("corporate", "concise analytical advisor", "executive_summary", "at most 5 evidence points", "house_analysis")]
    public async Task Narration_SendsTheSelectedToneProfile_AndEvidence(string tone, string persona, string structure, string evidenceCap, string otherStructure)
    {
        var handler = new CaptureHandler("A toned explanation.");
        var promptPath = Path.Combine(Path.GetTempPath(), $"dhruva-prompt-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(promptPath, DhruvaToneProfile.Template);
        try
        {
            var service = new DhruvaNarrationService(
                new HttpClient(handler),
                Options.Create(new DhruvaNarrationOptions
                {
                    BaseUrl = "http://127.0.0.1:11434/v1",
                    Model = "dhruva",
                    SystemPromptPath = promptPath,
                    Temperature = 0.4,
                    TimeoutSeconds = 180,
                }),
                new TestHostEnvironment());

            using var document = JsonDocument.Parse(SampleChart);
            await service.NarrateAsync(new DhruvaNarrateRequestDto
            {
                Mode = "qa",
                Question = "How is my career?",
                Locale = "en",
                Tone = tone,
                Chart = document.RootElement.Clone(),
            }, CancellationToken.None);

            using var sent = JsonDocument.Parse(handler.Body);
            var systemMessage = sent.RootElement.GetProperty("messages")[0].GetProperty("content").GetString() ?? string.Empty;
            var userMessage = sent.RootElement.GetProperty("messages")[1].GetProperty("content").GetString() ?? string.Empty;
            Assert.Contains(persona, systemMessage);
            Assert.Contains(structure, systemMessage);
            Assert.Contains(evidenceCap, systemMessage);
            Assert.DoesNotContain(otherStructure, systemMessage);
            Assert.Contains($"Selected tone: {tone}", userMessage);
            Assert.Contains("EVIDENCE:", userMessage);
            Assert.Contains("How is my career?", userMessage);
            Assert.DoesNotContain("{{AUDIENCE}}", systemMessage);
            Assert.DoesNotContain("{{RESPONSE_STYLE}}", systemMessage);
        }
        finally
        {
            File.Delete(promptPath);
        }
    }

    [Theory]
    [InlineData("When is marriage likely?", null)]
    [InlineData("I am scared my marriage will fail", DhruvaQuestionFeeling.Anxious)]
    [InlineData("मुझे शादी को लेकर डर लग रहा है", DhruvaQuestionFeeling.Anxious)]
    [InlineData("I hope this new job works out", DhruvaQuestionFeeling.Hopeful)]
    [InlineData("He passed away and I feel lonely", DhruvaQuestionFeeling.Grieving)]
    [InlineData("Please answer immediately", DhruvaQuestionFeeling.Urgent)]
    [InlineData("I am fearless about this career", null)]
    public void QuestionFeeling_ReadsEmotionWithoutAnExtraModelCall(string question, string? feeling)
    {
        Assert.Equal(feeling, DhruvaQuestionFeeling.Classify(question));
    }

    [Fact]
    public async Task Narration_FoldsAWorriedQuestionIntoOneFeelCue()
    {
        var handler = new CaptureHandler("Asha, the nearer period still supports marriage.");
        var promptPath = Path.Combine(Path.GetTempPath(), $"dhruva-prompt-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(promptPath, DhruvaToneProfile.Template);
        try
        {
            var service = new DhruvaNarrationService(
                new HttpClient(handler),
                Options.Create(new DhruvaNarrationOptions
                {
                    BaseUrl = "http://127.0.0.1:11434/v1",
                    Model = "openai/gpt-oss-120b",
                    SystemPromptPath = promptPath,
                    Temperature = 0.4,
                    TimeoutSeconds = 180,
                    ReasoningEffort = "low",
                }),
                new TestHostEnvironment());

            using var document = JsonDocument.Parse(SampleChart);
            await service.NarrateAsync(new DhruvaNarrateRequestDto
            {
                Mode = "qa",
                Question = "I am scared my marriage will fail",
                Locale = "en",
                Tone = "friendly",
                Chart = document.RootElement.Clone(),
            }, CancellationToken.None);

            using var sent = JsonDocument.Parse(handler.Body);
            var systemMessage = sent.RootElement.GetProperty("messages")[0].GetProperty("content").GetString() ?? string.Empty;
            var userMessage = sent.RootElement.GetProperty("messages")[1].GetProperty("content").GetString() ?? string.Empty;
            Assert.Contains("feel:anxious", userMessage);
            Assert.Contains("fold that feeling into the first sentence", systemMessage);
            Assert.DoesNotContain("birthPlace", userMessage);
            Assert.Equal("low", sent.RootElement.GetProperty("reasoning_effort").GetString());
            Assert.Equal(400, sent.RootElement.GetProperty("max_completion_tokens").GetInt32());
        }
        finally
        {
            File.Delete(promptPath);
        }
    }

    [Fact]
    public async Task Narration_KeepsCorporateAnswersFactual()
    {
        var handler = new CaptureHandler("The career indicators are mixed.");
        var promptPath = Path.Combine(Path.GetTempPath(), $"dhruva-prompt-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(promptPath, "Narrate the chart.");
        try
        {
            var service = new DhruvaNarrationService(
                new HttpClient(handler),
                Options.Create(new DhruvaNarrationOptions
                {
                    BaseUrl = "http://127.0.0.1:11434/v1",
                    Model = "dhruva",
                    SystemPromptPath = promptPath,
                    Temperature = 0.4,
                    TimeoutSeconds = 180,
                }),
                new TestHostEnvironment());

            using var document = JsonDocument.Parse(SampleChart);
            await service.NarrateAsync(new DhruvaNarrateRequestDto
            {
                Mode = "qa",
                Question = "I am scared about my career",
                Locale = "en",
                Tone = "corporate",
                Chart = document.RootElement.Clone(),
            }, CancellationToken.None);

            using var sent = JsonDocument.Parse(handler.Body);
            var userMessage = sent.RootElement.GetProperty("messages")[1].GetProperty("content").GetString() ?? string.Empty;
            Assert.Contains("Selected tone: corporate", userMessage);
            Assert.DoesNotContain("feel:", userMessage);
            Assert.Equal(400, sent.RootElement.GetProperty("max_completion_tokens").GetInt32());
        }
        finally
        {
            File.Delete(promptPath);
        }
    }

    [Fact]
    public async Task Narration_AnswersInTheSelectedLanguage_WhenTheQuestionIsAnotherLanguage()
    {
        var handler = new CaptureHandler("विवाह का समय आगे की अवधि में दिखता है।");
        var promptPath = Path.Combine(Path.GetTempPath(), $"dhruva-prompt-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(promptPath, "Narrate the chart.");
        try
        {
            var service = new DhruvaNarrationService(
                new HttpClient(handler),
                Options.Create(new DhruvaNarrationOptions
                {
                    BaseUrl = "http://127.0.0.1:11434/v1",
                    Model = "dhruva",
                    SystemPromptPath = promptPath,
                    Temperature = 0.4,
                    TimeoutSeconds = 180,
                }),
                new TestHostEnvironment());

            using var document = JsonDocument.Parse(SampleChart);
            var response = await service.NarrateAsync(new DhruvaNarrateRequestDto
            {
                Mode = "qa",
                Question = "When is marriage likely?",
                Locale = "hi",
                Chart = document.RootElement.Clone(),
            }, CancellationToken.None);

            using var sent = JsonDocument.Parse(handler.Body);
            var userMessage = sent.RootElement.GetProperty("messages")[1].GetProperty("content").GetString() ?? string.Empty;
            Assert.Contains("Response language: Hindi", userMessage);
            Assert.Contains("Devanagari", userMessage);
            Assert.Contains("Do not answer in the question's language", userMessage);
            Assert.Contains("When is marriage likely?", userMessage);
            Assert.EndsWith(DhruvaNarrationService.DisclaimerFor("hi"), response.Narration);
            Assert.Equal("hi", DhruvaNarrationService.ResolveResponseLanguage("hi", "en"));
            Assert.Equal("kn", DhruvaNarrationService.ResolveResponseLanguage(null, "kn"));
        }
        finally
        {
            File.Delete(promptPath);
        }
    }

    [Fact]
    public void Evidence_KeepsTheMostRelevantPoints_UpToTheToneLimit()
    {
        const string facts = """
            {
              "topic": "career",
              "currentDasha": { "mahadasha": "Rahu", "periodInfluence": "challenging" },
              "timingWindows": [ { "role": "nearest" }, { "role": "stronger" } ],
              "houses": [ { "house": 10 } ],
              "planets": [ { "planet": "Saturn" }, { "planet": "Mercury" }, { "planet": "Jupiter" }, { "planet": "Mars" } ]
            }
            """;

        var friendly = DhruvaEvidenceSelector.Trim(facts, DhruvaToneProfile.Resolve("friendly").EvidenceLimit);
        var astrologer = DhruvaEvidenceSelector.Trim(facts, DhruvaToneProfile.Resolve("astrologer").EvidenceLimit);

        Assert.Contains("\"role\":\"nearest\"", friendly);
        Assert.Contains("\"role\":\"stronger\"", friendly);
        Assert.Contains("\"house\":10", friendly);
        Assert.Contains("Saturn", friendly);
        Assert.DoesNotContain("Mercury", friendly);
        Assert.DoesNotContain("Mars", friendly);
        Assert.Contains("periodInfluence\":\"challenging", friendly);
        Assert.Contains("Mercury", astrologer);
        Assert.Contains("Jupiter", astrologer);
        Assert.Contains("Mars", astrologer);
    }

    [Fact]
    public void Narration_plain_text_removes_markdown_markup()
    {
        var plain = DhruvaNarrationService.ToPlainText("""
            ## Career
            **The nearer stretch is supportive.**
            - Saturn presses the house
            <p>Use only the supplied evidence.</p>
            """);

        Assert.DoesNotContain("##", plain);
        Assert.DoesNotContain("**", plain);
        Assert.DoesNotContain("<p>", plain);
        Assert.Contains("The nearer stretch is supportive.", plain);
        Assert.Contains("Saturn presses the house", plain);
        Assert.Contains("Use only the supplied evidence.", plain);
    }

    [Fact]
    public void Past_dates_are_left_out_of_evidence_and_replies()
    {
        var today = new DateTime(2026, 9, 27);
        const string facts = """
            {
              "birthDate": "1992-04-11",
              "currentDasha": { "mahadashaStart": "2018-06-01", "mahadashaEnd": "2036-06-01" },
              "timingWindows": [ { "startDate": "2024-01-01", "endDate": "2030-04-01" } ]
            }
            """;

        var evidence = DhruvaEvidenceSelector.OmitPastDates(facts, today);
        Assert.DoesNotContain("1992-04-11", evidence);
        Assert.DoesNotContain("2018-06-01", evidence);
        Assert.DoesNotContain("2024-01-01", evidence);
        Assert.Contains("2036-06-01", evidence);
        Assert.Contains("2030-04-01", evidence);

        var reply = DhruvaNarrationService.OmitPastDatesFromReply(
            "The nearer stretch runs from June 2018 to April 2030. A stronger window opens on 1 August 2032.",
            today);
        Assert.DoesNotContain("2018", reply);
        Assert.Contains("April 2030", reply);
        Assert.Contains("1 August 2032", reply);

        var marriage = DhruvaNarrationService.OmitPastDatesFromReply(
            "Marriage could start between March 15, 2024 and June 4 2027, and it looks even more likely between June 4 2027 and October 28 2029.",
            today);
        Assert.DoesNotContain("2024", marriage);
        Assert.DoesNotContain("between and", marriage);
        Assert.Contains("by June 4 2027", marriage);
        Assert.Contains("between June 4 2027 and October 28 2029", marriage);
    }

    [Fact]
    public void Free_life_questions_stop_at_two_and_paid_plans_are_unlimited()
    {
        Assert.Equal(2, DhruvaUsageService.Remaining(2, 0));
        Assert.Equal(1, DhruvaUsageService.Remaining(2, 1));
        Assert.Equal(0, DhruvaUsageService.Remaining(2, 2));
        Assert.Equal(0, DhruvaUsageService.Remaining(2, 5));
        Assert.Null(DhruvaUsageService.Remaining(null, 5));
    }

    private static int CountOf(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        private readonly string _content;

        public CaptureHandler(string content)
        {
            _content = content;
        }

        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            var payload = JsonSerializer.Serialize(new
            {
                choices = new[]
                {
                    new { message = new { content = _content } },
                },
            });
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed class TestHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
