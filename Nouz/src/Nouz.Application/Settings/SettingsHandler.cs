using Mediator;
using Nouz.Application.Logger;
using Nouz.Application.Notifications;
using Nouz.Application.Preferences;
using Nouz.Application.Store;

namespace Nouz.Application.Settings;

internal sealed class SettingsHandler :
    ICommandHandler<SettingsCommands.SaveOpenAiApiKey>
{
    private readonly IMediator _mediator;
    private readonly IPreferences _preferences;
    private readonly IActionDispatcher _actionDispatcher;
    private readonly ILoggerAdapter<SettingsHandler> _logger;

    public SettingsHandler(
        IMediator mediator,
        IPreferences preferences,
        IActionDispatcher actionDispatcher,
        ILoggerAdapter<SettingsHandler> logger)
    {
        _mediator = mediator;
        _preferences = preferences;
        _actionDispatcher = actionDispatcher;
        _logger = logger;
    }

    public async ValueTask<Unit> Handle(SettingsCommands.SaveOpenAiApiKey command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Saving OpenAI API key");

        _preferences.Set(PreferenceKeys.OpenAiApiKey, command.ApiKey);

        await _actionDispatcher.Dispatch(new SettingsActions.OpenAiApiKeyUpdated(command.ApiKey)).ConfigureAwait(false);

        await _mediator.Send(new NotificationCommands.ShowNotification(
            "Saved",
            "OpenAI API key saved successfully.",
            NotificationSeverity.Success), cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("OpenAI API key saved");

        return Unit.Value;
    }
}
