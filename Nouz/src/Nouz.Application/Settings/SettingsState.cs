using Nouz.Application.OpenAi;

namespace Nouz.Application.Settings;

public sealed record SettingsState
{
    public ThemeMode ThemeMode { get; init; } = ThemeMode.Light;

    public string? OpenAiApiKey { get; init; }
    public string? OpenAiAdminKey { get; init; }
    public string OpenAiChatModel { get; init; } = "gpt-4o-mini";
    public string OpenAiEmbeddingModel { get; init; } = "text-embedding-3-small";
    public int TopNRelevantNotes { get; init; } = 3;
    public float MinSimilarityThreshold { get; init; } = 0.3f;

    /// <summary>
    /// OpenAI usage data for the current billing period.
    /// </summary>
    public OpenAiUsageData? OpenAiUsage { get; init; }

    /// <summary>
    /// Whether usage data is currently being fetched.
    /// </summary>
    public bool IsLoadingUsage { get; init; }
}
