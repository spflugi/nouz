namespace Nouz.Application.Settings;

public sealed record SettingsState
{
    public string? OpenAiApiKey { get; init; }
}
