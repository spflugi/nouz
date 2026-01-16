using System.Collections.Immutable;
using Mediator;
using Nouz.Application.Logger;
using Nouz.Application.Notebooks;
using Nouz.Application.Notifications;
using Nouz.Application.Store;
using Nouz.Domain.Entities;
using Nouz.Domain.Repositories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Nouz.Application.Unit.Tests.Notebooks;

public class NotebookHandlerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly INotebookRepository _notebookRepository = Substitute.For<INotebookRepository>();
    private readonly IActionDispatcher _actionDispatcher = Substitute.For<IActionDispatcher>();
    private readonly ILoggerAdapter<NotebookHandler> _logger = Substitute.For<ILoggerAdapter<NotebookHandler>>();
    private readonly NotebookHandler _handler;

    public NotebookHandlerTests()
    {
        _handler = new NotebookHandler(_mediator, _notebookRepository, _actionDispatcher, _logger);
    }

    #region LoadAllNotebooks Tests

    [Fact]
    public async Task LoadAllNotebooks_ShouldLoadNotebooksAndDispatchAction()
    {
        // Arrange
        var notebooks = new List<Notebook>
        {
            new() { Id = Guid.NewGuid(), Name = "Notebook 1", CreatedAt = DateTimeOffset.UtcNow, LastModifiedAt = DateTimeOffset.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Notebook 2", CreatedAt = DateTimeOffset.UtcNow, LastModifiedAt = DateTimeOffset.UtcNow }
        };
        _notebookRepository.GetAll(Arg.Any<CancellationToken>()).Returns(notebooks);

        // Act
        await _handler.Handle(new NotebookCommands.LoadAllNotebooks(), CancellationToken.None);

        // Assert
        await _notebookRepository.Received(1).GetAll(Arg.Any<CancellationToken>());
        await _actionDispatcher.Received(1).Dispatch(Arg.Is<NotebookActions.AllNotebooksLoaded>(a => a.Notebooks.Count == 2));
        _logger.Received(1).LogInformation("Loading all available notebooks");
        _logger.Received(1).LogInformation("{NumNotebooks} successfully loaded", 2);
    }

    [Fact]
    public async Task LoadAllNotebooks_WhenRepositoryThrows_ShouldShowErrorNotification()
    {
        // Arrange
        var exception = new Exception("Database error");
        _notebookRepository.GetAll(Arg.Any<CancellationToken>()).Throws(exception);

        // Act
        await _handler.Handle(new NotebookCommands.LoadAllNotebooks(), CancellationToken.None);

        // Assert
        _logger.Received(1).LogError(exception, "Failed to load all notebooks");
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Error" &&
                n.Message == "Failed to load all notebooks." &&
                n.Severity == NotificationSeverity.Error),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region CreateNotebook Tests

    [Fact]
    public async Task CreateNotebook_ShouldCreateNotebookAndDispatchAction()
    {
        // Arrange
        const string title = "My New Notebook";

        // Act
        await _handler.Handle(new NotebookCommands.CreateNotebook(title), CancellationToken.None);

        // Assert
        await _notebookRepository.Received(1).Add(
            Arg.Is<Notebook>(n => n.Name == title && n.SortOrder == 1),
            Arg.Any<CancellationToken>());
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NotebookActions.NotebookAdded>(a => a.Notebook.Name == title));
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Success" &&
                n.Severity == NotificationSeverity.Success),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateNotebook_WhenTitleIsEmpty_ShouldShowWarningNotification()
    {
        // Arrange & Act
        await _handler.Handle(new NotebookCommands.CreateNotebook(""), CancellationToken.None);

        // Assert
        await _notebookRepository.DidNotReceive().Add(Arg.Any<Notebook>(), Arg.Any<CancellationToken>());
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Warning" &&
                n.Severity == NotificationSeverity.Warning),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateNotebook_ShouldTrimTitle()
    {
        // Arrange
        const string titleWithSpaces = "  My Notebook  ";

        // Act
        await _handler.Handle(new NotebookCommands.CreateNotebook(titleWithSpaces), CancellationToken.None);

        // Assert
        await _notebookRepository.Received(1).Add(
            Arg.Is<Notebook>(n => n.Name == "My Notebook"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateNotebook_ShouldSetCorrectTimestamps()
    {
        // Arrange
        const string title = "Test Notebook";
        var beforeCreate = DateTimeOffset.UtcNow;

        // Act
        await _handler.Handle(new NotebookCommands.CreateNotebook(title), CancellationToken.None);

        // Assert
        await _notebookRepository.Received(1).Add(
            Arg.Is<Notebook>(n =>
                n.CreatedAt >= beforeCreate &&
                n.LastModifiedAt >= beforeCreate &&
                n.CreatedAt == n.LastModifiedAt),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateNotebook_WhenRepositoryThrows_ShouldShowErrorNotification()
    {
        // Arrange
        var exception = new Exception("Database error");
        _notebookRepository.Add(Arg.Any<Notebook>(), Arg.Any<CancellationToken>()).Throws(exception);

        // Act
        await _handler.Handle(new NotebookCommands.CreateNotebook("Test"), CancellationToken.None);

        // Assert
        _logger.Received(1).LogError(exception, "Failed to create new notebook");
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Error" &&
                n.Message == "Failed to create new notebook." &&
                n.Severity == NotificationSeverity.Error),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region SelectNotebook Tests

    [Fact]
    public async Task SelectNotebook_ShouldDispatchNotebookSelectedAction()
    {
        // Arrange
        var notebookId = Guid.NewGuid();

        // Act
        await _handler.Handle(new NotebookCommands.SelectNotebook(notebookId), CancellationToken.None);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NotebookActions.NotebookSelected>(a => a.Id == notebookId));
        _logger.Received(1).LogInformation("Selecting the notebook with the id '{NotebookId}'", notebookId);
        _logger.Received(1).LogInformation("New notebook with id '{NotebookId}' selected", notebookId);
    }

    #endregion

    #region RenameNotebook Tests

    [Fact]
    public async Task RenameNotebook_WhenNotebookExists_ShouldUpdateAndDispatchAction()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        var existingNotebook = new Notebook
        {
            Id = notebookId,
            Name = "Old Name",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            LastModifiedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };
        _notebookRepository.GetById(notebookId, Arg.Any<CancellationToken>()).Returns(existingNotebook);

        // Act
        await _handler.Handle(new NotebookCommands.RenameNotebook(notebookId, "New Name"), CancellationToken.None);

        // Assert
        await _notebookRepository.Received(1).Update(
            Arg.Is<Notebook>(n =>
                n.Id == notebookId &&
                n.Name == "New Name" &&
                n.CreatedAt == existingNotebook.CreatedAt &&
                n.LastModifiedAt > existingNotebook.LastModifiedAt),
            Arg.Any<CancellationToken>());
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NotebookActions.NotebookUpdated>(a => a.Notebook.Name == "New Name"));
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Success" &&
                n.Severity == NotificationSeverity.Success),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RenameNotebook_WhenNotebookDoesNotExist_ShouldShowErrorNotification()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        _notebookRepository.GetById(notebookId, Arg.Any<CancellationToken>()).Returns((Notebook?)null);

        // Act
        await _handler.Handle(new NotebookCommands.RenameNotebook(notebookId, "New Name"), CancellationToken.None);

        // Assert
        await _notebookRepository.DidNotReceive().Update(Arg.Any<Notebook>(), Arg.Any<CancellationToken>());
        await _actionDispatcher.DidNotReceive().Dispatch(Arg.Any<NotebookActions.NotebookUpdated>());
        _logger.Received(1).LogWarning("Cannot rename notebook. Notebook with id '{NotebookId}' not found", notebookId);
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Error" &&
                n.Message == "Notebook not found." &&
                n.Severity == NotificationSeverity.Error),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeleteNotebook Tests

    [Fact]
    public async Task DeleteNotebook_ShouldDeleteAndDispatchAction()
    {
        // Arrange
        var notebookId = Guid.NewGuid();

        // Act
        await _handler.Handle(new NotebookCommands.DeleteNotebook(notebookId), CancellationToken.None);

        // Assert
        await _notebookRepository.Received(1).Delete(notebookId, Arg.Any<CancellationToken>());
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Deleted" &&
                n.Severity == NotificationSeverity.Success),
            Arg.Any<CancellationToken>());
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NotebookActions.NotebookDeleted>(a => a.Id == notebookId));
    }

    [Fact]
    public async Task DeleteNotebook_ShouldLogDeletion()
    {
        // Arrange
        var notebookId = Guid.NewGuid();

        // Act
        await _handler.Handle(new NotebookCommands.DeleteNotebook(notebookId), CancellationToken.None);

        // Assert
        _logger.Received(1).LogInformation("Deleting notebook with id '{NotebookId}'", notebookId);
        _logger.Received(1).LogInformation("Notebook with id '{NotebookId}' deleted", notebookId);
    }

    [Fact]
    public async Task DeleteNotebook_WhenRepositoryThrows_ShouldShowErrorNotification()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        var exception = new Exception("Database error");
        _notebookRepository.Delete(notebookId, Arg.Any<CancellationToken>()).Throws(exception);

        // Act
        await _handler.Handle(new NotebookCommands.DeleteNotebook(notebookId), CancellationToken.None);

        // Assert
        _logger.Received(1).LogError(exception, "Failed to delete notebook with id '{NotebookId}'", notebookId);
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Error" &&
                n.Severity == NotificationSeverity.Error),
            Arg.Any<CancellationToken>());
        await _actionDispatcher.DidNotReceive().Dispatch(Arg.Any<NotebookActions.NotebookDeleted>());
    }

    #endregion
}
