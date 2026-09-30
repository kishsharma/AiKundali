using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace KundaliEngine.API.Services.Billing;

public interface IRazorpayClient
{
    bool IsConfigured { get; }

    string KeyId { get; }

    Task<RazorpaySubscriptionResult> CreateSubscriptionAsync(string planId, IReadOnlyDictionary<string, string> notes, CancellationToken cancellationToken);

    Task CancelSubscriptionAsync(string subscriptionId, bool cancelAtCycleEnd, CancellationToken cancellationToken);

    Task<RazorpayOrderResult> CreateOrderAsync(int amountPaise, string currency, IReadOnlyDictionary<string, string> notes, CancellationToken cancellationToken, string? receipt = null);
}

public sealed class RazorpaySubscriptionResult
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

public sealed class RazorpayOrderResult
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public int Amount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "INR";
}

public sealed class RazorpayClient : IRazorpayClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _httpClient;
    private readonly RazorpayOptions _options;

    public RazorpayClient(HttpClient httpClient, IOptions<RazorpayOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
        if (IsConfigured)
        {
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_options.KeyId}:{_options.KeySecret}"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.KeyId) && !string.IsNullOrWhiteSpace(_options.KeySecret);

    public string KeyId => _options.KeyId;

    public async Task<RazorpaySubscriptionResult> CreateSubscriptionAsync(
        string planId,
        IReadOnlyDictionary<string, string> notes,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var payload = new
        {
            plan_id = planId,
            total_count = 120,
            quantity = 1,
            customer_notify = 1,
            notes
        };

        return await PostAsync<RazorpaySubscriptionResult>("v1/subscriptions", payload, cancellationToken);
    }

    public async Task CancelSubscriptionAsync(string subscriptionId, bool cancelAtCycleEnd, CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var payload = new { cancel_at_cycle_end = cancelAtCycleEnd ? 1 : 0 };
        await PostAsync<object>($"v1/subscriptions/{subscriptionId}/cancel", payload, cancellationToken);
    }

    public async Task<RazorpayOrderResult> CreateOrderAsync(
        int amountPaise,
        string currency,
        IReadOnlyDictionary<string, string> notes,
        CancellationToken cancellationToken,
        string? receipt = null)
    {
        EnsureConfigured();
        if (amountPaise < 100)
        {
            throw new ArgumentException("Amount must be at least 100 paise.");
        }

        var payload = new
        {
            amount = amountPaise,
            currency = string.IsNullOrWhiteSpace(currency) ? "INR" : currency,
            receipt = string.IsNullOrWhiteSpace(receipt)
                ? $"hl_{Guid.NewGuid():N}"[..32]
                : receipt.Trim()[..Math.Min(40, receipt.Trim().Length)],
            notes
        };

        try
        {
            return await PostAsync<RazorpayOrderResult>("v1/orders", payload, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("Razorpay is not configured.");
        }
    }

    private async Task<T> PostAsync<T>(string path, object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(path, content, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException("Razorpay authentication failed.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Razorpay request failed: {response.StatusCode} {body}");
        }

        if (typeof(T) == typeof(object))
        {
            return default!;
        }

        return JsonSerializer.Deserialize<T>(body, JsonOptions)
            ?? throw new InvalidOperationException("Razorpay returned an empty response.");
    }
}
