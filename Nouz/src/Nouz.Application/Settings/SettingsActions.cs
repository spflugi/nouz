using Nouz.Application.Store;

namespace Nouz.Application.Settings;

public static class SettingsActions
{
    /// <summary>
    /// Triggered when the OpenAI API key is updated.
    /// </summary>
    public sealed record OpenAiApiKeyUpdated(string? ApiKey) : IAction;
}
