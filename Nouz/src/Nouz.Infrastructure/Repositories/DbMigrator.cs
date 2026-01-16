using Microsoft.EntityFrameworkCore;
using Nouz.Domain.Repositories;

namespace Nouz.Infrastructure.Repositories;

internal sealed class DbMigrator : IDbMigrator
{
    private readonly IDbContextFactory<NouzDbContext> _factory;

    public DbMigrator(IDbContextFactory<NouzDbContext> factory)
    {
        _factory = factory;
    }

    public async Task ApplyMigrations(CancellationToken token = default)
    {
        await using var context = await _factory.CreateDbContextAsync(token).ConfigureAwait(false);
        await context.Database.MigrateAsync(token).ConfigureAwait(false);
    }
}