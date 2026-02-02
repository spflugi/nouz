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

public class NoteHandlerImageTests
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

    public NoteHandlerImageTests()
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

    #region UpdateImageCaption Tests

    [Fact]
    public async Task UpdateImageCaption_ShouldUpdateCaptionInMetadata()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var imageBlock = CreateImageBlock(blockId);
        var note = CreateNoteWithBlocks(noteId, imageBlock);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.UpdateImageCaption(noteId, blockId, "New caption"), CancellationToken.None);

        // Assert
        capturedAction.ShouldNotBeNull();
        var updatedBlock = capturedAction.Note.Blocks.First(b => b.Id == blockId);
        updatedBlock.Metadata["caption"].ShouldBe("New caption");
    }

    [Fact]
    public async Task UpdateImageCaption_ShouldDispatchNoteUpdated()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var imageBlock = CreateImageBlock(blockId);
        var note = CreateNoteWithBlocks(noteId, imageBlock);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.UpdateImageCaption(noteId, blockId, "Caption"), CancellationToken.None);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(Arg.Any<NoteActions.NoteUpdated>());
    }

    [Fact]
    public async Task UpdateImageCaption_WhenNoteNotFound_ShouldLogWarningAndReturn()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        SetupEmptyState();

        // Act
        await _handler.Handle(new NoteCommands.UpdateImageCaption(noteId, blockId, "Caption"), CancellationToken.None);

        // Assert
        _logger.Received(1).LogWarning("Note '{NoteId}' not found in state", noteId);
        await _actionDispatcher.DidNotReceive().Dispatch(Arg.Any<NoteActions.NoteUpdated>());
    }

    [Fact]
    public async Task UpdateImageCaption_WhenBlockNotFound_ShouldLogWarningAndReturn()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var differentBlock = CreateImageBlock(Guid.NewGuid());
        var note = CreateNoteWithBlocks(noteId, differentBlock);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.UpdateImageCaption(noteId, blockId, "Caption"), CancellationToken.None);

        // Assert
        _logger.Received(1).LogWarning("Image block '{BlockId}' not found in note '{NoteId}'", blockId, noteId);
        await _actionDispatcher.DidNotReceive().Dispatch(Arg.Any<NoteActions.NoteUpdated>());
    }

    [Fact]
    public async Task UpdateImageCaption_WhenBlockIsNotImage_ShouldLogWarningAndReturn()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var paragraphBlock = new Block
        {
            Id = blockId,
            Type = BlockType.Paragraph,
            Content = "Text",
            Order = 0
        };
        var note = CreateNoteWithBlocks(noteId, paragraphBlock);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.UpdateImageCaption(noteId, blockId, "Caption"), CancellationToken.None);

        // Assert
        _logger.Received(1).LogWarning("Image block '{BlockId}' not found in note '{NoteId}'", blockId, noteId);
        await _actionDispatcher.DidNotReceive().Dispatch(Arg.Any<NoteActions.NoteUpdated>());
    }

    [Fact]
    public async Task UpdateImageCaption_ShouldPreserveOtherMetadata()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var imageBlock = new Block
        {
            Id = blockId,
            Type = BlockType.Image,
            Content = "",
            Order = 0,
            Metadata = new Dictionary<string, object>
            {
                ["attachmentId"] = "some-id",
                ["widthPercent"] = 50
            }
        };
        var note = CreateNoteWithBlocks(noteId, imageBlock);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.UpdateImageCaption(noteId, blockId, "New caption"), CancellationToken.None);

        // Assert
        capturedAction.ShouldNotBeNull();
        var updatedBlock = capturedAction.Note.Blocks.First(b => b.Id == blockId);
        updatedBlock.Metadata["attachmentId"].ShouldBe("some-id");
        updatedBlock.Metadata["widthPercent"].ShouldBe(50);
        updatedBlock.Metadata["caption"].ShouldBe("New caption");
    }

    [Fact]
    public async Task UpdateImageCaption_ShouldUpdateLastModifiedAt()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var imageBlock = CreateImageBlock(blockId);
        var oldTime = DateTimeOffset.UtcNow.AddDays(-1);
        var note = new Note
        {
            Id = noteId,
            NotebookId = Guid.NewGuid(),
            CreatedAt = oldTime,
            LastModifiedAt = oldTime,
            Blocks = ImmutableList.Create(imageBlock)
        };
        SetupStateWithNote(note);
        var beforeUpdate = DateTimeOffset.UtcNow;

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.UpdateImageCaption(noteId, blockId, "Caption"), CancellationToken.None);

        // Assert
        capturedAction.ShouldNotBeNull();
        capturedAction.Note.LastModifiedAt.ShouldBeGreaterThanOrEqualTo(beforeUpdate);
    }

    #endregion

    #region UpdateImageWidth Tests

    [Fact]
    public async Task UpdateImageWidth_ShouldUpdateWidthInMetadata()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var imageBlock = CreateImageBlock(blockId);
        var note = CreateNoteWithBlocks(noteId, imageBlock);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.UpdateImageWidth(noteId, blockId, 75), CancellationToken.None);

        // Assert
        capturedAction.ShouldNotBeNull();
        var updatedBlock = capturedAction.Note.Blocks.First(b => b.Id == blockId);
        updatedBlock.Metadata["widthPercent"].ShouldBe(75);
    }

    [Fact]
    public async Task UpdateImageWidth_ShouldClampToMinimum10Percent()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var imageBlock = CreateImageBlock(blockId);
        var note = CreateNoteWithBlocks(noteId, imageBlock);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.UpdateImageWidth(noteId, blockId, 5), CancellationToken.None);

        // Assert
        capturedAction.ShouldNotBeNull();
        var updatedBlock = capturedAction.Note.Blocks.First(b => b.Id == blockId);
        updatedBlock.Metadata["widthPercent"].ShouldBe(10);
    }

    [Fact]
    public async Task UpdateImageWidth_ShouldClampToMaximum100Percent()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var imageBlock = CreateImageBlock(blockId);
        var note = CreateNoteWithBlocks(noteId, imageBlock);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.UpdateImageWidth(noteId, blockId, 150), CancellationToken.None);

        // Assert
        capturedAction.ShouldNotBeNull();
        var updatedBlock = capturedAction.Note.Blocks.First(b => b.Id == blockId);
        updatedBlock.Metadata["widthPercent"].ShouldBe(100);
    }

    [Fact]
    public async Task UpdateImageWidth_ShouldPersistToRepository()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var imageBlock = CreateImageBlock(blockId);
        var note = CreateNoteWithBlocks(noteId, imageBlock);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.UpdateImageWidth(noteId, blockId, 50), CancellationToken.None);

        // Assert
        await _noteRepository.Received(1).Update(Arg.Any<Note>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateImageWidth_WhenNoteNotFound_ShouldLogWarningAndReturn()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        SetupEmptyState();

        // Act
        await _handler.Handle(new NoteCommands.UpdateImageWidth(noteId, blockId, 50), CancellationToken.None);

        // Assert
        _logger.Received(1).LogWarning("Note '{NoteId}' not found in state", noteId);
        await _actionDispatcher.DidNotReceive().Dispatch(Arg.Any<NoteActions.NoteUpdated>());
        await _noteRepository.DidNotReceive().Update(Arg.Any<Note>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateImageWidth_WhenBlockNotFound_ShouldLogWarningAndReturn()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var differentBlock = CreateImageBlock(Guid.NewGuid());
        var note = CreateNoteWithBlocks(noteId, differentBlock);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.UpdateImageWidth(noteId, blockId, 50), CancellationToken.None);

        // Assert
        _logger.Received(1).LogWarning("Image block '{BlockId}' not found in note '{NoteId}'", blockId, noteId);
        await _actionDispatcher.DidNotReceive().Dispatch(Arg.Any<NoteActions.NoteUpdated>());
    }

    [Fact]
    public async Task UpdateImageWidth_WhenBlockIsNotImage_ShouldLogWarningAndReturn()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var paragraphBlock = new Block
        {
            Id = blockId,
            Type = BlockType.Paragraph,
            Content = "Text",
            Order = 0
        };
        var note = CreateNoteWithBlocks(noteId, paragraphBlock);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.UpdateImageWidth(noteId, blockId, 50), CancellationToken.None);

        // Assert
        _logger.Received(1).LogWarning("Image block '{BlockId}' not found in note '{NoteId}'", blockId, noteId);
        await _noteRepository.DidNotReceive().Update(Arg.Any<Note>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateImageWidth_ShouldPreserveOtherMetadata()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var imageBlock = new Block
        {
            Id = blockId,
            Type = BlockType.Image,
            Content = "",
            Order = 0,
            Metadata = new Dictionary<string, object>
            {
                ["attachmentId"] = "some-id",
                ["caption"] = "Original caption"
            }
        };
        var note = CreateNoteWithBlocks(noteId, imageBlock);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.UpdateImageWidth(noteId, blockId, 60), CancellationToken.None);

        // Assert
        capturedAction.ShouldNotBeNull();
        var updatedBlock = capturedAction.Note.Blocks.First(b => b.Id == blockId);
        updatedBlock.Metadata["attachmentId"].ShouldBe("some-id");
        updatedBlock.Metadata["caption"].ShouldBe("Original caption");
        updatedBlock.Metadata["widthPercent"].ShouldBe(60);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task UpdateImageWidth_ShouldAcceptValidWidthValues(int width)
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var blockId = Guid.NewGuid();
        var imageBlock = CreateImageBlock(blockId);
        var note = CreateNoteWithBlocks(noteId, imageBlock);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.UpdateImageWidth(noteId, blockId, width), CancellationToken.None);

        // Assert
        capturedAction.ShouldNotBeNull();
        var updatedBlock = capturedAction.Note.Blocks.First(b => b.Id == blockId);
        updatedBlock.Metadata["widthPercent"].ShouldBe(width);
    }

    #endregion

    private static Block CreateImageBlock(Guid blockId)
    {
        return new Block
        {
            Id = blockId,
            Type = BlockType.Image,
            Content = "",
            Order = 0,
            Metadata = new Dictionary<string, object>
            {
                ["attachmentId"] = Guid.NewGuid().ToString()
            }
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
