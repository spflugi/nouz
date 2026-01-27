using Nouz.Application.Chat.Models;
using Nouz.Domain.Entities;

namespace Nouz.Application.Chat;

/// <summary>
/// Service for managing notes and notebooks through the AI assistant.
/// Provides operations that the Semantic Kernel plugin can use.
/// </summary>
public interface INoteManagementService
{
    /// <summary>
    /// Gets all available notebooks.
    /// </summary>
    Task<IReadOnlyList<Notebook>> GetAllNotebooksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new notebook with the specified name.
    /// </summary>
    Task<Notebook> CreateNotebookAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all notes in a specific notebook.
    /// </summary>
    Task<IReadOnlyList<Note>> GetNotebookNotesAsync(Guid notebookId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new note in the specified notebook with the given blocks.
    /// </summary>
    Task<Note> CreateNoteAsync(Guid notebookId, IReadOnlyList<BlockDefinition> blocks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a note by its ID.
    /// </summary>
    Task<Note?> GetNoteAsync(Guid noteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing note with new blocks.
    /// </summary>
    Task<Note> EditNoteAsync(Guid noteId, IReadOnlyList<BlockDefinition> blocks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a note by its ID.
    /// </summary>
    Task DeleteNoteAsync(Guid noteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for notes matching the query across all notebooks.
    /// </summary>
    Task<IReadOnlyList<Note>> SearchNotesAsync(string query, CancellationToken cancellationToken = default);
}
