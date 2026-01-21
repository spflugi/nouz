using Nouz.Domain.Entities;

namespace Nouz.Application.Embeddings;

/// <summary>
/// Repository for storing and retrieving note embeddings.
/// </summary>
public interface IEmbeddingRepository
{
    /// <summary>
    /// Gets the embedding for a specific note.
    /// </summary>
    Task<NoteEmbedding?> GetByNoteIdAsync(Guid noteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates the embedding for a note.
    /// </summary>
    Task UpsertAsync(Guid noteId, float[] embedding, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the embedding for a note.
    /// </summary>
    Task DeleteAsync(Guid noteId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the most similar notes to the given query embedding.
    /// </summary>
    /// <param name="queryEmbedding">The embedding to compare against.</param>
    /// <param name="topN">The maximum number of results to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of note IDs with their similarity scores, ordered by similarity descending.</returns>
    Task<IReadOnlyList<(Guid NoteId, float Similarity)>> FindSimilarAsync(
        float[] queryEmbedding,
        int topN,
        CancellationToken cancellationToken = default);
}
