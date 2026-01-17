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
}
