namespace Nouz.Application.OpenAi;

/// <summary>
/// Service to retrieve OpenAI API usage and cost data.
/// </summary>
public interface IOpenAiUsageService
{
    /// <summary>
    /// Gets usage data for the current billing month.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Usage data for the current month, or null if unavailable.</returns>
    Task<OpenAiUsageData?> GetCurrentMonthUsageAsync(CancellationToken cancellationToken = default);
}
