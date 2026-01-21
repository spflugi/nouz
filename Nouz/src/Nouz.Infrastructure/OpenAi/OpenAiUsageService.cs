using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Nouz.Application.OpenAi;
using Nouz.Application.Preferences;
using IPreferences = Nouz.Application.Preferences.IPreferences;

namespace Nouz.Infrastructure.OpenAi;

internal sealed class OpenAiUsageService : IOpenAiUsageService
{
    private const string BaseUrl = "https://api.openai.com/v1/organization";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly IPreferences _preferences;
    private readonly HttpClient _httpClient;

    public OpenAiUsageService(IPreferences preferences, IHttpClientFactory httpClientFactory)
    {
        _preferences = preferences;
        _httpClient = httpClientFactory.CreateClient("OpenAI");
    }

    public async Task<OpenAiUsageData?> GetCurrentMonthUsageAsync(CancellationToken cancellationToken = default)
    {
        var apiKey = await _preferences.Get(PreferenceKeys.OpenAiApiAdminKey).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        try
        {
            // Get the start of the current month
            var now = DateTimeOffset.UtcNow;
            var startOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
            var startTimestamp = startOfMonth.ToUnixTimeSeconds();
            var endTimestamp = now.ToUnixTimeSeconds();

            // Fetch completions usage (chat)
            var completionsTask = FetchUsageAsync(
                apiKey,
                $"{BaseUrl}/usage/completions?start_time={startTimestamp}&end_time={endTimestamp}&limit=30",
                cancellationToken);

            // Fetch embeddings usage
            var embeddingsTask = FetchUsageAsync(
                apiKey,
                $"{BaseUrl}/usage/embeddings?start_time={startTimestamp}&end_time={endTimestamp}&limit=30",
                cancellationToken);

            // Fetch costs
            var costsTask = FetchCostsAsync(
                apiKey,
                $"{BaseUrl}/costs?start_time={startTimestamp}&end_time={endTimestamp}&limit=30",
                cancellationToken);

            await Task.WhenAll(completionsTask, embeddingsTask, costsTask).ConfigureAwait(false);

            var completions = await completionsTask.ConfigureAwait(false);
            var embeddings = await embeddingsTask.ConfigureAwait(false);
            var costs = await costsTask.ConfigureAwait(false);

            return new OpenAiUsageData
            {
                InputTokens = (completions?.InputTokens ?? 0) + (embeddings?.InputTokens ?? 0),
                OutputTokens = completions?.OutputTokens ?? 0,
                RequestCount = (completions?.RequestCount ?? 0) + (embeddings?.RequestCount ?? 0),
                EstimatedCostUsd = costs ?? 0m,
                PeriodStart = startOfMonth,
                PeriodEnd = now,
                FetchedAt = DateTimeOffset.UtcNow
            };
        }
        catch
        {
            // Silently fail - usage data is non-critical
            return null;
        }
    }

    private async Task<UsageResult?> FetchUsageAsync(string apiKey, string url, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<UsageResponse>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (result?.Data is null)
            {
                return null;
            }

            long inputTokens = 0;
            long outputTokens = 0;
            int requestCount = 0;

            foreach (var bucket in result.Data)
            {
                foreach (var item in bucket.Results)
                {
                    inputTokens += item.InputTokens;
                    outputTokens += item.OutputTokens;
                    requestCount += item.NumModelRequests;
                }
            }

            return new UsageResult(inputTokens, outputTokens, requestCount);
        }
        catch
        {
            return null;
        }
    }

    private async Task<decimal?> FetchCostsAsync(string apiKey, string url, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<CostsResponse>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            if (result?.Data is null)
            {
                return null;
            }

            var totalCost = 0m;

            foreach (var bucket in result.Data)
            {
                foreach (var item in bucket.Results)
                {
                    // Amount is in cents, convert to dollars
                    totalCost += item.Amount.Value / 100m;
                }
            }

            return totalCost;
        }
        catch
        {
            return null;
        }
    }

    private sealed record UsageResult(long InputTokens, long OutputTokens, int RequestCount);

    // Response DTOs for OpenAI API
    private sealed class UsageResponse
    {
        [JsonPropertyName("data")]
        public List<UsageBucket> Data { get; set; } = [];
    }

    private sealed class UsageBucket
    {
        [JsonPropertyName("results")]
        public List<UsageItem> Results { get; set; } = [];
    }

    private sealed class UsageItem
    {
        [JsonPropertyName("input_tokens")]
        public long InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public long OutputTokens { get; set; }

        [JsonPropertyName("num_model_requests")]
        public int NumModelRequests { get; set; }
    }

    private sealed class CostsResponse
    {
        [JsonPropertyName("data")]
        public List<CostsBucket> Data { get; set; } = [];
    }

    private sealed class CostsBucket
    {
        [JsonPropertyName("results")]
        public List<CostsItem> Results { get; set; } = [];
    }

    private sealed class CostsItem
    {
        [JsonPropertyName("amount")]
        public CostAmount Amount { get; set; } = default!;
    }

    private sealed class CostAmount
    {
        // IMPORTANT: value is an integer (cents)
        [JsonPropertyName("value")]
        public long Value { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "usd";
    }
}
