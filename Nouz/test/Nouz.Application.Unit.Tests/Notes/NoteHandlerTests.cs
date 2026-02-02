using System.Collections.Immutable;
using Mediator;
using Nouz.Application.Embeddings;
using Nouz.Application.Logger;
using Nouz.Application.Notes;
using Nouz.Application.Notifications;
using Nouz.Application.Store;
using Nouz.Domain.Entities;
using Nouz.Domain.Repositories;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Nouz.Application.Unit.Tests.Notes;

public class NoteHandlerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly INoteRepository _noteRepository = Substitute.For<INoteRepository>();
    private readonly IEmbeddingService _embeddingService = Substitute.For<IEmbeddingService>();
    private readonly IEmbeddingRepository _embeddingRepository = Substitute.For<IEmbeddingRepository>();
    private readonly IAttachmentRepository _attachmentRepository = Substitute.For<IAttachmentRepository>();
    private readonly INoteAttachmentRepository _noteAttachmentRepository = Substitute.For<INoteAttachmentRepository>();
    private readonly IStateProvider _stateProvider = Substitute.For<IStateProvider>();
    private readonly IActionDispatcher _actionDispatcher = Substitute.For<IActionDispatcher>();
    private readonly ILoggerAdapter<NoteHandler> _logger = Substitute.For<ILoggerAdapter<NoteHandler>>();
    private readonly NoteExportService _noteExportService;
    private readonly NoteHandler _handler;

    public NoteHandlerTests()
    {
        _noteExportService = new NoteExportService(_attachmentRepository);
        _handler = new NoteHandler(
            _mediator,
            _noteRepository,
            _embeddingService,
            _embeddingRepository,
            _attachmentRepository,
            _noteAttachmentRepository,
            _stateProvider,
            _actionDispatcher,
            _logger,
            _noteExportService);
    }

    #region LoadNotesForNotebook Tests

    [Fact]
    public async Task LoadNotesForNotebook_ShouldLoadAndDispatchNotes()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        var notes = new List<Note>
        {
            CreateNote(notebookId, DateTimeOffset.UtcNow.AddDays(-1)),
            CreateNote(notebookId, DateTimeOffset.UtcNow)
        };
        _noteRepository.GetAllByNotebook(notebookId, Arg.Any<CancellationToken>()).Returns(notes);

        // Act
        await _handler.Handle(new NoteCommands.LoadNotesForNotebook(notebookId), CancellationToken.None);

        // Assert
        await _noteRepository.Received(1).GetAllByNotebook(notebookId, Arg.Any<CancellationToken>());
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NoteActions.NotesLoaded>(a => a.Notes.Count == 2));
        _logger.Received(1).LogInformation("Loading notes for notebook '{NotebookId}'", notebookId);
    }

    [Fact]
    public async Task LoadNotesForNotebook_ShouldSortByCreatedAtDescending()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        var olderNote = CreateNote(notebookId, DateTimeOffset.UtcNow.AddDays(-2));
        var newerNote = CreateNote(notebookId, DateTimeOffset.UtcNow);
        var notes = new List<Note> { olderNote, newerNote };
        _noteRepository.GetAllByNotebook(notebookId, Arg.Any<CancellationToken>()).Returns(notes);

        ImmutableList<Note>? capturedNotes = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NotesLoaded>(a => capturedNotes = a.Notes));

        // Act
        await _handler.Handle(new NoteCommands.LoadNotesForNotebook(notebookId), CancellationToken.None);

        // Assert
        capturedNotes.ShouldNotBeNull();
        capturedNotes[0].Id.ShouldBe(newerNote.Id);
        capturedNotes[1].Id.ShouldBe(olderNote.Id);
    }

    [Fact]
    public async Task LoadNotesForNotebook_WhenRepositoryThrows_ShouldShowErrorNotification()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        var exception = new Exception("Database error");
        _noteRepository.GetAllByNotebook(notebookId, Arg.Any<CancellationToken>()).Throws(exception);

        // Act
        await _handler.Handle(new NoteCommands.LoadNotesForNotebook(notebookId), CancellationToken.None);

        // Assert
        _logger.Received(1).LogError(exception, "Failed to load notes for notebook '{NotebookId}'", notebookId);
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Error" &&
                n.Message == "Failed to load notes." &&
                n.Severity == NotificationSeverity.Error),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region CreateNote Tests

    [Fact]
    public async Task CreateNote_ShouldCreateNoteWithInitialBlock()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        _embeddingService.GenerateEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<float>());

        // Act
        await _handler.Handle(new NoteCommands.CreateNote(notebookId), CancellationToken.None);

        // Assert
        await _noteRepository.Received(1).Add(
            Arg.Is<Note>(n =>
                n.NotebookId == notebookId &&
                n.Blocks.Count == 1 &&
                n.Blocks[0].Type == BlockType.Paragraph),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateNote_ShouldDispatchNoteCreatedAndEditingBlockChanged()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        _embeddingService.GenerateEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<float>());

        // Act
        await _handler.Handle(new NoteCommands.CreateNote(notebookId), CancellationToken.None);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NoteActions.NoteCreated>(a => a.Note.NotebookId == notebookId));
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Any<NoteActions.EditingBlockChanged>());
    }

    [Fact]
    public async Task CreateNote_WhenRepositoryThrows_ShouldShowErrorNotification()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        var exception = new Exception("Database error");
        _noteRepository.Add(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Throws(exception);

        // Act
        await _handler.Handle(new NoteCommands.CreateNote(notebookId), CancellationToken.None);

        // Assert
        _logger.Received(1).LogError(exception, "Failed to create note in notebook '{NotebookId}'", notebookId);
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Error" &&
                n.Severity == NotificationSeverity.Error),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region UpdateNote Tests

    [Fact]
    public async Task UpdateNote_ShouldUpdateNoteAndDispatchAction()
    {
        // Arrange
        var note = CreateNote(Guid.NewGuid(), DateTimeOffset.UtcNow);
        _embeddingService.GenerateEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<float>());

        // Act
        await _handler.Handle(new NoteCommands.UpdateNote(note), CancellationToken.None);

        // Assert
        await _noteRepository.Received(1).Update(
            Arg.Is<Note>(n => n.Id == note.Id),
            Arg.Any<CancellationToken>());
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NoteActions.NoteUpdated>(a => a.Note.Id == note.Id));
    }

    [Fact]
    public async Task UpdateNote_ShouldUpdateLastModifiedAt()
    {
        // Arrange
        var oldTime = DateTimeOffset.UtcNow.AddDays(-1);
        var note = new Note
        {
            Id = Guid.NewGuid(),
            NotebookId = Guid.NewGuid(),
            CreatedAt = oldTime,
            LastModifiedAt = oldTime,
            Blocks = ImmutableList<Block>.Empty
        };
        _embeddingService.GenerateEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<float>());
        var beforeUpdate = DateTimeOffset.UtcNow;

        // Act
        await _handler.Handle(new NoteCommands.UpdateNote(note), CancellationToken.None);

        // Assert
        await _noteRepository.Received(1).Update(
            Arg.Is<Note>(n => n.LastModifiedAt >= beforeUpdate),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateNote_WhenRepositoryThrows_ShouldShowErrorNotification()
    {
        // Arrange
        var note = CreateNote(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var exception = new Exception("Database error");
        _noteRepository.Update(Arg.Any<Note>(), Arg.Any<CancellationToken>()).Throws(exception);

        // Act
        await _handler.Handle(new NoteCommands.UpdateNote(note), CancellationToken.None);

        // Assert
        _logger.Received(1).LogError(exception, "Failed to update note '{NoteId}'", note.Id);
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Error" &&
                n.Message == "Failed to save changes." &&
                n.Severity == NotificationSeverity.Error),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region SearchNotes Tests

    [Fact]
    public async Task SearchNotes_ShouldSearchAndDispatchResults()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        var query = "test query";
        var notes = new List<Note>
        {
            CreateNote(notebookId, DateTimeOffset.UtcNow),
            CreateNote(notebookId, DateTimeOffset.UtcNow.AddDays(-1))
        };
        _noteRepository.SearchInNotebook(notebookId, query, Arg.Any<CancellationToken>()).Returns(notes);

        // Act
        await _handler.Handle(new NoteCommands.SearchNotes(notebookId, query), CancellationToken.None);

        // Assert
        await _noteRepository.Received(1).SearchInNotebook(notebookId, query, Arg.Any<CancellationToken>());
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NoteActions.SearchResultsLoaded>(a =>
                a.Query == query &&
                a.FilteredNotes.Count == 2));
    }

    [Fact]
    public async Task SearchNotes_WhenRepositoryThrows_ShouldLogError()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        var exception = new Exception("Search error");
        _noteRepository.SearchInNotebook(notebookId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(exception);

        // Act
        await _handler.Handle(new NoteCommands.SearchNotes(notebookId, "test"), CancellationToken.None);

        // Assert
        _logger.Received(1).LogError(exception, "Failed to search notes in notebook '{NotebookId}'", notebookId);
    }

    #endregion

    #region ClearNotes Tests

    [Fact]
    public async Task ClearNotes_ShouldDispatchNotesCleared()
    {
        // Act
        await _handler.Handle(new NoteCommands.ClearNotes(), CancellationToken.None);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(Arg.Any<NoteActions.NotesCleared>());
        _logger.Received(1).LogDebug("Clearing notes from state");
    }

    #endregion

    #region ClearSearch Tests

    [Fact]
    public async Task ClearSearch_ShouldDispatchSearchCleared()
    {
        // Act
        await _handler.Handle(new NoteCommands.ClearSearch(), CancellationToken.None);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(Arg.Any<NoteActions.SearchCleared>());
        _logger.Received(1).LogDebug("Clearing search");
    }

    #endregion

    #region SetEditingBlock Tests

    [Fact]
    public async Task SetEditingBlock_ShouldDispatchEditingBlockChanged()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var blockId = Guid.NewGuid();

        // Act
        await _handler.Handle(new NoteCommands.SetEditingBlock(noteId, blockId), CancellationToken.None);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NoteActions.EditingBlockChanged>(a =>
                a.NoteId == noteId &&
                a.BlockId == blockId));
    }

    [Fact]
    public async Task SetEditingBlock_WithNullValues_ShouldDispatchNullValues()
    {
        // Act
        await _handler.Handle(new NoteCommands.SetEditingBlock(null, null), CancellationToken.None);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NoteActions.EditingBlockChanged>(a =>
                a.NoteId == null &&
                a.BlockId == null));
    }

    #endregion

    private static Note CreateNote(Guid notebookId, DateTimeOffset createdAt)
    {
        return new Note
        {
            Id = Guid.NewGuid(),
            NotebookId = notebookId,
            CreatedAt = createdAt,
            LastModifiedAt = createdAt,
            Blocks = ImmutableList.Create(new Block
            {
                Id = Guid.NewGuid(),
                Type = BlockType.Paragraph,
                Content = "Test content",
                Order = 0
            })
        };
    }
}
