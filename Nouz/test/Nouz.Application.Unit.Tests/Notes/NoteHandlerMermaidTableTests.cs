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

public class NoteHandlerMermaidTableTests
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

    public NoteHandlerMermaidTableTests()
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

    #region ChangeBlockType to Mermaid Tests

    [Fact]
    public async Task ChangeBlockType_ToMermaid_ShouldSetDefaultContent()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block = CreateBlock(BlockType.Paragraph, "Content");
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.ChangeBlockType(noteId, block.Id, BlockType.Mermaid), TestContext.Current.CancellationToken);

        // Assert
        capturedAction.ShouldNotBeNull();
        var mermaidBlock = capturedAction.Note.Blocks.First();
        mermaidBlock.Type.ShouldBe(BlockType.Mermaid);
        mermaidBlock.Content.ShouldContain("graph TD");
        mermaidBlock.Metadata.ShouldContainKey("isEditMode");
        mermaidBlock.Metadata["isEditMode"].ShouldBe(true);
    }

    [Fact]
    public async Task ChangeBlockType_ToMermaid_ShouldNotAddParagraphAfter()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block = CreateBlock(BlockType.Paragraph, "Content");
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.ChangeBlockType(noteId, block.Id, BlockType.Mermaid), TestContext.Current.CancellationToken);

        // Assert
        capturedAction.ShouldNotBeNull();
        capturedAction.Note.Blocks.Count.ShouldBe(1);
        capturedAction.Note.Blocks[0].Type.ShouldBe(BlockType.Mermaid);
    }

    [Fact]
    public async Task ChangeBlockType_ToMermaid_ShouldFocusMermaidBlock()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block = CreateBlock(BlockType.Paragraph, "Content");
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.ChangeBlockType(noteId, block.Id, BlockType.Mermaid), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NoteActions.EditingBlockChanged>(a => a.BlockId == block.Id));
    }

    #endregion

    #region ToggleMermaidEditMode Tests

    [Fact]
    public async Task ToggleMermaidEditMode_ShouldToggleFromEditToPreview()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.Mermaid,
            Content = "graph TD\n    A --> B",
            Metadata = new Dictionary<string, object> { ["isEditMode"] = true }
        };
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.ToggleMermaidEditMode(noteId, block.Id), TestContext.Current.CancellationToken);

        // Assert
        capturedAction.ShouldNotBeNull();
        var updatedBlock = capturedAction.Note.Blocks.First();
        updatedBlock.Metadata["isEditMode"].ShouldBe(false);
    }

    [Fact]
    public async Task ToggleMermaidEditMode_ShouldToggleFromPreviewToEdit()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.Mermaid,
            Content = "graph TD\n    A --> B",
            Metadata = new Dictionary<string, object> { ["isEditMode"] = false }
        };
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.ToggleMermaidEditMode(noteId, block.Id), TestContext.Current.CancellationToken);

        // Assert
        capturedAction.ShouldNotBeNull();
        var updatedBlock = capturedAction.Note.Blocks.First();
        updatedBlock.Metadata["isEditMode"].ShouldBe(true);
    }

    [Fact]
    public async Task ToggleMermaidEditMode_WhenBlockNotMermaid_ShouldNotUpdate()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block = CreateBlock(BlockType.Paragraph, "Content");
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.ToggleMermaidEditMode(noteId, block.Id), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.DidNotReceive().Dispatch(Arg.Any<NoteActions.NoteUpdated>());
        _logger.Received(1).LogWarning("Mermaid block '{BlockId}' not found in note '{NoteId}'", block.Id, noteId);
    }

    #endregion

    #region ChangeBlockType to Table Tests

    [Fact]
    public async Task ChangeBlockType_ToTable_ShouldSetDefaultMetadata()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block = CreateBlock(BlockType.Paragraph, "Content");
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.ChangeBlockType(noteId, block.Id, BlockType.Table), TestContext.Current.CancellationToken);

        // Assert
        capturedAction.ShouldNotBeNull();
        var tableBlock = capturedAction.Note.Blocks.First();
        tableBlock.Type.ShouldBe(BlockType.Table);
        tableBlock.Metadata.ShouldContainKey("rows");
        tableBlock.Metadata.ShouldContainKey("columns");
        tableBlock.Metadata.ShouldContainKey("data");
        tableBlock.Metadata.ShouldContainKey("hasHeader");
        tableBlock.Metadata["rows"].ShouldBe(3);
        tableBlock.Metadata["columns"].ShouldBe(3);
        tableBlock.Metadata["hasHeader"].ShouldBe(true);
    }

    [Fact]
    public async Task ChangeBlockType_ToTable_ShouldNotAddParagraphAfter()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block = CreateBlock(BlockType.Paragraph, "Content");
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.ChangeBlockType(noteId, block.Id, BlockType.Table), TestContext.Current.CancellationToken);

        // Assert
        capturedAction.ShouldNotBeNull();
        capturedAction.Note.Blocks.Count.ShouldBe(1);
        capturedAction.Note.Blocks[0].Type.ShouldBe(BlockType.Table);
    }

    [Fact]
    public async Task ChangeBlockType_ToTable_ShouldFocusTableBlock()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var block = CreateBlock(BlockType.Paragraph, "Content");
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.ChangeBlockType(noteId, block.Id, BlockType.Table), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NoteActions.EditingBlockChanged>(a => a.BlockId == block.Id));
    }

    #endregion

    #region AddTableRow Tests

    [Fact]
    public async Task AddTableRow_ShouldAddRowAtEnd()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var tableData = new List<List<string>>
        {
            new() { "A", "B" },
            new() { "1", "2" }
        };
        var block = CreateTableBlock(tableData, 2, 2);
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.AddTableRow(noteId, block.Id), TestContext.Current.CancellationToken);

        // Assert
        capturedAction.ShouldNotBeNull();
        var updatedBlock = capturedAction.Note.Blocks.First();
        updatedBlock.Metadata["rows"].ShouldBe(3);
        var data = (List<List<string>>)updatedBlock.Metadata["data"];
        data.Count.ShouldBe(3);
        data[2].ShouldAllBe(s => s == string.Empty);
    }

    [Fact]
    public async Task AddTableRow_ShouldAddRowAfterSpecifiedIndex()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var tableData = new List<List<string>>
        {
            new() { "A", "B" },
            new() { "1", "2" }
        };
        var block = CreateTableBlock(tableData, 2, 2);
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.AddTableRow(noteId, block.Id, 0), TestContext.Current.CancellationToken);

        // Assert
        capturedAction.ShouldNotBeNull();
        var data = (List<List<string>>)capturedAction.Note.Blocks.First().Metadata["data"];
        data.Count.ShouldBe(3);
        data[0].ShouldBe(new List<string> { "A", "B" });
        data[1].ShouldAllBe(s => s == string.Empty);
        data[2].ShouldBe(new List<string> { "1", "2" });
    }

    #endregion

    #region RemoveTableRow Tests

    [Fact]
    public async Task RemoveTableRow_ShouldRemoveRow()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var tableData = new List<List<string>>
        {
            new() { "A", "B" },
            new() { "1", "2" },
            new() { "3", "4" }
        };
        var block = CreateTableBlock(tableData, 3, 2);
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.RemoveTableRow(noteId, block.Id, 1), TestContext.Current.CancellationToken);

        // Assert
        capturedAction.ShouldNotBeNull();
        var updatedBlock = capturedAction.Note.Blocks.First();
        updatedBlock.Metadata["rows"].ShouldBe(2);
        var data = (List<List<string>>)updatedBlock.Metadata["data"];
        data.Count.ShouldBe(2);
        data[0].ShouldBe(new List<string> { "A", "B" });
        data[1].ShouldBe(new List<string> { "3", "4" });
    }

    [Fact]
    public async Task RemoveTableRow_WhenOnlyOneRow_ShouldNotRemove()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var tableData = new List<List<string>>
        {
            new() { "A", "B" }
        };
        var block = CreateTableBlock(tableData, 1, 2);
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.RemoveTableRow(noteId, block.Id, 0), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.DidNotReceive().Dispatch(Arg.Any<NoteActions.NoteUpdated>());
        _logger.Received(1).LogDebug("Cannot remove the last row from table block '{BlockId}'", block.Id);
    }

    [Fact]
    public async Task RemoveTableRow_WithInvalidIndex_ShouldNotRemove()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var tableData = new List<List<string>>
        {
            new() { "A", "B" },
            new() { "1", "2" }
        };
        var block = CreateTableBlock(tableData, 2, 2);
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.RemoveTableRow(noteId, block.Id, 10), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.DidNotReceive().Dispatch(Arg.Any<NoteActions.NoteUpdated>());
        _logger.Received(1).LogWarning("Invalid row index {RowIndex} for table block '{BlockId}'", 10, block.Id);
    }

    #endregion

    #region AddTableColumn Tests

    [Fact]
    public async Task AddTableColumn_ShouldAddColumnAtEnd()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var tableData = new List<List<string>>
        {
            new() { "A", "B" },
            new() { "1", "2" }
        };
        var block = CreateTableBlock(tableData, 2, 2);
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.AddTableColumn(noteId, block.Id), TestContext.Current.CancellationToken);

        // Assert
        capturedAction.ShouldNotBeNull();
        var updatedBlock = capturedAction.Note.Blocks.First();
        updatedBlock.Metadata["columns"].ShouldBe(3);
        var data = (List<List<string>>)updatedBlock.Metadata["data"];
        data[0].ShouldBe(new List<string> { "A", "B", "" });
        data[1].ShouldBe(new List<string> { "1", "2", "" });
    }

    [Fact]
    public async Task AddTableColumn_ShouldAddColumnAfterSpecifiedIndex()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var tableData = new List<List<string>>
        {
            new() { "A", "B" },
            new() { "1", "2" }
        };
        var block = CreateTableBlock(tableData, 2, 2);
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.AddTableColumn(noteId, block.Id, 0), TestContext.Current.CancellationToken);

        // Assert
        capturedAction.ShouldNotBeNull();
        var data = (List<List<string>>)capturedAction.Note.Blocks.First().Metadata["data"];
        data[0].ShouldBe(new List<string> { "A", "", "B" });
        data[1].ShouldBe(new List<string> { "1", "", "2" });
    }

    #endregion

    #region RemoveTableColumn Tests

    [Fact]
    public async Task RemoveTableColumn_ShouldRemoveColumn()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var tableData = new List<List<string>>
        {
            new() { "A", "B", "C" },
            new() { "1", "2", "3" }
        };
        var block = CreateTableBlock(tableData, 2, 3);
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.RemoveTableColumn(noteId, block.Id, 1), TestContext.Current.CancellationToken);

        // Assert
        capturedAction.ShouldNotBeNull();
        var updatedBlock = capturedAction.Note.Blocks.First();
        updatedBlock.Metadata["columns"].ShouldBe(2);
        var data = (List<List<string>>)updatedBlock.Metadata["data"];
        data[0].ShouldBe(new List<string> { "A", "C" });
        data[1].ShouldBe(new List<string> { "1", "3" });
    }

    [Fact]
    public async Task RemoveTableColumn_WhenOnlyOneColumn_ShouldNotRemove()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var tableData = new List<List<string>>
        {
            new() { "A" },
            new() { "1" }
        };
        var block = CreateTableBlock(tableData, 2, 1);
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.RemoveTableColumn(noteId, block.Id, 0), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.DidNotReceive().Dispatch(Arg.Any<NoteActions.NoteUpdated>());
        _logger.Received(1).LogDebug("Cannot remove the last column from table block '{BlockId}'", block.Id);
    }

    #endregion

    #region UpdateTableCell Tests

    [Fact]
    public async Task UpdateTableCell_ShouldUpdateCellContent()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var tableData = new List<List<string>>
        {
            new() { "A", "B" },
            new() { "1", "2" }
        };
        var block = CreateTableBlock(tableData, 2, 2);
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        NoteActions.NoteUpdated? capturedAction = null;
        await _actionDispatcher.Dispatch(Arg.Do<NoteActions.NoteUpdated>(a => capturedAction = a));

        // Act
        await _handler.Handle(new NoteCommands.UpdateTableCell(noteId, block.Id, 1, 1, "Updated"), TestContext.Current.CancellationToken);

        // Assert
        capturedAction.ShouldNotBeNull();
        var data = (List<List<string>>)capturedAction.Note.Blocks.First().Metadata["data"];
        data[1][1].ShouldBe("Updated");
    }

    [Fact]
    public async Task UpdateTableCell_WithInvalidRowIndex_ShouldNotUpdate()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var tableData = new List<List<string>>
        {
            new() { "A", "B" }
        };
        var block = CreateTableBlock(tableData, 1, 2);
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.UpdateTableCell(noteId, block.Id, 5, 0, "Value"), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.DidNotReceive().Dispatch(Arg.Any<NoteActions.NoteUpdated>());
        _logger.Received(1).LogWarning("Invalid row index {RowIndex} for table block '{BlockId}'", 5, block.Id);
    }

    [Fact]
    public async Task UpdateTableCell_WithInvalidColIndex_ShouldNotUpdate()
    {
        // Arrange
        var noteId = Guid.NewGuid();
        var tableData = new List<List<string>>
        {
            new() { "A", "B" }
        };
        var block = CreateTableBlock(tableData, 1, 2);
        var note = CreateNoteWithBlocks(noteId, block);
        SetupStateWithNote(note);

        // Act
        await _handler.Handle(new NoteCommands.UpdateTableCell(noteId, block.Id, 0, 5, "Value"), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.DidNotReceive().Dispatch(Arg.Any<NoteActions.NoteUpdated>());
        _logger.Received(1).LogWarning("Invalid column index {ColIndex} for table block '{BlockId}'", 5, block.Id);
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

    private static Block CreateTableBlock(List<List<string>> data, int rows, int columns)
    {
        return new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.Table,
            Content = string.Empty,
            Order = 0,
            Metadata = new Dictionary<string, object>
            {
                ["rows"] = rows,
                ["columns"] = columns,
                ["data"] = data,
                ["hasHeader"] = true
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
}
