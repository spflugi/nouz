using System.Collections.Immutable;
using Mediator;
using Nouz.Application.Embeddings;
using Nouz.Application.Logger;
using Nouz.Application.Notes;
using Nouz.Application.Store;
using Nouz.Domain.Entities;
using Nouz.Domain.Repositories;
using NSubstitute;
using Shouldly;

namespace Nouz.Application.Unit.Tests.Notes;

public class NoteHandlerBlockTests
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

    public NoteHandlerBlockTests()
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

    #region AddBlock Tests

    [Fact]
    public async Task AddBlock_ShouldAddBlockAndDispatchNoteUpdated()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var existingBlock = CreateBlock(BlockType.Paragraph, "Existing");
        var note = CreateNoteWithBlocks(noteId, existingBlock);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.AddBlock(noteId, existingBlock.Id, BlockType.Paragraph), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NoteActions.NoteUpdated>(a =>
                a.Note.Blocks.Count == 2));
    }

    [Fact]
    public async Task AddBlock_WhenAfterBlockIdIsNull_ShouldAddAtBeginning()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var existingBlock = CreateBlock(BlockType.Paragraph, "Existing");
        var note = CreateNoteWithBlocks(noteId, existingBlock);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.AddBlock(noteId, null, BlockType.H1), TestContext.Current.CancellationToken);

        // Assert
        capturedAction.ShouldNotBeNull();
        capturedAction.Note.Blocks[0].Type.ShouldBe(BlockType.H1);
    }

    [Fact]
    public async Task AddBlock_ShouldDispatchEditingBlockChanged()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var existingBlock = CreateBlock(BlockType.Paragraph, "Content");
        var note = CreateNoteWithBlocks(noteId, existingBlock);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.AddBlock(noteId, existingBlock.Id, BlockType.Paragraph), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(Arg.Any<NoteActions.EditingBlockChanged>());
    }

    [Fact]
    public async Task AddBlock_WhenNoteNotFound_ShouldLogWarningAndReturn()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        SetupEmptyState();

        // Act
        await _handler.Handle(new NoteCommands.AddBlock(noteId, null, BlockType.Paragraph), TestContext.Current.CancellationToken);

        // Assert
        _logger.Received(1).LogWarning("Note '{NoteId}' not found in state", noteId);
        await _actionDispatcher.DidNotReceive().Dispatch(Arg.Any<NoteActions.NoteUpdated>());
    }

    [Fact]
    public async Task AddBlock_ShouldAutoSaveToRepository()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var existingBlock = CreateBlock(BlockType.Paragraph, "Content");
        var note = CreateNoteWithBlocks(noteId, existingBlock);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.AddBlock(noteId, existingBlock.Id, BlockType.Paragraph), TestContext.Current.CancellationToken);

        // Assert
        await _noteRepository.Received(1).Update(Arg.Any<Note>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddBlock_WithMetadata_ShouldIncludeMetadataInNewBlock()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var existingBlock = CreateBlock(BlockType.Paragraph, "Content");
        var note = CreateNoteWithBlocks(noteId, existingBlock);
        SetupStateWithNote(note);

        var metadata = new Dictionary<string, object> { ["checked"] = false };

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.AddBlock(noteId, existingBlock.Id, BlockType.TodoItem, metadata), TestContext.Current.CancellationToken);

        // Assert
        capturedAction.ShouldNotBeNull();
        var newBlock = capturedAction.Note.Blocks.Last();
        newBlock.Type.ShouldBe(BlockType.TodoItem);
        newBlock.Metadata.ShouldContainKey("checked");
    }

    #endregion

    #region UpdateBlock Tests

    [Fact]
    public async Task UpdateBlock_ShouldUpdateAndDispatch()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block = CreateBlock(BlockType.Paragraph, "Original");
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        var updatedBlock = block with { Content = "Updated" };

        // Act
        await _handler.Handle(new NoteCommands.UpdateBlock(noteId, updatedBlock), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NoteActions.NoteUpdated>(a =>
                a.Note.Blocks.Any(b => b.Content == "Updated")));
    }

    [Fact]
    public async Task UpdateBlock_WhenNoteNotFound_ShouldLogWarning()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        SetupEmptyState();

        var block = CreateBlock(BlockType.Paragraph, "Content");

        // Act
        await _handler.Handle(new NoteCommands.UpdateBlock(noteId, block), TestContext.Current.CancellationToken);

        // Assert
        _logger.Received(1).LogWarning("Note '{NoteId}' not found in state", noteId);
    }

    [Fact]
    public async Task UpdateBlock_WhenBlockNotFound_ShouldLogWarning()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var existingBlock = CreateBlock(BlockType.Paragraph, "Existing");
        var note = CreateNoteWithBlocks(noteId, existingBlock);
        SetupStateWithNote(note);

        var nonExistingBlock = CreateBlock(BlockType.Paragraph, "Non-existing");

        // Act
        await _handler.Handle(new NoteCommands.UpdateBlock(noteId, nonExistingBlock), TestContext.Current.CancellationToken);

        // Assert
        _logger.Received(1).LogWarning("Block '{BlockId}' not found in note '{NoteId}'", nonExistingBlock.Id, noteId);
    }

    [Fact]
    public async Task UpdateBlock_WhenTodoCheckboxToggled_ShouldAutoSave()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.TodoItem,
            Content = "Task",
            Metadata = new Dictionary<string, object> { ["checked"] = false }
        };
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        var updatedBlock = block with
        {
            Metadata = new Dictionary<string, object> { ["checked"] = true }
        };

        _embeddingService.GenerateEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<float>());

        // Act
        await _handler.Handle(new NoteCommands.UpdateBlock(noteId, updatedBlock), TestContext.Current.CancellationToken);

        // Assert
        await _noteRepository.Received(1).Update(Arg.Any<Note>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region DeleteBlock Tests

    [Fact]
    public async Task DeleteBlock_ShouldRemoveBlockAndDispatch()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block1 = CreateBlock(BlockType.Paragraph, "First");
        var block2 = CreateBlock(BlockType.Paragraph, "Second");
        var note = CreateNoteWithBlocks(noteId, block1, block2);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.DeleteBlock(noteId, block1.Id), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NoteActions.NoteUpdated>(a =>
                a.Note.Blocks.Count == 1 &&
                a.Note.Blocks[0].Id == block2.Id));
    }

    [Fact]
    public async Task DeleteBlock_ShouldNotDeleteLastBlock()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block = CreateBlock(BlockType.Paragraph, "Only block");
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.DeleteBlock(noteId, block.Id), TestContext.Current.CancellationToken);

        // Assert
        _logger.Received(1).LogDebug("Cannot delete the last block in note '{NoteId}'", noteId);
        await _actionDispatcher.DidNotReceive().Dispatch(Arg.Any<NoteActions.NoteUpdated>());
    }

    [Fact]
    public async Task DeleteBlock_ShouldFocusPreviousBlock()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block1 = CreateBlock(BlockType.Paragraph, "First");
        var block2 = CreateBlock(BlockType.Paragraph, "Second");
        var note = CreateNoteWithBlocks(noteId, block1, block2);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.DeleteBlock(noteId, block2.Id), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NoteActions.EditingBlockChanged>(a => a.BlockId == block1.Id));
    }

    [Fact]
    public async Task DeleteBlock_WhenDeletingImageBlock_ShouldDeleteAttachment()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var attachmentId = Guid.NewGuid();
        var imageBlock = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.Image,
            Content = "",
            Metadata = new Dictionary<string, object> { ["attachmentId"] = attachmentId.ToString() }
        };
        var otherBlock = CreateBlock(BlockType.Paragraph, "Other");
        var note = CreateNoteWithBlocks(noteId, imageBlock, otherBlock);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.DeleteBlock(noteId, imageBlock.Id), TestContext.Current.CancellationToken);

        // Assert
        await _attachmentRepository.Received(1).DeleteAsync(attachmentId, Arg.Any<CancellationToken>());
    }

    #endregion

    #region ChangeBlockType Tests

    [Fact]
    public async Task ChangeBlockType_ShouldChangeTypeAndDispatch()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block = CreateBlock(BlockType.Paragraph, "Content");
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.ChangeBlockType(noteId, block.Id, BlockType.H1), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NoteActions.NoteUpdated>(a =>
                a.Note.Blocks.Any(b => b.Type == BlockType.H1)));
    }

    [Fact]
    public async Task ChangeBlockType_WhenChangingToDivider_ShouldAddParagraphAfter()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block = CreateBlock(BlockType.Paragraph, "Content");
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.ChangeBlockType(noteId, block.Id, BlockType.Divider), TestContext.Current.CancellationToken);

        // Assert
        capturedAction.ShouldNotBeNull();
        capturedAction.Note.Blocks.Count.ShouldBe(2);
        capturedAction.Note.Blocks[0].Type.ShouldBe(BlockType.Divider);
        capturedAction.Note.Blocks[1].Type.ShouldBe(BlockType.Paragraph);
    }

    [Fact]
    public async Task ChangeBlockType_WhenChangingToDivider_ShouldFocusNewParagraph()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block = CreateBlock(BlockType.Paragraph, "Content");
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.ChangeBlockType(noteId, block.Id, BlockType.Divider), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NoteActions.EditingBlockChanged>(a => a.BlockId != block.Id));
    }

    [Fact]
    public async Task ChangeBlockType_ToIdea_SetsDefaultRawStatus()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block = CreateBlock(BlockType.Paragraph, "Content");
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.ChangeBlockType(noteId, block.Id, BlockType.Idea), TestContext.Current.CancellationToken);

        // Assert
        capturedAction.ShouldNotBeNull();
        var ideaBlock = capturedAction.Note.Blocks.First(b => b.Type == BlockType.Idea);
        ideaBlock.Metadata.ShouldContainKey("status");
        ideaBlock.Metadata["status"].ShouldBe("raw");
    }

    [Fact]
    public async Task ChangeBlockType_ToIdea_ShouldAutoSaveToRepository()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block = CreateBlock(BlockType.Paragraph, "Content");
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.ChangeBlockType(noteId, block.Id, BlockType.Idea), TestContext.Current.CancellationToken);

        // Assert
        await _noteRepository.Received(1).Update(Arg.Any<Note>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateBlock_IdeaStatusChanged_AutoSaves()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.Idea,
            Content = "My idea",
            Metadata = new Dictionary<string, object> { ["status"] = "raw" }
        };
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        var updatedBlock = block with
        {
            Metadata = new Dictionary<string, object> { ["status"] = "exploring" }
        };

        // Act
        await _handler.Handle(new NoteCommands.UpdateBlock(noteId, updatedBlock), TestContext.Current.CancellationToken);

        // Assert
        await _noteRepository.Received(1).Update(Arg.Any<Note>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateBlock_IdeaStatusUnchanged_ShouldNotAutoSave()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.Idea,
            Content = "My idea",
            Metadata = new Dictionary<string, object> { ["status"] = "exploring" }
        };
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        // Update content only, status stays the same
        var updatedBlock = block with { Content = "Updated idea text" };

        // Act
        await _handler.Handle(new NoteCommands.UpdateBlock(noteId, updatedBlock), TestContext.Current.CancellationToken);

        // Assert
        await _noteRepository.DidNotReceive().Update(Arg.Any<Note>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region ReorderBlocks Tests

    [Fact]
    public async Task ReorderBlocks_ShouldMoveBlockToNewPosition()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block1 = CreateBlock(BlockType.Paragraph, "First", 0);
        var block2 = CreateBlock(BlockType.Paragraph, "Second", 1);
        var block3 = CreateBlock(BlockType.Paragraph, "Third", 2);
        var note = CreateNoteWithBlocks(noteId, block1, block2, block3);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act - Move first block to end
        await _handler.Handle(new NoteCommands.ReorderBlocks(noteId, block1.Id, 2), TestContext.Current.CancellationToken);

        // Assert
        capturedAction.ShouldNotBeNull();
        capturedAction.Note.Blocks[0].Id.ShouldBe(block2.Id);
        capturedAction.Note.Blocks[1].Id.ShouldBe(block3.Id);
        capturedAction.Note.Blocks[2].Id.ShouldBe(block1.Id);
    }

    [Fact]
    public async Task ReorderBlocks_WhenSamePosition_ShouldNotDispatch()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block1 = CreateBlock(BlockType.Paragraph, "First", 0);
        var block2 = CreateBlock(BlockType.Paragraph, "Second", 1);
        var note = CreateNoteWithBlocks(noteId, block1, block2);
        SetupStateWithNote(note);

        // Act - Move to same position
        await _handler.Handle(new NoteCommands.ReorderBlocks(noteId, block1.Id, 0), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.DidNotReceive().Dispatch(Arg.Any<NoteActions.NoteUpdated>());
    }

    [Fact]
    public async Task ReorderBlocks_ShouldClampIndexToValidRange()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block1 = CreateBlock(BlockType.Paragraph, "First", 0);
        var block2 = CreateBlock(BlockType.Paragraph, "Second", 1);
        var note = CreateNoteWithBlocks(noteId, block1, block2);
        SetupStateWithNote(note);

        // Act - Try to move to index beyond bounds
        await _handler.Handle(new NoteCommands.ReorderBlocks(noteId, block1.Id, 100), TestContext.Current.CancellationToken);

        // Assert - Should clamp to valid index
        await _actionDispatcher.Received(1).Dispatch(Arg.Any<NoteActions.NoteUpdated>());
    }

    #endregion

    private static Block CreateBlock(BlockType type, string content, int order = 0)
    {
        return new Block
        {
            Id = Guid.NewGuid(),
            Type = type,
            Content = content,
            Order = order
        };
    }

    private static Note CreateNoteWithBlocks(Guid noteId, params Block[] blocks)
    {
        return new Note
        {
            Id = noteId,
            NotebookId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            LastModifiedAt = DateTimeOffset.UtcNow,
            Blocks = blocks.ToImmutableList()
        };
    }

    private void SetupStateWithNote(Note note)
    {
        var state = new RootState
        {
            Notes = new NoteState { Notes = ImmutableList.Create(note) }
        };
        _stateProvider.State.Returns(state);
    }

    private void SetupEmptyState()
    {
        var state = new RootState
        {
            Notes = new NoteState { Notes = ImmutableList<Note>.Empty }
        };
        _stateProvider.State.Returns(state);
    }
}
