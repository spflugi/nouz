using Mediator;

namespace Nouz.Application.Settings;

public static class SettingsCommands
{
    /// <summary>
    /// Save the OpenAI API key to preferences.
    /// </summary>
    public sealed record SaveOpenAiApiKey(string ApiKey) : ICommand;
}
