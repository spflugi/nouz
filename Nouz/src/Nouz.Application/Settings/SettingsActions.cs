using Nouz.Application.OpenAi;
using Nouz.Application.Store;

namespace Nouz.Application.Settings;

public static class SettingsActions
{
    /// <summary>
    /// Triggered when the OpenAI API key is updated.
    /// </summary>
    public sealed record OpenAiApiKeyUpdated(string? ApiKey) : IAction;

    /// <summary>
    /// Triggered when the OpenAI admin key is updated.
    /// </summary>
    public sealed record OpenAiAdminKeyUpdated(string? AdminKey) : IAction;

    /// <summary>
    /// Triggered when the OpenAI chat model is updated.
    /// </summary>
    public sealed record OpenAiChatModelUpdated(string Model) : IAction;

    /// <summary>
    /// Triggered when the OpenAI embedding model is updated.
    /// </summary>
    public sealed record OpenAiEmbeddingModelUpdated(string Model) : IAction;

    /// <summary>
    /// Triggered when the number of relevant notes setting is updated.
    /// </summary>
    public sealed record TopNRelevantNotesUpdated(int Count) : IAction;

    /// <summary>
    /// Triggered when usage data loading starts.
    /// </summary>
    public sealed record OpenAiUsageLoadingStarted : IAction;

    /// <summary>
    /// Triggered when usage data is loaded.
    /// </summary>
    public sealed record OpenAiUsageLoaded(OpenAiUsageData? Usage) : IAction;
}
