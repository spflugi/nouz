namespace Nouz.Application.Chat;

/// <summary>
/// Service for retrieving relevant notes to provide as context for chat.
/// </summary>
public interface INoteContextService
{
    /// <summary>
    /// Gets the most relevant notes for a given query.
    /// </summary>
    /// <param name="query">The user's query to find relevant notes for.</param>
    /// <param name="topN">The maximum number of notes to return.</param>
    /// <param name="minSimilarity">The minimum similarity score for notes to be included (0.0-1.0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of the most relevant notes with their similarity scores and titles.</returns>
    Task<IReadOnlyList<NoteContextResult>> GetRelevantNotesAsync(
        string query,
        int topN,
        float minSimilarity = 0f,
        CancellationToken cancellationToken = default);
}
