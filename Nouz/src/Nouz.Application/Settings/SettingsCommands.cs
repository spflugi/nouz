using Mediator;

namespace Nouz.Application.Settings;

public static class SettingsCommands
{
    /// <summary>
    /// Save the OpenAI API key to preferences.
    /// </summary>
    public sealed record SaveOpenAiApiKey(string ApiKey) : ICommand;

    /// <summary>
    /// Save the OpenAI API key to preferences.
    /// </summary>
    public sealed record SaveOpenAiAdminKey(string AdminKey) : ICommand;

    /// <summary>
    /// Save the OpenAI chat model to preferences.
    /// </summary>
    public sealed record SaveOpenAiChatModel(string Model) : ICommand;

    /// <summary>
    /// Save the OpenAI embedding model to preferences.
    /// </summary>
    public sealed record SaveOpenAiEmbeddingModel(string Model) : ICommand;

    /// <summary>
    /// Save the number of relevant notes to include in chat context.
    /// </summary>
    public sealed record SaveTopNRelevantNotes(int Count) : ICommand;

    /// <summary>
    /// Load OpenAI usage data for the current billing period.
    /// </summary>
    public sealed record LoadOpenAiUsage : ICommand;

    /// <summary>
    /// Save the theme mode (Light/Dark) to preferences.
    /// </summary>
    public sealed record SaveThemeMode(ThemeMode Mode) : ICommand;
}
