using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Nouz.Infrastructure.Repositories;
using Shouldly;

namespace Nouz.Infrastructure.Unit.Tests.Repositories;

public class DbMigratorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<NouzDbContext> _options;

    public DbMigratorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<NouzDbContext>()
            .UseSqlite(_connection)
            .Options;
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    [Fact]
    public async Task ApplyMigrations_ShouldCreateDatabaseSchema()
    {
        // Arrange
        var factory = new TestDbContextFactory(_options);
        var migrator = new DbMigrator(factory);

        // Act
        await migrator.ApplyMigrations(TestContext.Current.CancellationToken);

        // Assert
        await using var context = factory.CreateDbContext();
        var tableExists = await context.Database
            .ExecuteSqlRawAsync("SELECT name FROM sqlite_master WHERE type='table' AND name='Notebooks'", [], TestContext.Current.CancellationToken);

        // If the table exists, we can query from it without errors
        var notebooks = await context.Notebooks.ToListAsync(TestContext.Current.CancellationToken);
        notebooks.ShouldBeEmpty();
    }

    [Fact]
    public async Task ApplyMigrations_ShouldBeIdempotent()
    {
        // Arrange
        var factory = new TestDbContextFactory(_options);
        var migrator = new DbMigrator(factory);

        // Act - Apply migrations twice
        await migrator.ApplyMigrations(TestContext.Current.CancellationToken);
        await migrator.ApplyMigrations(TestContext.Current.CancellationToken);

        // Assert - Should not throw and database should still work
        await using var context = factory.CreateDbContext();
        var notebooks = await context.Notebooks.ToListAsync(TestContext.Current.CancellationToken);
        notebooks.ShouldBeEmpty();
    }

    [Fact]
    public async Task ApplyMigrations_ShouldRespectCancellationToken()
    {
        // Arrange
        var factory = new TestDbContextFactory(_options);
        var migrator = new DbMigrator(factory);
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            migrator.ApplyMigrations(cts.Token));
    }

    private sealed class TestDbContextFactory : IDbContextFactory<NouzDbContext>
    {
        private readonly DbContextOptions<NouzDbContext> _options;

        public TestDbContextFactory(DbContextOptions<NouzDbContext> options)
        {
            _options = options;
        }

        public NouzDbContext CreateDbContext()
        {
            return new NouzDbContext(_options);
        }
    }
}
