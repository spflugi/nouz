using Mediator;
using Nouz.Application.Logger;
using Nouz.Application.OpenAi;
using Nouz.Application.Preferences;
using Nouz.Application.Settings;
using Nouz.Application.Store;
using NSubstitute;
using Shouldly;

namespace Nouz.Application.Unit.Tests.Settings;

public class SettingsHandlerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IPreferences _preferences = Substitute.For<IPreferences>();
    private readonly IOpenAiUsageService _usageService = Substitute.For<IOpenAiUsageService>();
    private readonly IActionDispatcher _actionDispatcher = Substitute.For<IActionDispatcher>();
    private readonly ILoggerAdapter<SettingsHandler> _logger = Substitute.For<ILoggerAdapter<SettingsHandler>>();
    private readonly SettingsHandler _handler;

    public SettingsHandlerTests()
    {
        _handler = new SettingsHandler(_mediator, _preferences, _usageService, _actionDispatcher, _logger);
    }

    #region SaveOpenAiApiKey Tests

    [Fact]
    public async Task SaveOpenAiApiKey_ShouldSaveKeyToPreferences()
    {
        // Arrange
        var apiKey = "sk-test-key-12345";

        // Act
        await _handler.Handle(new SettingsCommands.SaveOpenAiApiKey(apiKey), TestContext.Current.CancellationToken);

        // Assert
        await _preferences.Received(1).Set(PreferenceKeys.OpenAiApiKey, apiKey);
    }

    [Fact]
    public async Task SaveOpenAiApiKey_ShouldDispatchUpdatedAction()
    {
        // Arrange
        var apiKey = "sk-test-key-12345";

        // Act
        await _handler.Handle(new SettingsCommands.SaveOpenAiApiKey(apiKey), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<SettingsActions.OpenAiApiKeyUpdated>(a => a.ApiKey == apiKey));
    }

    #endregion

    #region SaveOpenAiChatModel Tests

    [Fact]
    public async Task SaveOpenAiChatModel_ShouldSaveModelToPreferences()
    {
        // Arrange
        var model = "gpt-4";

        // Act
        await _handler.Handle(new SettingsCommands.SaveOpenAiChatModel(model), TestContext.Current.CancellationToken);

        // Assert
        await _preferences.Received(1).Set(PreferenceKeys.OpenAiChatModel, model);
    }

    [Fact]
    public async Task SaveOpenAiChatModel_WhenModelIsEmpty_ShouldUseDefault()
    {
        // Act
        await _handler.Handle(new SettingsCommands.SaveOpenAiChatModel(""), TestContext.Current.CancellationToken);

        // Assert
        await _preferences.Received(1).Set(PreferenceKeys.OpenAiChatModel, "gpt-4o-mini");
    }

    [Fact]
    public async Task SaveOpenAiChatModel_WhenModelIsWhitespace_ShouldUseDefault()
    {
        // Act
        await _handler.Handle(new SettingsCommands.SaveOpenAiChatModel("   "), TestContext.Current.CancellationToken);

        // Assert
        await _preferences.Received(1).Set(PreferenceKeys.OpenAiChatModel, "gpt-4o-mini");
    }

    [Fact]
    public async Task SaveOpenAiChatModel_ShouldTrimModel()
    {
        // Act
        await _handler.Handle(new SettingsCommands.SaveOpenAiChatModel("  gpt-4  "), TestContext.Current.CancellationToken);

        // Assert
        await _preferences.Received(1).Set(PreferenceKeys.OpenAiChatModel, "gpt-4");
    }

    [Fact]
    public async Task SaveOpenAiChatModel_ShouldDispatchUpdatedAction()
    {
        // Arrange
        var model = "gpt-4";

        // Act
        await _handler.Handle(new SettingsCommands.SaveOpenAiChatModel(model), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<SettingsActions.OpenAiChatModelUpdated>(a => a.Model == model));
    }

    #endregion

    #region SaveOpenAiEmbeddingModel Tests

    [Fact]
    public async Task SaveOpenAiEmbeddingModel_ShouldSaveModelToPreferences()
    {
        // Arrange
        var model = "text-embedding-3-large";

        // Act
        await _handler.Handle(new SettingsCommands.SaveOpenAiEmbeddingModel(model), TestContext.Current.CancellationToken);

        // Assert
        await _preferences.Received(1).Set(PreferenceKeys.OpenAiEmbeddingModel, model);
    }

    [Fact]
    public async Task SaveOpenAiEmbeddingModel_WhenModelIsEmpty_ShouldUseDefault()
    {
        // Act
        await _handler.Handle(new SettingsCommands.SaveOpenAiEmbeddingModel(""), TestContext.Current.CancellationToken);

        // Assert
        await _preferences.Received(1).Set(PreferenceKeys.OpenAiEmbeddingModel, "text-embedding-3-small");
    }

    [Fact]
    public async Task SaveOpenAiEmbeddingModel_ShouldDispatchUpdatedAction()
    {
        // Arrange
        var model = "text-embedding-3-large";

        // Act
        await _handler.Handle(new SettingsCommands.SaveOpenAiEmbeddingModel(model), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<SettingsActions.OpenAiEmbeddingModelUpdated>(a => a.Model == model));
    }

    #endregion

    #region SaveTopNRelevantNotes Tests

    [Fact]
    public async Task SaveTopNRelevantNotes_ShouldSaveCountToPreferences()
    {
        // Arrange
        var count = 5;

        // Act
        await _handler.Handle(new SettingsCommands.SaveTopNRelevantNotes(count), TestContext.Current.CancellationToken);

        // Assert
        await _preferences.Received(1).Set(PreferenceKeys.TopNRelevantNotes, "5");
    }

    [Fact]
    public async Task SaveTopNRelevantNotes_WhenCountIsNegative_ShouldClampToZero()
    {
        // Act
        await _handler.Handle(new SettingsCommands.SaveTopNRelevantNotes(-5), TestContext.Current.CancellationToken);

        // Assert
        await _preferences.Received(1).Set(PreferenceKeys.TopNRelevantNotes, "0");
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<SettingsActions.TopNRelevantNotesUpdated>(a => a.Count == 0));
    }

    [Fact]
    public async Task SaveTopNRelevantNotes_WhenCountIsGreaterThan10_ShouldClampTo10()
    {
        // Act
        await _handler.Handle(new SettingsCommands.SaveTopNRelevantNotes(15), TestContext.Current.CancellationToken);

        // Assert
        await _preferences.Received(1).Set(PreferenceKeys.TopNRelevantNotes, "10");
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<SettingsActions.TopNRelevantNotesUpdated>(a => a.Count == 10));
    }

    [Fact]
    public async Task SaveTopNRelevantNotes_WhenCountIsWithinRange_ShouldSaveAsIs()
    {
        // Act
        await _handler.Handle(new SettingsCommands.SaveTopNRelevantNotes(7), TestContext.Current.CancellationToken);

        // Assert
        await _preferences.Received(1).Set(PreferenceKeys.TopNRelevantNotes, "7");
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<SettingsActions.TopNRelevantNotesUpdated>(a => a.Count == 7));
    }

    [Fact]
    public async Task SaveTopNRelevantNotes_ShouldDispatchUpdatedAction()
    {
        // Arrange
        var count = 3;

        // Act
        await _handler.Handle(new SettingsCommands.SaveTopNRelevantNotes(count), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<SettingsActions.TopNRelevantNotesUpdated>(a => a.Count == count));
    }

    #endregion

    #region SaveThemeMode Tests

    [Fact]
    public async Task SaveThemeMode_ShouldSaveModeToPreferences()
    {
        // Arrange
        var mode = ThemeMode.Dark;

        // Act
        await _handler.Handle(new SettingsCommands.SaveThemeMode(mode), TestContext.Current.CancellationToken);

        // Assert
        await _preferences.Received(1).Set(PreferenceKeys.ThemeMode, "Dark");
    }

    [Fact]
    public async Task SaveThemeMode_ShouldDispatchUpdatedAction()
    {
        // Arrange
        var mode = ThemeMode.Dark;

        // Act
        await _handler.Handle(new SettingsCommands.SaveThemeMode(mode), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<SettingsActions.ThemeModeUpdated>(a => a.Mode == mode));
    }

    [Fact]
    public async Task SaveThemeMode_WhenLightMode_ShouldSaveLightToPreferences()
    {
        // Arrange
        var mode = ThemeMode.Light;

        // Act
        await _handler.Handle(new SettingsCommands.SaveThemeMode(mode), TestContext.Current.CancellationToken);

        // Assert
        await _preferences.Received(1).Set(PreferenceKeys.ThemeMode, "Light");
    }

    #endregion
}
