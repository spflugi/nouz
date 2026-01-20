using System.Collections.Immutable;
using Nouz.Application.Store;
using Nouz.Domain.Entities;

namespace Nouz.Application.Notes;

public static class NoteActions
{
    /// <summary>
    /// Triggered when notes for a notebook are loaded.
    /// </summary>
    public sealed record NotesLoaded(ImmutableList<Note> Notes) : IAction;

    /// <summary>
    /// Triggered when a new note is created.
    /// </summary>
    public sealed record NoteCreated(Note Note) : IAction;

    /// <summary>
    /// Triggered when a note is updated.
    /// </summary>
    public sealed record NoteUpdated(Note Note) : IAction;

    /// <summary>
    /// Triggered when a note is deleted.
    /// </summary>
    public sealed record NoteDeleted(Guid NoteId) : IAction;

    /// <summary>
    /// Triggered when the editing block changes.
    /// </summary>
    public sealed record EditingBlockChanged(Guid? NoteId, Guid? BlockId) : IAction;

    /// <summary>
    /// Triggered when notes are cleared (e.g., when no notebook is selected).
    /// </summary>
    public sealed record NotesCleared : IAction;

    /// <summary>
    /// Triggered when search results are returned.
    /// </summary>
    public sealed record SearchResultsLoaded(string Query, ImmutableList<Note> FilteredNotes) : IAction;

    /// <summary>
    /// Triggered when search is cleared.
    /// </summary>
    public sealed record SearchCleared : IAction;

    /// <summary>
    /// Triggered when a note is moved to a different notebook.
    /// </summary>
    public sealed record NoteMoved(Guid NoteId, Guid NewNotebookId) : IAction;
}
