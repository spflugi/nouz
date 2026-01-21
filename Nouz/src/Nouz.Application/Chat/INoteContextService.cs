using Nouz.Domain.Entities;

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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of the most relevant notes.</returns>
    Task<IReadOnlyList<Note>> GetRelevantNotesAsync(
        string query,
        int topN,
        CancellationToken cancellationToken = default);
}
