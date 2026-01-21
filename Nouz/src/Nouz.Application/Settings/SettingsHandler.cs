using Mediator;
using Nouz.Application.Logger;
using Nouz.Application.Notifications;
using Nouz.Application.OpenAi;
using Nouz.Application.Preferences;
using Nouz.Application.Store;

namespace Nouz.Application.Settings;

internal sealed class SettingsHandler :
    ICommandHandler<SettingsCommands.SaveOpenAiApiKey>,
    ICommandHandler<SettingsCommands.SaveOpenAiAdminKey>,
    ICommandHandler<SettingsCommands.SaveOpenAiChatModel>,
    ICommandHandler<SettingsCommands.SaveOpenAiEmbeddingModel>,
    ICommandHandler<SettingsCommands.SaveTopNRelevantNotes>,
    ICommandHandler<SettingsCommands.LoadOpenAiUsage>
{
    private readonly IMediator _mediator;
    private readonly IPreferences _preferences;
    private readonly IOpenAiUsageService _usageService;
    private readonly IActionDispatcher _actionDispatcher;
    private readonly ILoggerAdapter<SettingsHandler> _logger;

    public SettingsHandler(
        IMediator mediator,
        IPreferences preferences,
        IOpenAiUsageService usageService,
        IActionDispatcher actionDispatcher,
        ILoggerAdapter<SettingsHandler> logger)
    {
        _mediator = mediator;
        _preferences = preferences;
        _usageService = usageService;
        _actionDispatcher = actionDispatcher;
        _logger = logger;
    }

    public async ValueTask<Unit> Handle(SettingsCommands.SaveOpenAiApiKey command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Saving OpenAI API key");

        await _preferences.Set(PreferenceKeys.OpenAiApiKey, command.ApiKey).ConfigureAwait(false);

        await _actionDispatcher.Dispatch(new SettingsActions.OpenAiApiKeyUpdated(command.ApiKey)).ConfigureAwait(false);

        await _mediator.Send(new NotificationCommands.ShowNotification(
            "Saved",
            "OpenAI API key saved successfully.",
            NotificationSeverity.Success), cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("OpenAI API key saved");

        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(SettingsCommands.SaveOpenAiAdminKey command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Saving OpenAI Admin key");

        await _preferences.Set(PreferenceKeys.OpenAiApiAdminKey, command.AdminKey).ConfigureAwait(false);

        await _actionDispatcher.Dispatch(new SettingsActions.OpenAiAdminKeyUpdated(command.AdminKey)).ConfigureAwait(false);

        await _mediator.Send(new NotificationCommands.ShowNotification(
            "Saved",
            "OpenAI admin key saved successfully.",
            NotificationSeverity.Success), cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("OpenAI admin key saved");

        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(SettingsCommands.SaveOpenAiChatModel command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Saving OpenAI chat model: {Model}", command.Model);

        var model = string.IsNullOrWhiteSpace(command.Model) ? "gpt-4o-mini" : command.Model.Trim();
        await _preferences.Set(PreferenceKeys.OpenAiChatModel, model).ConfigureAwait(false);

        await _actionDispatcher.Dispatch(new SettingsActions.OpenAiChatModelUpdated(model)).ConfigureAwait(false);

        await _mediator.Send(new NotificationCommands.ShowNotification(
            "Saved",
            "OpenAI chat model saved successfully.",
            NotificationSeverity.Success), cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("OpenAI chat model saved: {Model}", model);

        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(SettingsCommands.SaveOpenAiEmbeddingModel command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Saving OpenAI embedding model: {Model}", command.Model);

        var model = string.IsNullOrWhiteSpace(command.Model) ? "text-embedding-3-small" : command.Model.Trim();
        await _preferences.Set(PreferenceKeys.OpenAiEmbeddingModel, model).ConfigureAwait(false);

        await _actionDispatcher.Dispatch(new SettingsActions.OpenAiEmbeddingModelUpdated(model)).ConfigureAwait(false);

        await _mediator.Send(new NotificationCommands.ShowNotification(
            "Saved",
            "OpenAI embedding model saved successfully.",
            NotificationSeverity.Success), cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("OpenAI embedding model saved: {Model}", model);

        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(SettingsCommands.SaveTopNRelevantNotes command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Saving TopNRelevantNotes setting: {Count}", command.Count);

        var count = Math.Clamp(command.Count, 0, 10);
        await _preferences.Set(PreferenceKeys.TopNRelevantNotes, count.ToString()).ConfigureAwait(false);

        await _actionDispatcher.Dispatch(new SettingsActions.TopNRelevantNotesUpdated(count)).ConfigureAwait(false);

        _logger.LogInformation("TopNRelevantNotes saved: {Count}", count);

        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(SettingsCommands.LoadOpenAiUsage command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Loading OpenAI usage data");

        await _actionDispatcher.Dispatch(new SettingsActions.OpenAiUsageLoadingStarted()).ConfigureAwait(false);

        try
        {
            var usage = await _usageService.GetCurrentMonthUsageAsync(cancellationToken).ConfigureAwait(false);
            await _actionDispatcher.Dispatch(new SettingsActions.OpenAiUsageLoaded(usage)).ConfigureAwait(false);

            if (usage is not null)
            {
                _logger.LogDebug("OpenAI usage loaded: {InputTokens} in, {OutputTokens} out, ${Cost:F2}",
                    usage.InputTokens, usage.OutputTokens, usage.EstimatedCostUsd);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load OpenAI usage data");
            await _actionDispatcher.Dispatch(new SettingsActions.OpenAiUsageLoaded(null)).ConfigureAwait(false);
        }

        return Unit.Value;
    }
}
