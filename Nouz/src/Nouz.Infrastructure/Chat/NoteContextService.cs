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

    public async Task<IReadOnlyList<NoteContextResult>> GetRelevantNotesAsync(
        string query,
        int topN,
        float minSimilarity = 0f,
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

        // Fetch the actual notes, filtering by minimum similarity
        var results = new List<NoteContextResult>(similarNotes.Count);
        foreach (var (noteId, similarity) in similarNotes)
        {
            if (similarity < minSimilarity)
            {
                continue;
            }

            var note = await _noteRepository.GetById(noteId, cancellationToken).ConfigureAwait(false);
            if (note is not null)
            {
                var title = GetNoteTitle(note) ?? "(Untitled)";
                results.Add(new NoteContextResult(note, title, similarity));
            }
        }

        return results;
    }

    private static string? GetNoteTitle(Note note)
    {
        // Try to find an H1 block first
        var h1Block = note.Blocks.FirstOrDefault(b => b.Type == BlockType.H1);
        if (h1Block is not null && !string.IsNullOrWhiteSpace(h1Block.Content))
        {
            return h1Block.Content.Length > 50 ? h1Block.Content[..50] + "..." : h1Block.Content;
        }

        // Fall back to first non-empty block
        var firstBlock = note.Blocks
            .OrderBy(b => b.Order)
            .FirstOrDefault(b => !string.IsNullOrWhiteSpace(b.Content));

        if (firstBlock is not null)
        {
            return firstBlock.Content.Length > 50 ? firstBlock.Content[..50] + "..." : firstBlock.Content;
        }

        return null;
    }
}
