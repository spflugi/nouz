using Microsoft.EntityFrameworkCore;
using Nouz.Domain.Entities;
using Nouz.Domain.Repositories;

namespace Nouz.Infrastructure.Repositories;

internal sealed class NotebookRepository : INotebookRepository
{
    private readonly IDbContextFactory<NouzDbContext> _contextFactory;

    public NotebookRepository(IDbContextFactory<NouzDbContext> factory)
    {
        _contextFactory = factory;
    }

    public async Task<IReadOnlyList<Notebook>> GetAll(CancellationToken token = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        return await context.Notebooks
            .AsNoTracking()
            .ToListAsync(token).ConfigureAwait(false);
    }

    public async Task<Notebook?> GetById(Guid id, CancellationToken token = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        return await context.Notebooks
            .FindAsync([id], cancellationToken: token).ConfigureAwait(false);
    }

    public async Task Add(Notebook notebook, CancellationToken token = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        context.Notebooks.Add(notebook);
        await context.SaveChangesAsync(token).ConfigureAwait(false);
    }

    public async Task Update(Notebook notebook, CancellationToken token = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        context.Notebooks.Update(notebook);
        await context.SaveChangesAsync(token).ConfigureAwait(false);
    }

    public async Task Delete(Guid id, CancellationToken token = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token).ConfigureAwait(false);

        var notebook = await context.Notebooks
            .FindAsync([id], cancellationToken: token).ConfigureAwait(false);

        if (notebook is not null)
        {
            context.Notebooks.Remove(notebook);
            await context.SaveChangesAsync(token).ConfigureAwait(false);
        }
    }
}