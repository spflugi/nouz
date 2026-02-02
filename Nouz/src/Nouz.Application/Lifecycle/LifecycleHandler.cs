using Mediator;
using Nouz.Application.Logger;
using Nouz.Application.Notebooks;
using Nouz.Application.Preferences;
using Nouz.Application.Settings;
using Nouz.Application.Store;
using Nouz.Domain.Repositories;

namespace Nouz.Application.Lifecycle;

internal sealed class LifecycleHandler :
    ICommandHandler<LifecycleCommands.PerformOnAppStart>,
    ICommandHandler<LifecycleCommands.PerformOnAppResume>,
    ICommandHandler<LifecycleCommands.PerformOnAppSleep>
{
    private readonly IMediator _mediator;
    private readonly IPreferences _preferences;
    private readonly IDbMigrator _dbMigrator;
    private readonly IActionDispatcher _actionDispatcher;
    private readonly ILoggerAdapter<LifecycleHandler> _logger;

    public LifecycleHandler(IMediator mediator, IPreferences preferences, IDbMigrator dbMigrator,
        IActionDispatcher actionDispatcher, ILoggerAdapter<LifecycleHandler> logger)
    {
        _mediator = mediator;
        _preferences = preferences;
        _dbMigrator = dbMigrator;
        _actionDispatcher = actionDispatcher;
        _logger = logger;
    }

    public async ValueTask<Unit> Handle(LifecycleCommands.PerformOnAppStart command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initialize app lifecycle on start.");

        await _dbMigrator.ApplyMigrations(cancellationToken).ConfigureAwait(false);
        await _mediator.Send(new NotebookCommands.LoadAllNotebooks(), cancellationToken).ConfigureAwait(false);

        await LoadLastSelectedNotebookId().ConfigureAwait(false);
        await LoadOpenAiSettings().ConfigureAwait(false);

        return Unit.Value;
    }

    public ValueTask<Unit> Handle(LifecycleCommands.PerformOnAppResume command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initialize app lifecycle on resume.");
        return ValueTask.FromResult(Unit.Value);
    }

    public ValueTask<Unit> Handle(LifecycleCommands.PerformOnAppSleep command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("App going to sleep.");
        return ValueTask.FromResult(Unit.Value);
    }

    private async Task LoadLastSelectedNotebookId()
    {
        var selectedNotebookIdValue = await _preferences.Get(PreferenceKeys.SelectedNotebookId).ConfigureAwait(false);

        if (selectedNotebookIdValue is not null && Guid.TryParse(selectedNotebookIdValue, out var selectedNotebookId))
        {
            await _actionDispatcher.Dispatch(new NotebookActions.NotebookSelected(selectedNotebookId)).ConfigureAwait(false);
        }
    }

    private async Task LoadOpenAiSettings()
    {
        var openAiApiKey = await _preferences.Get(PreferenceKeys.OpenAiApiKey).ConfigureAwait(false);

        if (openAiApiKey is not null)
        {
            await _actionDispatcher.Dispatch(new SettingsActions.OpenAiApiKeyUpdated(openAiApiKey)).ConfigureAwait(false);
        }

        var openAiAdminKey = await _preferences.Get(PreferenceKeys.OpenAiApiAdminKey).ConfigureAwait(false);

        if (openAiAdminKey is not null)
        {
            await _actionDispatcher.Dispatch(new SettingsActions.OpenAiAdminKeyUpdated(openAiAdminKey)).ConfigureAwait(false);
        }

        var chatModel = await _preferences.Get(PreferenceKeys.OpenAiChatModel).ConfigureAwait(false);

        if (chatModel is not null)
        {
            await _actionDispatcher.Dispatch(new SettingsActions.OpenAiChatModelUpdated(chatModel)).ConfigureAwait(false);
        }

        var embeddingModel = await _preferences.Get(PreferenceKeys.OpenAiEmbeddingModel).ConfigureAwait(false);

        if (embeddingModel is not null)
        {
            await _actionDispatcher.Dispatch(new SettingsActions.OpenAiEmbeddingModelUpdated(embeddingModel)).ConfigureAwait(false);
        }

        var themeModeValue = await _preferences.Get(PreferenceKeys.ThemeMode).ConfigureAwait(false);

        if (themeModeValue is not null && Enum.TryParse<ThemeMode>(themeModeValue, out var themeMode))
        {
            await _actionDispatcher.Dispatch(new SettingsActions.ThemeModeUpdated(themeMode)).ConfigureAwait(false);
        }
    }
}