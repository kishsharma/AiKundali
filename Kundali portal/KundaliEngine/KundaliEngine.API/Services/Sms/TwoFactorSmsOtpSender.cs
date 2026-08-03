using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace KundaliEngine.API.Services.Sms;

public sealed class TwoFactorSmsOtpSender : ISmsOtpSender
{
    private readonly HttpClient _httpClient;
    private readonly SmsOtpOptions _options;
    private readonly ILogger<TwoFactorSmsOtpSender> _logger;

    public TwoFactorSmsOtpSender(
        HttpClient httpClient,
        IOptions<SmsOtpOptions> options,
        ILogger<TwoFactorSmsOtpSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken)
    {
        var apiKey = ResolveApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("2Factor SMS API key is not configured.");
        }

        var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
            ? "https://2factor.in"
            : _options.BaseUrl.TrimEnd('/');
        var templateName = string.IsNullOrWhiteSpace(_options.TemplateName)
            ? "horalifeotp"
            : _options.TemplateName.Trim();

        var requestUri = $"{baseUrl}/API/V1/{Uri.EscapeDataString(apiKey)}/SMS/{Uri.EscapeDataString(phoneNumber)}/{Uri.EscapeDataString(code)}/{Uri.EscapeDataString(templateName)}";
        using var response = await _httpClient.GetAsync(requestUri, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("2Factor SMS OTP send failed with status {StatusCode}: {ResponseBody}", response.StatusCode, body);
            throw new InvalidOperationException("Unable to send SMS verification code. Please try again later.");
        }

        var result = await response.Content.ReadFromJsonAsync<TwoFactorSmsResponse>(cancellationToken: cancellationToken);
        if (result != null && !string.Equals(result.Status, "Success", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("2Factor SMS OTP send returned {Status}: {Details}", result.Status, result.Details);
            throw new InvalidOperationException("Unable to send SMS verification code. Please try again later.");
        }
    }

    private string ResolveApiKey()
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return _options.ApiKey.Trim();
        }

        return string.IsNullOrWhiteSpace(_options.ApiKeyEnvironmentVariable)
            ? string.Empty
            : Environment.GetEnvironmentVariable(_options.ApiKeyEnvironmentVariable)?.Trim() ?? string.Empty;
    }

    private sealed class TwoFactorSmsResponse
    {
        public string? Status { get; set; }

        public string? Details { get; set; }
    }
}