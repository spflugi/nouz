namespace Nouz.Application.OpenAi;

/// <summary>
/// Represents aggregated OpenAI usage data for a billing period.
/// </summary>
public sealed record OpenAiUsageData
{
    /// <summary>
    /// Total input tokens used across all models.
    /// </summary>
    public long InputTokens { get; init; }

    /// <summary>
    /// Total output tokens used across all models.
    /// </summary>
    public long OutputTokens { get; init; }

    /// <summary>
    /// Total number of API requests made.
    /// </summary>
    public int RequestCount { get; init; }

    /// <summary>
    /// Estimated cost in USD for the period.
    /// </summary>
    public decimal EstimatedCostUsd { get; init; }

    /// <summary>
    /// Start of the billing period.
    /// </summary>
    public DateTimeOffset PeriodStart { get; init; }

    /// <summary>
    /// End of the billing period.
    /// </summary>
    public DateTimeOffset PeriodEnd { get; init; }

    /// <summary>
    /// When this data was fetched.
    /// </summary>
    public DateTimeOffset FetchedAt { get; init; }

    /// <summary>
    /// Creates an empty usage data instance.
    /// </summary>
    public static OpenAiUsageData Empty => new()
    {
        InputTokens = 0,
        OutputTokens = 0,
        RequestCount = 0,
        EstimatedCostUsd = 0,
        PeriodStart = DateTimeOffset.UtcNow,
        PeriodEnd = DateTimeOffset.UtcNow,
        FetchedAt = DateTimeOffset.UtcNow
    };
}
