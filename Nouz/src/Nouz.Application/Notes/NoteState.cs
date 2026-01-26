using System.Collections.Immutable;
using Nouz.Domain.Entities;

namespace Nouz.Application.Notes;

public sealed record NoteState
{
    public ImmutableList<Note> Notes { get; init; } = [];

    public string SearchQuery { get; init; } = string.Empty;

    public ImmutableList<Note>? FilteredNotes { get; init; }

    public Guid? EditingNoteId { get; init; }

    public Guid? EditingBlockId { get; init; }

    /// <summary>
    /// Attachments indexed by NoteId for efficient lookup.
    /// </summary>
    public ImmutableDictionary<Guid, ImmutableList<NoteAttachment>> AttachmentsByNoteId { get; init; }
        = ImmutableDictionary<Guid, ImmutableList<NoteAttachment>>.Empty;

    /// <summary>
    /// Gets the notes to display - filtered notes if search is active, otherwise all notes.
    /// </summary>
    public ImmutableList<Note> DisplayNotes => FilteredNotes ?? Notes;
}
