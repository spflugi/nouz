using Nouz.Domain.Entities;

namespace Nouz.Domain.Repositories;

public interface INoteRepository
{
    /// <summary>
    /// Return all notes that belong to the specified notebook.
    /// </summary>
    public Task<IReadOnlyList<Note>> GetAllByNotebook(Guid notebookId, CancellationToken token = default);

    /// <summary>
    /// Return the note with the specified id - if any - null otherwise.
    /// </summary>
    public Task<Note?> GetById(Guid noteId, CancellationToken token = default);

    /// <summary>
    /// Add a new note.
    /// </summary>
    public Task Add(Note note, CancellationToken token = default);

    /// <summary>
    /// Update an existing note.
    /// </summary>
    public Task Update(Note note, CancellationToken token = default);

    /// <summary>
    /// Delete an existing note.
    /// </summary>
    public Task Delete(Guid noteId, CancellationToken token = default);

    /// <summary>
    /// Move an existing note to a new notebook.
    /// </summary>
    public Task MoveToNotebook(Guid noteId, Guid newNotebookId, CancellationToken token = default);

    /// <summary>
    /// Search for notes in a specific notebook that match the specified query.
    /// </summary>
    public Task<IReadOnlyList<Note>> SearchInNotebook(Guid notebookId, string query, CancellationToken token = default);
}