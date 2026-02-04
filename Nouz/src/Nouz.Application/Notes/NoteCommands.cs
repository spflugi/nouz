using Mediator;
using Nouz.Domain.Entities;

namespace Nouz.Application.Notes;

public static class NoteCommands
{
    /// <summary>
    /// Load all notes for a specific notebook.
    /// </summary>
    public sealed record LoadNotesForNotebook(Guid NotebookId) : ICommand;

    /// <summary>
    /// Create a new note with a single empty paragraph block.
    /// </summary>
    public sealed record CreateNote(Guid NotebookId) : ICommand;

    /// <summary>
    /// Create a new note with a meeting template.
    /// </summary>
    public sealed record CreateMeetingNote(Guid NotebookId) : ICommand;

    /// <summary>
    /// Delete an existing note.
    /// </summary>
    public sealed record DeleteNote(Guid NoteId) : ICommand;

    /// <summary>
    /// Update a note with new blocks.
    /// </summary>
    public sealed record UpdateNote(Note Note) : ICommand;

    /// <summary>
    /// Add a new block after the specified block.
    /// </summary>
    public sealed record AddBlock(Guid NoteId, Guid? AfterBlockId, BlockType Type, Dictionary<string, object>? Metadata = null) : ICommand;

    /// <summary>
    /// Update an existing block's content and metadata.
    /// </summary>
    public sealed record UpdateBlock(Guid NoteId, Block Block) : ICommand;

    /// <summary>
    /// Delete a block from a note.
    /// </summary>
    public sealed record DeleteBlock(Guid NoteId, Guid BlockId) : ICommand;

    /// <summary>
    /// Change the type of an existing block.
    /// </summary>
    public sealed record ChangeBlockType(Guid NoteId, Guid BlockId, BlockType NewType) : ICommand;

    /// <summary>
    /// Set which block is currently being edited.
    /// </summary>
    public sealed record SetEditingBlock(Guid? NoteId, Guid? BlockId) : ICommand;

    /// <summary>
    /// Clear all notes from state (when no notebook is selected).
    /// </summary>
    public sealed record ClearNotes : ICommand;

    /// <summary>
    /// Search for notes in the specified notebook.
    /// </summary>
    public sealed record SearchNotes(Guid NotebookId, string Query) : ICommand;

    /// <summary>
    /// Clear the search results and show all notes.
    /// </summary>
    public sealed record ClearSearch : ICommand;

    /// <summary>
    /// Move a note to a different notebook.
    /// </summary>
    public sealed record MoveNote(Guid NoteId, Guid NewNotebookId) : ICommand;

    /// <summary>
    /// Reorder blocks within a note by moving a block to a new position.
    /// </summary>
    public sealed record ReorderBlocks(Guid NoteId, Guid BlockId, int NewIndex) : ICommand;

    /// <summary>
    /// Add an image block with binary image data.
    /// </summary>
    /// <param name="NoteId">The note to add the image to</param>
    /// <param name="AfterBlockId">The block to insert after (null for beginning)</param>
    /// <param name="ImageData">Base64-encoded image data</param>
    /// <param name="FileName">Original file name</param>
    /// <param name="MimeType">MIME type of the image (e.g., "image/png")</param>
    public sealed record AddImageBlock(Guid NoteId, Guid? AfterBlockId, string ImageData, string FileName, string MimeType) : ICommand;

    /// <summary>
    /// Update an image block's caption.
    /// </summary>
    public sealed record UpdateImageCaption(Guid NoteId, Guid BlockId, string Caption) : ICommand;

    /// <summary>
    /// Update an image block's width.
    /// </summary>
    /// <param name="NoteId">The note containing the image</param>
    /// <param name="BlockId">The image block to update</param>
    /// <param name="WidthPercent">Width as a percentage (10-100)</param>
    public sealed record UpdateImageWidth(Guid NoteId, Guid BlockId, int WidthPercent) : ICommand;

    /// <summary>
    /// Export a note as a self-contained HTML document.
    /// </summary>
    /// <param name="NoteId">The note to export</param>
    public sealed record ExportNoteAsHtml(Guid NoteId) : ICommand<string>;

    /// <summary>
    /// Toggle between edit mode and preview mode for a Mermaid diagram block.
    /// </summary>
    public sealed record ToggleMermaidEditMode(Guid NoteId, Guid BlockId) : ICommand;

    /// <summary>
    /// Add a new row to a table block.
    /// </summary>
    /// <param name="NoteId">The note containing the table</param>
    /// <param name="BlockId">The table block to modify</param>
    /// <param name="AfterRowIndex">Insert after this row index (null for end)</param>
    public sealed record AddTableRow(Guid NoteId, Guid BlockId, int? AfterRowIndex = null) : ICommand;

    /// <summary>
    /// Remove a row from a table block.
    /// </summary>
    /// <param name="NoteId">The note containing the table</param>
    /// <param name="BlockId">The table block to modify</param>
    /// <param name="RowIndex">The index of the row to remove</param>
    public sealed record RemoveTableRow(Guid NoteId, Guid BlockId, int RowIndex) : ICommand;

    /// <summary>
    /// Add a new column to a table block.
    /// </summary>
    /// <param name="NoteId">The note containing the table</param>
    /// <param name="BlockId">The table block to modify</param>
    /// <param name="AfterColumnIndex">Insert after this column index (null for end)</param>
    public sealed record AddTableColumn(Guid NoteId, Guid BlockId, int? AfterColumnIndex = null) : ICommand;

    /// <summary>
    /// Remove a column from a table block.
    /// </summary>
    /// <param name="NoteId">The note containing the table</param>
    /// <param name="BlockId">The table block to modify</param>
    /// <param name="ColumnIndex">The index of the column to remove</param>
    public sealed record RemoveTableColumn(Guid NoteId, Guid BlockId, int ColumnIndex) : ICommand;

    /// <summary>
    /// Update the content of a cell in a table block.
    /// </summary>
    /// <param name="NoteId">The note containing the table</param>
    /// <param name="BlockId">The table block to modify</param>
    /// <param name="RowIndex">The row index of the cell</param>
    /// <param name="ColIndex">The column index of the cell</param>
    /// <param name="Content">The new cell content</param>
    public sealed record UpdateTableCell(Guid NoteId, Guid BlockId, int RowIndex, int ColIndex, string Content) : ICommand;
}
