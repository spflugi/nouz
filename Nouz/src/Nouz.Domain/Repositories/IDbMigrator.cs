namespace Nouz.Domain.Repositories;

public interface IDbMigrator
{
    Task ApplyMigrations(CancellationToken token = default);
}