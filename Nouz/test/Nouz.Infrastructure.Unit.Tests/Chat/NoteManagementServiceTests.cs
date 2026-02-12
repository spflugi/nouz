using System.Collections.Immutable;
using Mediator;
using Nouz.Application.Chat;
using Nouz.Application.Chat.Models;
using Nouz.Application.Embeddings;
using Nouz.Application.Logger;
using Nouz.Application.Notebooks;
using Nouz.Application.Notes;
using Nouz.Application.Notifications;
using Nouz.Application.Store;
using Nouz.Domain.Entities;
using Nouz.Domain.Repositories;
using Nouz.Infrastructure.Chat;
using NSubstitute;
using Shouldly;

namespace Nouz.Infrastructure.Unit.Tests.Chat;

public class NoteManagementServiceTests
{
    private readonly INotebookRepository _notebookRepository = Substitute.For<INotebookRepository>();
    private readonly INoteRepository _noteRepository = Substitute.For<INoteRepository>();
    private readonly INoteContextService _noteContextService = Substitute.For<INoteContextService>();
    private readonly IEmbeddingService _embeddingService = Substitute.For<IEmbeddingService>();
    private readonly IEmbeddingRepository _embeddingRepository = Substitute.For<IEmbeddingRepository>();
    private readonly IActionDispatcher _actionDispatcher = Substitute.For<IActionDispatcher>();
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILoggerAdapter<NoteManagementService> _logger = Substitute.For<ILoggerAdapter<NoteManagementService>>();
    private readonly NoteManagementService _service;

    public NoteManagementServiceTests()
    {
        _service = new NoteManagementService(
            _notebookRepository,
            _noteRepository,
            _noteContextService,
            _embeddingService,
            _embeddingRepository,
            _actionDispatcher,
            _mediator,
            _logger);
    }

    #region GetAllNotebooksAsync Tests

    [Fact]
    public async Task GetAllNotebooksAsync_ShouldReturnNotebooksFromRepository()
    {
        // Arrange
        var notebooks = new List<Notebook>
        {
            CreateNotebook("Work"),
            CreateNotebook("Personal")
        };
        _notebookRepository.GetAll(Arg.Any<CancellationToken>()).Returns(notebooks);

        // Act
        var result = await _service.GetAllNotebooksAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);
    }

    #endregion

    #region CreateNotebookAsync Tests

    [Fact]
    public async Task CreateNotebookAsync_ShouldCreateNotebookWithCorrectName()
    {
        // Act
        var result = await _service.CreateNotebookAsync("Test Notebook", TestContext.Current.CancellationToken);

        // Assert
        result.Name.ShouldBe("Test Notebook");
        result.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateNotebookAsync_ShouldTrimNotebookName()
    {
        // Act
        var result = await _service.CreateNotebookAsync("  Test Notebook  ", TestContext.Current.CancellationToken);

        // Assert
        result.Name.ShouldBe("Test Notebook");
    }

    [Fact]
    public async Task CreateNotebookAsync_ShouldPersistToRepository()
    {
        // Act
        await _service.CreateNotebookAsync("Test Notebook", TestContext.Current.CancellationToken);

        // Assert
        await _notebookRepository.Received(1).Add(Arg.Is<Notebook>(n => n.Name == "Test Notebook"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateNotebookAsync_ShouldDispatchNotebookAddedAction()
    {
        // Act
        await _service.CreateNotebookAsync("Test Notebook", TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(Arg.Is<NotebookActions.NotebookAdded>(a => a.Notebook.Name == "Test Notebook"));
    }

    [Fact]
    public async Task CreateNotebookAsync_ShouldShowSuccessNotification()
    {
        // Act
        await _service.CreateNotebookAsync("Test Notebook", TestContext.Current.CancellationToken);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Severity == NotificationSeverity.Success &&
                n.Message.Contains("Test Notebook")),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetNotebookNotesAsync Tests

    [Fact]
    public async Task GetNotebookNotesAsync_ShouldReturnNotesFromRepository()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        var notes = new List<Note>
        {
            CreateNote(notebookId),
            CreateNote(notebookId)
        };
        _noteRepository.GetAllByNotebook(notebookId, Arg.Any<CancellationToken>()).Returns(notes);

        // Act
        var result = await _service.GetNotebookNotesAsync(notebookId, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);
    }

    #endregion

    #region CreateNoteAsync Tests

    [Fact]
    public async Task CreateNoteAsync_ShouldCreateNoteWithBlocks()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        var blocks = new List<BlockDefinition>
        {
            new() { Type = "h1", Content = "Title" },
            new() { Type = "paragraph", Content = "Content" }
        };

        // Act
        var result = await _service.CreateNoteAsync(notebookId, blocks, TestContext.Current.CancellationToken);

        // Assert
        result.NotebookId.ShouldBe(notebookId);
        result.Blocks.Count.ShouldBe(2);
        result.Blocks[0].Type.ShouldBe(BlockType.H1);
        result.Blocks[0].Content.ShouldBe("Title");
        result.Blocks[1].Type.ShouldBe(BlockType.Paragraph);
        result.Blocks[1].Content.ShouldBe("Content");
    }

    [Fact]
    public async Task CreateNoteAsync_ShouldCreateDefaultBlockWhenNoBlocksProvided()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        var blocks = new List<BlockDefinition>();

        // Act
        var result = await _service.CreateNoteAsync(notebookId, blocks, TestContext.Current.CancellationToken);

        // Assert
        result.Blocks.Count.ShouldBe(1);
        result.Blocks[0].Type.ShouldBe(BlockType.Paragraph);
    }

    [Fact]
    public async Task CreateNoteAsync_ShouldPersistToRepository()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        var blocks = new List<BlockDefinition>
        {
            new() { Type = "paragraph", Content = "Test" }
        };

        // Act
        await _service.CreateNoteAsync(notebookId, blocks, TestContext.Current.CancellationToken);

        // Assert
        await _noteRepository.Received(1).Add(Arg.Is<Note>(n => n.NotebookId == notebookId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateNoteAsync_ShouldDispatchNoteCreatedAction()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        var blocks = new List<BlockDefinition>
        {
            new() { Type = "paragraph", Content = "Test" }
        };

        // Act
        await _service.CreateNoteAsync(notebookId, blocks, TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(Arg.Is<NoteActions.NoteCreated>(a => a.Note.NotebookId == notebookId));
    }

    [Fact]
    public async Task CreateNoteAsync_ShouldShowSuccessNotification()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        var blocks = new List<BlockDefinition>
        {
            new() { Type = "paragraph", Content = "Test" }
        };

        // Act
        await _service.CreateNoteAsync(notebookId, blocks, TestContext.Current.CancellationToken);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n => n.Severity == NotificationSeverity.Success),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("h1", BlockType.H1)]
    [InlineData("h2", BlockType.H2)]
    [InlineData("h3", BlockType.H3)]
    [InlineData("h4", BlockType.H4)]
    [InlineData("paragraph", BlockType.Paragraph)]
    [InlineData("listitem", BlockType.ListItem)]
    [InlineData("todoitem", BlockType.TodoItem)]
    [InlineData("code", BlockType.Code)]
    [InlineData("quote", BlockType.Quote)]
    [InlineData("decision", BlockType.Decision)]
    [InlineData("warning", BlockType.Warning)]
    [InlineData("agendaitem", BlockType.AgendaItem)]
    public async Task CreateNoteAsync_ShouldParseBlockTypesCorrectly(string inputType, BlockType expectedType)
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        var blocks = new List<BlockDefinition>
        {
            new() { Type = inputType, Content = "Test" }
        };

        // Act
        var result = await _service.CreateNoteAsync(notebookId, blocks, TestContext.Current.CancellationToken);

        // Assert
        result.Blocks[0].Type.ShouldBe(expectedType);
    }

    [Fact]
    public async Task CreateNoteAsync_ShouldDefaultToPlainTextForUnknownBlockType()
    {
        // Arrange
        var notebookId = Guid.NewGuid();
        var blocks = new List<BlockDefinition>
        {
            new() { Type = "unknown", Content = "Test" }
        };

        // Act
        var result = await _service.CreateNoteAsync(notebookId, blocks, TestContext.Current.CancellationToken);

        // Assert
        result.Blocks[0].Type.ShouldBe(BlockType.Paragraph);
    }

    #endregion

    #region GetNoteAsync Tests

    [Fact]
    public async Task GetNoteAsync_ShouldReturnNoteFromRepository()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var note = CreateNote(Guid.NewGuid(), noteId);
        _noteRepository.GetById(noteId, Arg.Any<CancellationToken>()).Returns(note);

        // Act
        var result = await _service.GetNoteAsync(noteId, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(noteId);
    }

    [Fact]
    public async Task GetNoteAsync_ShouldReturnNullWhenNoteNotFound()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        _noteRepository.GetById(noteId, Arg.Any<CancellationToken>()).Returns((Note?)null);

        // Act
        var result = await _service.GetNoteAsync(noteId, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region EditNoteAsync Tests

    [Fact]
    public async Task EditNoteAsync_ShouldUpdateNoteWithNewBlocks()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var existingNote = CreateNote(Guid.NewGuid(), noteId);
        var newBlocks = new List<BlockDefinition>
        {
            new() { Type = "h1", Content = "Updated Title" },
            new() { Type = "paragraph", Content = "Updated Content" }
        };
        _noteRepository.GetById(noteId, Arg.Any<CancellationToken>()).Returns(existingNote);

        // Act
        var result = await _service.EditNoteAsync(noteId, newBlocks, TestContext.Current.CancellationToken);

        // Assert
        result.Blocks.Count.ShouldBe(2);
        result.Blocks[0].Content.ShouldBe("Updated Title");
        result.Blocks[1].Content.ShouldBe("Updated Content");
    }

    [Fact]
    public async Task EditNoteAsync_ShouldThrowWhenNoteNotFound()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var newBlocks = new List<BlockDefinition>
        {
            new() { Type = "paragraph", Content = "Test" }
        };
        _noteRepository.GetById(noteId, Arg.Any<CancellationToken>()).Returns((Note?)null);

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _service.EditNoteAsync(noteId, newBlocks, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task EditNoteAsync_ShouldPersistToRepository()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var existingNote = CreateNote(Guid.NewGuid(), noteId);
        var newBlocks = new List<BlockDefinition>
        {
            new() { Type = "paragraph", Content = "Updated" }
        };
        _noteRepository.GetById(noteId, Arg.Any<CancellationToken>()).Returns(existingNote);

        // Act
        await _service.EditNoteAsync(noteId, newBlocks, TestContext.Current.CancellationToken);

        // Assert
        await _noteRepository.Received(1).Update(Arg.Is<Note>(n => n.Id == noteId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EditNoteAsync_ShouldDispatchNoteUpdatedAction()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var existingNote = CreateNote(Guid.NewGuid(), noteId);
        var newBlocks = new List<BlockDefinition>
        {
            new() { Type = "paragraph", Content = "Updated" }
        };
        _noteRepository.GetById(noteId, Arg.Any<CancellationToken>()).Returns(existingNote);

        // Act
        await _service.EditNoteAsync(noteId, newBlocks, TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(Arg.Is<NoteActions.NoteUpdated>(a => a.Note.Id == noteId));
    }

    [Fact]
    public async Task EditNoteAsync_ShouldShowSuccessNotification()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var existingNote = CreateNote(Guid.NewGuid(), noteId);
        var newBlocks = new List<BlockDefinition>
        {
            new() { Type = "paragraph", Content = "Updated" }
        };
        _noteRepository.GetById(noteId, Arg.Any<CancellationToken>()).Returns(existingNote);

        // Act
        await _service.EditNoteAsync(noteId, newBlocks, TestContext.Current.CancellationToken);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n => n.Severity == NotificationSeverity.Success),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeleteNoteAsync Tests

    [Fact]
    public async Task DeleteNoteAsync_ShouldDeleteFromRepository()
    {
        // Arrange
        var noteId = Guid.NewGuid();

        // Act
        await _service.DeleteNoteAsync(noteId, TestContext.Current.CancellationToken);

        // Assert
        await _noteRepository.Received(1).Delete(noteId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteNoteAsync_ShouldDispatchNoteDeletedAction()
    {
        // Arrange
        var noteId = Guid.NewGuid();

        // Act
        await _service.DeleteNoteAsync(noteId, TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(Arg.Is<NoteActions.NoteDeleted>(a => a.NoteId == noteId));
    }

    [Fact]
    public async Task DeleteNoteAsync_ShouldShowSuccessNotification()
    {
        // Arrange
        var noteId = Guid.NewGuid();

        // Act
        await _service.DeleteNoteAsync(noteId, TestContext.Current.CancellationToken);

        // Assert
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n => n.Severity == NotificationSeverity.Success),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region SearchNotesAsync Tests

    [Fact]
    public async Task SearchNotesAsync_ShouldUseNoteContextService()
    {
        // Arrange
        var query = "test query";
        var notes = new List<Note> { CreateNote(Guid.NewGuid()) };
        _noteContextService.GetRelevantNotesAsync(query, 10, Arg.Any<CancellationToken>()).Returns(notes);

        // Act
        var result = await _service.SearchNotesAsync(query, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(1);
        await _noteContextService.Received(1).GetRelevantNotesAsync(query, 10, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Helper Methods

    private static Notebook CreateNotebook(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        CreatedAt = DateTimeOffset.UtcNow,
        LastModifiedAt = DateTimeOffset.UtcNow
    };

    private static Note CreateNote(Guid notebookId, Guid? noteId = null) => new()
    {
        Id = noteId ?? Guid.NewGuid(),
        NotebookId = notebookId,
        CreatedAt = DateTimeOffset.UtcNow,
        LastModifiedAt = DateTimeOffset.UtcNow,
        Blocks = ImmutableList<Block>.Empty
    };

    #endregion
}
