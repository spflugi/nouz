using Nouz.Application.Chat;
using Nouz.Application.Embeddings;
using Nouz.Domain.Entities;
using Nouz.Domain.Repositories;

namespace Nouz.Infrastructure.Chat;

internal sealed class NoteContextService : INoteContextService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IEmbeddingRepository _embeddingRepository;
    private readonly INoteRepository _noteRepository;

    public NoteContextService(
        IEmbeddingService embeddingService,
        IEmbeddingRepository embeddingRepository,
        INoteRepository noteRepository)
    {
        _embeddingService = embeddingService;
        _embeddingRepository = embeddingRepository;
        _noteRepository = noteRepository;
    }

    public async Task<IReadOnlyList<Note>> GetRelevantNotesAsync(
        string query,
        int topN,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || topN <= 0)
        {
            return [];
        }

        // Generate embedding for the query
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken).ConfigureAwait(false);
        if (queryEmbedding.Length == 0)
        {
            return [];
        }

        // Find similar notes
        var similarNotes = await _embeddingRepository.FindSimilarAsync(
            queryEmbedding,
            topN,
            cancellationToken).ConfigureAwait(false);

        if (similarNotes.Count == 0)
        {
            return [];
        }

        // Fetch the actual notes
        var notes = new List<Note>(similarNotes.Count);
        foreach (var (noteId, _) in similarNotes)
        {
            var note = await _noteRepository.GetById(noteId, cancellationToken).ConfigureAwait(false);
            if (note is not null)
            {
                notes.Add(note);
            }
        }

        return notes;
    }
}
