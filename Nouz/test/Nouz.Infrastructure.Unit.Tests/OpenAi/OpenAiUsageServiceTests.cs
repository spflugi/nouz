using System.Net;
using System.Text.Json;
using Nouz.Application.Preferences;
using Nouz.Infrastructure.OpenAi;
using NSubstitute;
using Shouldly;
using IPreferences = Nouz.Application.Preferences.IPreferences;

namespace Nouz.Infrastructure.Unit.Tests.OpenAi;

public class OpenAiUsageServiceTests
{
    private readonly IPreferences _preferences = Substitute.For<IPreferences>();
    private readonly MockHttpMessageHandler _messageHandler;
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly OpenAiUsageService _service;

    public OpenAiUsageServiceTests()
    {
        _messageHandler = new MockHttpMessageHandler();
        var httpClient = new HttpClient(_messageHandler);
        _httpClientFactory.CreateClient("OpenAI").Returns(httpClient);
        _service = new OpenAiUsageService(_preferences, _httpClientFactory);
    }

    [Fact]
    public async Task GetCurrentMonthUsageAsync_WhenApiKeyIsNull_ReturnsNull()
    {
        // Arrange
        _preferences.Get(PreferenceKeys.OpenAiApiAdminKey).Returns(Task.FromResult<string?>(null));

        // Act
        var result = await _service.GetCurrentMonthUsageAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetCurrentMonthUsageAsync_WhenApiKeyIsEmpty_ReturnsNull()
    {
        // Arrange
        _preferences.Get(PreferenceKeys.OpenAiApiAdminKey).Returns(Task.FromResult<string?>(string.Empty));

        // Act
        var result = await _service.GetCurrentMonthUsageAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetCurrentMonthUsageAsync_WhenApiKeyIsWhitespace_ReturnsNull()
    {
        // Arrange
        _preferences.Get(PreferenceKeys.OpenAiApiAdminKey).Returns(Task.FromResult<string?>("   "));

        // Act
        var result = await _service.GetCurrentMonthUsageAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetCurrentMonthUsageAsync_WhenApiReturnsValidData_ParsesCorrectly()
    {
        // Arrange
        _preferences.Get(PreferenceKeys.OpenAiApiAdminKey).Returns(Task.FromResult<string?>("sk-test-key"));

        var completionsResponse = new
        {
            data = new[]
            {
                new
                {
                    results = new[]
                    {
                        new { input_tokens = 100L, output_tokens = 50L, num_model_requests = 5 },
                        new { input_tokens = 200L, output_tokens = 100L, num_model_requests = 10 }
                    }
                }
            }
        };

        var embeddingsResponse = new
        {
            data = new[]
            {
                new
                {
                    results = new[]
                    {
                        new { input_tokens = 500L, output_tokens = 0L, num_model_requests = 20 }
                    }
                }
            }
        };

        var costsResponse = new
        {
            data = new[]
            {
                new
                {
                    results = new[]
                    {
                        new { amount = new { value = 150L, currency = "usd" } },
                        new { amount = new { value = 50L, currency = "usd" } }
                    }
                }
            }
        };

        _messageHandler.SetupResponses(new Dictionary<string, string>
        {
            { "completions", JsonSerializer.Serialize(completionsResponse) },
            { "embeddings", JsonSerializer.Serialize(embeddingsResponse) },
            { "costs", JsonSerializer.Serialize(costsResponse) }
        });

        // Act
        var result = await _service.GetCurrentMonthUsageAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.InputTokens.ShouldBe(800); // 100 + 200 + 500
        result.OutputTokens.ShouldBe(150); // 50 + 100 (embeddings don't have output tokens)
        result.RequestCount.ShouldBe(35); // 5 + 10 + 20
        result.EstimatedCostUsd.ShouldBe(2.00m); // (150 + 50) / 100
    }

    [Fact]
    public async Task GetCurrentMonthUsageAsync_WhenApiReturnsError_ReturnsDataWithZeros()
    {
        // Arrange
        _preferences.Get(PreferenceKeys.OpenAiApiAdminKey).Returns(Task.FromResult<string?>("sk-test-key"));
        _messageHandler.SetupErrorResponse(HttpStatusCode.Unauthorized);

        // Act
        var result = await _service.GetCurrentMonthUsageAsync(TestContext.Current.CancellationToken);

        // Assert - service gracefully handles errors and returns data with zeros
        result.ShouldNotBeNull();
        result.InputTokens.ShouldBe(0);
        result.OutputTokens.ShouldBe(0);
        result.RequestCount.ShouldBe(0);
        result.EstimatedCostUsd.ShouldBe(0);
    }

    [Fact]
    public async Task GetCurrentMonthUsageAsync_WhenIndividualApiFails_ReturnsDataWithZeros()
    {
        // Arrange
        _preferences.Get(PreferenceKeys.OpenAiApiAdminKey).Returns(Task.FromResult<string?>("sk-test-key"));
        _messageHandler.SetupException(new HttpRequestException("Network error"));

        // Act
        var result = await _service.GetCurrentMonthUsageAsync(TestContext.Current.CancellationToken);

        // Assert - service gracefully handles errors and returns data with zeros
        result.ShouldNotBeNull();
        result.InputTokens.ShouldBe(0);
        result.OutputTokens.ShouldBe(0);
        result.RequestCount.ShouldBe(0);
        result.EstimatedCostUsd.ShouldBe(0);
    }

    [Fact]
    public async Task GetCurrentMonthUsageAsync_SetsPeriodDatesCorrectly()
    {
        // Arrange
        _preferences.Get(PreferenceKeys.OpenAiApiAdminKey).Returns(Task.FromResult<string?>("sk-test-key"));

        var emptyResponse = new { data = Array.Empty<object>() };
        var emptyJson = JsonSerializer.Serialize(emptyResponse);

        _messageHandler.SetupResponses(new Dictionary<string, string>
        {
            { "completions", emptyJson },
            { "embeddings", emptyJson },
            { "costs", emptyJson }
        });

        var now = DateTimeOffset.UtcNow;
        var expectedStartOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = await _service.GetCurrentMonthUsageAsync(TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.PeriodStart.ShouldBe(expectedStartOfMonth);
        result.PeriodEnd.Year.ShouldBe(now.Year);
        result.PeriodEnd.Month.ShouldBe(now.Month);
        result.FetchedAt.ShouldBeGreaterThanOrEqualTo(now);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private Dictionary<string, string>? _responses;
        private HttpStatusCode? _errorStatusCode;
        private Exception? _exception;

        public void SetupResponses(Dictionary<string, string> responses)
        {
            _responses = responses;
            _errorStatusCode = null;
            _exception = null;
        }

        public void SetupErrorResponse(HttpStatusCode statusCode)
        {
            _errorStatusCode = statusCode;
            _responses = null;
            _exception = null;
        }

        public void SetupException(Exception exception)
        {
            _exception = exception;
            _responses = null;
            _errorStatusCode = null;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_exception is not null)
            {
                throw _exception;
            }

            if (_errorStatusCode.HasValue)
            {
                return Task.FromResult(new HttpResponseMessage(_errorStatusCode.Value));
            }

            if (_responses is null)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}")
                });
            }

            var uri = request.RequestUri?.ToString() ?? string.Empty;
            string content;

            if (uri.Contains("completions"))
            {
                content = _responses.GetValueOrDefault("completions", "{}");
            }
            else if (uri.Contains("embeddings"))
            {
                content = _responses.GetValueOrDefault("embeddings", "{}");
            }
            else if (uri.Contains("costs"))
            {
                content = _responses.GetValueOrDefault("costs", "{}");
            }
            else
            {
                content = "{}";
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
