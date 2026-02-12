using Mediator;
using Nouz.Application.Lifecycle;
using Nouz.Application.Logger;
using Nouz.Application.Notebooks;
using Nouz.Application.Preferences;
using Nouz.Application.Settings;
using Nouz.Application.Store;
using Nouz.Domain.Repositories;
using NSubstitute;
using Shouldly;

namespace Nouz.Application.Unit.Tests.Lifecycle;

public class LifecycleHandlerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IPreferences _preferences = Substitute.For<IPreferences>();
    private readonly IDbMigrator _dbMigrator = Substitute.For<IDbMigrator>();
    private readonly IActionDispatcher _actionDispatcher = Substitute.For<IActionDispatcher>();
    private readonly ILoggerAdapter<LifecycleHandler> _logger = Substitute.For<ILoggerAdapter<LifecycleHandler>>();
    private readonly LifecycleHandler _handler;

    public LifecycleHandlerTests()
    {
        _handler = new LifecycleHandler(_mediator, _preferences, _dbMigrator, _actionDispatcher, _logger);
    }

    #region PerformOnAppStart Tests

    [Fact]
    public async Task PerformOnAppStart_ShouldApplyMigrations()
    {
        // Act
        await _handler.Handle(new LifecycleCommands.PerformOnAppStart(), TestContext.Current.CancellationToken);

        // Assert
        await _dbMigrator.Received(1).ApplyMigrations(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PerformOnAppStart_ShouldLoadNotebooks()
    {
        // Act
        await _handler.Handle(new LifecycleCommands.PerformOnAppStart(), TestContext.Current.CancellationToken);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Any<NotebookCommands.LoadAllNotebooks>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PerformOnAppStart_WhenSelectedNotebookExists_ShouldDispatchNotebookSelected()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        _preferences.Get(PreferenceKeys.SelectedNotebookId, null).Returns(notebookId.ToString());

        // Act
        await _handler.Handle(new LifecycleCommands.PerformOnAppStart(), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NotebookActions.NotebookSelected>(a => a.Id == notebookId));
    }

    [Fact]
    public async Task PerformOnAppStart_WhenNoSelectedNotebook_ShouldNotDispatchNotebookSelected()
    {
        // Arrange
        _preferences.Get(PreferenceKeys.SelectedNotebookId, null).Returns((string?)null);

        // Act
        await _handler.Handle(new LifecycleCommands.PerformOnAppStart(), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.DidNotReceive().Dispatch(Arg.Any<NotebookActions.NotebookSelected>());
    }

    [Fact]
    public async Task PerformOnAppStart_WhenInvalidSelectedNotebookId_ShouldNotDispatchNotebookSelected()
    {
        // Arrange
        _preferences.Get(PreferenceKeys.SelectedNotebookId, null).Returns("not-a-guid");

        // Act
        await _handler.Handle(new LifecycleCommands.PerformOnAppStart(), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.DidNotReceive().Dispatch(Arg.Any<NotebookActions.NotebookSelected>());
    }

    [Fact]
    public async Task PerformOnAppStart_WhenOpenAiApiKeyExists_ShouldDispatchApiKeyUpdated()
    {
        // Arrange
        var apiKey = "sk-test-key";
        _preferences.Get(PreferenceKeys.OpenAiApiKey, null).Returns(apiKey);

        // Act
        await _handler.Handle(new LifecycleCommands.PerformOnAppStart(), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<SettingsActions.OpenAiApiKeyUpdated>(a => a.ApiKey == apiKey));
    }

    [Fact]
    public async Task PerformOnAppStart_WhenNoOpenAiApiKey_ShouldNotDispatchApiKeyUpdated()
    {
        // Arrange
        _preferences.Get(PreferenceKeys.OpenAiApiKey, null).Returns((string?)null);

        // Act
        await _handler.Handle(new LifecycleCommands.PerformOnAppStart(), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.DidNotReceive().Dispatch(Arg.Any<SettingsActions.OpenAiApiKeyUpdated>());
    }

    [Fact]
    public async Task PerformOnAppStart_WhenOpenAiAdminKeyExists_ShouldDispatchAdminKeyUpdated()
    {
        // Arrange
        var adminKey = "admin-key";
        _preferences.Get(PreferenceKeys.OpenAiApiAdminKey, null).Returns(adminKey);

        // Act
        await _handler.Handle(new LifecycleCommands.PerformOnAppStart(), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<SettingsActions.OpenAiAdminKeyUpdated>(a => a.AdminKey == adminKey));
    }

    [Fact]
    public async Task PerformOnAppStart_WhenChatModelExists_ShouldDispatchChatModelUpdated()
    {
        // Arrange
        var chatModel = "gpt-4";
        _preferences.Get(PreferenceKeys.OpenAiChatModel, null).Returns(chatModel);

        // Act
        await _handler.Handle(new LifecycleCommands.PerformOnAppStart(), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<SettingsActions.OpenAiChatModelUpdated>(a => a.Model == chatModel));
    }

    [Fact]
    public async Task PerformOnAppStart_WhenEmbeddingModelExists_ShouldDispatchEmbeddingModelUpdated()
    {
        // Arrange
        var embeddingModel = "text-embedding-3-large";
        _preferences.Get(PreferenceKeys.OpenAiEmbeddingModel, null).Returns(embeddingModel);

        // Act
        await _handler.Handle(new LifecycleCommands.PerformOnAppStart(), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<SettingsActions.OpenAiEmbeddingModelUpdated>(a => a.Model == embeddingModel));
    }

    [Fact]
    public async Task PerformOnAppStart_ShouldLogInformation()
    {
        // Act
        await _handler.Handle(new LifecycleCommands.PerformOnAppStart(), TestContext.Current.CancellationToken);

        // Assert
        _logger.Received(1).LogInformation("Initialize app lifecycle on start.");
    }

    #endregion

    #region PerformOnAppResume Tests

    [Fact]
    public async Task PerformOnAppResume_ShouldLogInformation()
    {
        // Act
        await _handler.Handle(new LifecycleCommands.PerformOnAppResume(), TestContext.Current.CancellationToken);

        // Assert
        _logger.Received(1).LogInformation("Initialize app lifecycle on resume.");
    }

    [Fact]
    public async Task PerformOnAppResume_ShouldReturnUnit()
    {
        // Act
        var result = await _handler.Handle(new LifecycleCommands.PerformOnAppResume(), TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(Mediator.Unit.Value);
    }

    #endregion

    #region PerformOnAppSleep Tests

    [Fact]
    public async Task PerformOnAppSleep_ShouldLogInformation()
    {
        // Act
        await _handler.Handle(new LifecycleCommands.PerformOnAppSleep(), TestContext.Current.CancellationToken);

        // Assert
        _logger.Received(1).LogInformation("App going to sleep.");
    }

    [Fact]
    public async Task PerformOnAppSleep_ShouldReturnUnit()
    {
        // Act
        var result = await _handler.Handle(new LifecycleCommands.PerformOnAppSleep(), TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBe(Mediator.Unit.Value);
    }

    #endregion
}
