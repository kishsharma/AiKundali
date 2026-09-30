using System.Net.Http.Json;
using System.Text.Json;
using KundaliEngine.API.DTOs.LifeArea;
using KundaliEngine.API.Services.Dhruva;
using Microsoft.Extensions.Options;

namespace KundaliEngine.API.Services.LifeArea;

public sealed class LifeAreaInsightService
{
    private readonly HttpClient _httpClient;
    private readonly DhruvaNarrationOptions _options;

    public LifeAreaInsightService(HttpClient httpClient, IOptions<DhruvaNarrationOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> WriteAsync(LifeAreaReportDto report, string locale, CancellationToken cancellationToken)
    {
        var payload = new
        {
            model = _options.Model,
            temperature = _options.Temperature,
            messages = new object[]
            {
                new { role = "system", content = LifeAreaInsightPrompt.SystemPrompt },
                new { role = "user", content = LifeAreaInsightPrompt.BuildUserMessage(report, locale) },
            },
        };

        try
        {
            var baseUrl = _options.BaseUrl.TrimEnd('/');
            using var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/chat/completions", payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new DhruvaNarrationUnavailableException("A personal note is unavailable right now.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var insight = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
            insight = DhruvaNarrationService.ToPlainText(insight);
            if (string.IsNullOrWhiteSpace(insight))
            {
                throw new DhruvaNarrationUnavailableException("A personal note is unavailable right now.");
            }

            return insight;
        }
        catch (DhruvaNarrationUnavailableException)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            throw new DhruvaNarrationUnavailableException("A personal note is unavailable right now.", exception);
        }
    }
}
