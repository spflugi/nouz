using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Nouz.Domain.Entities;
using Nouz.Domain.Repositories;

namespace Nouz.Infrastructure.Repositories;

internal sealed class NoteRepository : INoteRepository
{
    private readonly IDbContextFactory<NouzDbContext> _contextFactory;

    public NoteRepository(IDbContextFactory<NouzDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<Note>> GetAllByNotebook(Guid notebookId, CancellationToken token = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        var notes = await context.Notes
            .Where(n => n.NotebookId == notebookId)
            .AsNoTracking()
            .ToListAsync(token)
            .ConfigureAwait(false);

        if (notes.Count == 0)
        {
            return notes;
        }

        var noteIds = notes.Select(n => n.Id).ToList();

        // Project NoteId within the query to avoid EF.Property outside LINQ
        var blocksWithNoteId = await context.Set<Block>()
            .Where(b => noteIds.Contains(EF.Property<Guid>(b, "NoteId")))
            .Select(b => new { Block = b, NoteId = EF.Property<Guid>(b, "NoteId") })
            .AsNoTracking()
            .ToListAsync(token)
            .ConfigureAwait(false);

        var blocksByNoteId = blocksWithNoteId
            .GroupBy(x => x.NoteId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Block).ToImmutableList());

        return notes
            .Select(n => n with
            {
                Blocks = blocksByNoteId.TryGetValue(n.Id, out var noteBlocks)
                    ? noteBlocks
                    : []
            })
            .ToList();
    }

    public async Task<Note?> GetById(Guid noteId, CancellationToken token = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        var note = await context.Notes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == noteId, token)
            .ConfigureAwait(false);

        if (note is null)
        {
            return null;
        }

        var blocks = await context.Set<Block>()
            .Where(b => EF.Property<Guid>(b, "NoteId") == noteId)
            .AsNoTracking()
            .ToListAsync(token)
            .ConfigureAwait(false);

        return note with { Blocks = blocks.ToImmutableList() };
    }

    public async Task Add(Note note, CancellationToken token = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        context.Notes.Add(note);

        foreach (var block in note.Blocks)
        {
            context.Entry(block).Property("NoteId").CurrentValue = note.Id;
            context.Set<Block>().Add(block);
        }

        await context.SaveChangesAsync(token).ConfigureAwait(false);
    }

    public async Task Update(Note note, CancellationToken token = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        context.Notes.Update(note);

        // Get existing block IDs for this note (without tracking)
        var existingBlockIds = await context.Set<Block>()
            .Where(b => EF.Property<Guid>(b, "NoteId") == note.Id)
            .Select(b => b.Id)
            .ToListAsync(token)
            .ConfigureAwait(false);

        var existingBlockIdSet = existingBlockIds.ToHashSet();
        var newBlockIds = note.Blocks.Select(b => b.Id).ToHashSet();

        // Delete blocks that are no longer in the note
        var blockIdsToDelete = existingBlockIdSet.Except(newBlockIds).ToList();
        if (blockIdsToDelete.Count > 0)
        {
            await context.Set<Block>()
                .Where(b => blockIdsToDelete.Contains(b.Id))
                .ExecuteDeleteAsync(token)
                .ConfigureAwait(false);
        }

        // Add or update blocks
        foreach (var block in note.Blocks)
        {
            var entry = context.Entry(block);
            entry.Property("NoteId").CurrentValue = note.Id;

            if (existingBlockIdSet.Contains(block.Id))
            {
                entry.State = EntityState.Modified;
            }
            else
            {
                entry.State = EntityState.Added;
            }
        }

        await context.SaveChangesAsync(token).ConfigureAwait(false);
    }

    public async Task Delete(Guid noteId, CancellationToken token = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        var note = await context.Notes
            .FindAsync([noteId], cancellationToken: token).ConfigureAwait(false);

        if (note is not null)
        {
            // Blocks will be cascade deleted due to FK constraint
            context.Notes.Remove(note);
            await context.SaveChangesAsync(token).ConfigureAwait(false);
        }
    }

    public async Task MoveToNotebook(Guid noteId, Guid newNotebookId, CancellationToken token = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        await context.Notes
            .Where(n => n.Id == noteId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(n => n.NotebookId, newNotebookId)
                    .SetProperty(n => n.LastModifiedAt, DateTimeOffset.UtcNow),
                token)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Note>> Search(string query, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        // Use FTS5 MATCH for fast full-text search
        // Get distinct NoteIds that have matching blocks
        var matchingNoteIds = await context.Database
            .SqlQuery<Guid>($"""
                SELECT DISTINCT CAST(NoteId AS TEXT) as Value
                FROM BlockSearch
                WHERE BlockSearch MATCH {query}
                """)
            .ToListAsync(token)
            .ConfigureAwait(false);

        if (matchingNoteIds.Count == 0)
        {
            return [];
        }

        // Fetch notes with their blocks
        var notes = await context.Notes
            .Where(n => matchingNoteIds.Contains(n.Id))
            .AsNoTracking()
            .ToListAsync(token)
            .ConfigureAwait(false);

        // Fetch blocks for matching notes
        var blocksWithNoteId = await context.Set<Block>()
            .Where(b => matchingNoteIds.Contains(EF.Property<Guid>(b, "NoteId")))
            .Select(b => new { Block = b, NoteId = EF.Property<Guid>(b, "NoteId") })
            .AsNoTracking()
            .ToListAsync(token)
            .ConfigureAwait(false);

        var blocksByNoteId = blocksWithNoteId
            .GroupBy(x => x.NoteId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Block).ToImmutableList());

        return notes
            .Select(n => n with
            {
                Blocks = blocksByNoteId.TryGetValue(n.Id, out var noteBlocks)
                    ? noteBlocks
                    : []
            })
            .ToList();
    }
}