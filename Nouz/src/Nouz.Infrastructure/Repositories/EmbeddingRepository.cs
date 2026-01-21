using System.Numerics.Tensors;
using Microsoft.EntityFrameworkCore;
using Nouz.Application.Embeddings;
using Nouz.Domain.Entities;

namespace Nouz.Infrastructure.Repositories;

internal sealed class EmbeddingRepository : IEmbeddingRepository
{
    private readonly IDbContextFactory<NouzDbContext> _contextFactory;

    public EmbeddingRepository(IDbContextFactory<NouzDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<NoteEmbedding?> GetByNoteIdAsync(Guid noteId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await context.NoteEmbeddings
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.NoteId == noteId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpsertAsync(Guid noteId, float[] embedding, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var existing = await context.NoteEmbeddings
            .FirstOrDefaultAsync(e => e.NoteId == noteId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            context.Entry(existing).CurrentValues.SetValues(new
            {
                Embedding = embedding,
                LastUpdatedAt = DateTimeOffset.UtcNow
            });
        }
        else
        {
            var newEmbedding = new NoteEmbedding
            {
                NoteId = noteId,
                Embedding = embedding,
                LastUpdatedAt = DateTimeOffset.UtcNow
            };
            context.NoteEmbeddings.Add(newEmbedding);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid noteId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        await context.NoteEmbeddings
            .Where(e => e.NoteId == noteId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<(Guid NoteId, float Similarity)>> FindSimilarAsync(
        float[] queryEmbedding,
        int topN,
        CancellationToken cancellationToken = default)
    {
        if (queryEmbedding.Length == 0 || topN <= 0)
        {
            return [];
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // Load all embeddings - for small datasets this is efficient enough
        // For larger datasets, consider using a vector database or SQLite VSS extension
        var allEmbeddings = await context.NoteEmbeddings
            .AsNoTracking()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (allEmbeddings.Count == 0)
        {
            return [];
        }

        // Calculate cosine similarity for each embedding
        var similarities = new List<(Guid NoteId, float Similarity)>(allEmbeddings.Count);
        foreach (var noteEmbedding in allEmbeddings)
        {
            if (noteEmbedding.Embedding.Length != queryEmbedding.Length)
            {
                continue;
            }

            var similarity = TensorPrimitives.CosineSimilarity(
                queryEmbedding.AsSpan(),
                noteEmbedding.Embedding.AsSpan());

            similarities.Add((noteEmbedding.NoteId, similarity));
        }

        // Return top N most similar
        return similarities
            .OrderByDescending(x => x.Similarity)
            .Take(topN)
            .ToList();
    }
}
