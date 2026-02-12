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

public class NoteHandlerDeleteNoteTests
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

    public NoteHandlerDeleteNoteTests()
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

    [Fact]
    public async Task DeleteNote_WithoutImages_ShouldDeleteNoteAndDispatchAction()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var note = CreateNoteWithBlocks(noteId, BlockType.Paragraph);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.DeleteNote(noteId), TestContext.Current.CancellationToken);

        // Assert
        await _noteRepository.Received(1).Delete(noteId, Arg.Any<CancellationToken>());
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NoteActions.NoteDeleted>(a => a.NoteId == noteId));
        await _attachmentRepository.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteNote_WithOneImage_ShouldDeleteAttachmentAndNote()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var attachmentId = Guid.NewGuid();
        var note = CreateNoteWithImageBlock(noteId, attachmentId);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.DeleteNote(noteId), TestContext.Current.CancellationToken);

        // Assert
        await _attachmentRepository.Received(1).DeleteAsync(attachmentId, Arg.Any<CancellationToken>());
        await _noteRepository.Received(1).Delete(noteId, Arg.Any<CancellationToken>());
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NoteActions.NoteDeleted>(a => a.NoteId == noteId));
    }

    [Fact]
    public async Task DeleteNote_WithMultipleImages_ShouldDeleteAllAttachments()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var attachmentId1 = Guid.NewGuid();
        var attachmentId2 = Guid.NewGuid();
        var attachmentId3 = Guid.NewGuid();
        var note = CreateNoteWithMultipleImageBlocks(noteId, attachmentId1, attachmentId2, attachmentId3);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.DeleteNote(noteId), TestContext.Current.CancellationToken);

        // Assert
        await _attachmentRepository.Received(1).DeleteAsync(attachmentId1, Arg.Any<CancellationToken>());
        await _attachmentRepository.Received(1).DeleteAsync(attachmentId2, Arg.Any<CancellationToken>());
        await _attachmentRepository.Received(1).DeleteAsync(attachmentId3, Arg.Any<CancellationToken>());
        await _noteRepository.Received(1).Delete(noteId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteNote_WithMixedBlocks_ShouldOnlyDeleteImageAttachments()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var attachmentId = Guid.NewGuid();
        var note = CreateNoteWithMixedBlocks(noteId, attachmentId);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.DeleteNote(noteId), TestContext.Current.CancellationToken);

        // Assert
        await _attachmentRepository.Received(1).DeleteAsync(attachmentId, Arg.Any<CancellationToken>());
        await _noteRepository.Received(1).Delete(noteId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteNote_WhenAttachmentDeletionFails_ShouldStillDeleteNote()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var attachmentId = Guid.NewGuid();
        var note = CreateNoteWithImageBlock(noteId, attachmentId);
        SetupStateWithNote(note);
        _attachmentRepository.DeleteAsync(attachmentId, Arg.Any<CancellationToken>())
            .Throws(new Exception("Storage error"));

        // Act
        await _handler.Handle(new NoteCommands.DeleteNote(noteId), TestContext.Current.CancellationToken);

        // Assert
        await _noteRepository.Received(1).Delete(noteId, Arg.Any<CancellationToken>());
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NoteActions.NoteDeleted>(a => a.NoteId == noteId));
    }

    [Fact]
    public async Task DeleteNote_WhenNoteNotInState_ShouldStillDeleteFromRepository()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        SetupEmptyState();

        // Act
        await _handler.Handle(new NoteCommands.DeleteNote(noteId), TestContext.Current.CancellationToken);

        // Assert
        await _noteRepository.Received(1).Delete(noteId, Arg.Any<CancellationToken>());
        await _attachmentRepository.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteNote_ShouldShowSuccessNotification()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var note = CreateNoteWithBlocks(noteId, BlockType.Paragraph);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.DeleteNote(noteId), TestContext.Current.CancellationToken);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Deleted" &&
                n.Message == "Note deleted." &&
                n.Severity == NotificationSeverity.Success),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteNote_WhenRepositoryThrows_ShouldShowErrorNotification()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var note = CreateNoteWithBlocks(noteId, BlockType.Paragraph);
        SetupStateWithNote(note);
        _noteRepository.Delete(noteId, Arg.Any<CancellationToken>()).Throws(new Exception("Database error"));

        // Act
        await _handler.Handle(new NoteCommands.DeleteNote(noteId), TestContext.Current.CancellationToken);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Error" &&
                n.Message == "Failed to delete note." &&
                n.Severity == NotificationSeverity.Error),
            Arg.Any<CancellationToken>());
        await _actionDispatcher.DidNotReceive().Dispatch(Arg.Any<NoteActions.NoteDeleted>());
    }

    private void SetupStateWithNote(Note note)
    {
        var state = new RootState
        {
            Notes = new NoteState { Notes = [note] }
        };
        _stateProvider.State.Returns(state);
    }

    private void SetupEmptyState()
    {
        var state = new RootState
        {
            Notes = new NoteState { Notes = [] }
        };
        _stateProvider.State.Returns(state);
    }

    private static Note CreateNoteWithBlocks(Guid noteId, BlockType blockType)
    {
        return new Note
        {
            Id = noteId,
            NotebookId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            LastModifiedAt = DateTimeOffset.UtcNow,
            Blocks =
            [
                new Block
                {
                    Id = Guid.NewGuid(),
                    Type = blockType,
                    Content = "Test content",
                    Order = 0
                }
            ]
        };
    }

    private static Note CreateNoteWithImageBlock(Guid noteId, Guid attachmentId)
    {
        return new Note
        {
            Id = noteId,
            NotebookId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            LastModifiedAt = DateTimeOffset.UtcNow,
            Blocks =
            [
                new Block
                {
                    Id = Guid.NewGuid(),
                    Type = BlockType.Image,
                    Content = string.Empty,
                    Order = 0,
                    Metadata = new Dictionary<string, object>
                    {
                        ["attachmentId"] = attachmentId.ToString(),
                        ["fileName"] = "test.png",
                        ["mimeType"] = "image/png"
                    }
                }
            ]
        };
    }

    private static Note CreateNoteWithMultipleImageBlocks(Guid noteId, Guid attachmentId1, Guid attachmentId2, Guid attachmentId3)
    {
        return new Note
        {
            Id = noteId,
            NotebookId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            LastModifiedAt = DateTimeOffset.UtcNow,
            Blocks =
            [
                new Block
                {
                    Id = Guid.NewGuid(),
                    Type = BlockType.Image,
                    Content = string.Empty,
                    Order = 0,
                    Metadata = new Dictionary<string, object>
                    {
                        ["attachmentId"] = attachmentId1.ToString(),
                        ["fileName"] = "test1.png",
                        ["mimeType"] = "image/png"
                    }
                },
                new Block
                {
                    Id = Guid.NewGuid(),
                    Type = BlockType.Image,
                    Content = string.Empty,
                    Order = 1,
                    Metadata = new Dictionary<string, object>
                    {
                        ["attachmentId"] = attachmentId2.ToString(),
                        ["fileName"] = "test2.png",
                        ["mimeType"] = "image/png"
                    }
                },
                new Block
                {
                    Id = Guid.NewGuid(),
                    Type = BlockType.Image,
                    Content = string.Empty,
                    Order = 2,
                    Metadata = new Dictionary<string, object>
                    {
                        ["attachmentId"] = attachmentId3.ToString(),
                        ["fileName"] = "test3.png",
                        ["mimeType"] = "image/png"
                    }
                }
            ]
        };
    }

    private static Note CreateNoteWithMixedBlocks(Guid noteId, Guid attachmentId)
    {
        return new Note
        {
            Id = noteId,
            NotebookId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            LastModifiedAt = DateTimeOffset.UtcNow,
            Blocks =
            [
                new Block
                {
                    Id = Guid.NewGuid(),
                    Type = BlockType.Paragraph,
                    Content = "Some text",
                    Order = 0
                },
                new Block
                {
                    Id = Guid.NewGuid(),
                    Type = BlockType.Image,
                    Content = string.Empty,
                    Order = 1,
                    Metadata = new Dictionary<string, object>
                    {
                        ["attachmentId"] = attachmentId.ToString(),
                        ["fileName"] = "test.png",
                        ["mimeType"] = "image/png"
                    }
                },
                new Block
                {
                    Id = Guid.NewGuid(),
                    Type = BlockType.H1,
                    Content = "Heading",
                    Order = 2
                }
            ]
        };
    }
}
