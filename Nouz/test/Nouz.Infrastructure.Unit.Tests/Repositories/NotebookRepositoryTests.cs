using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Nouz.Domain.Entities;
using Nouz.Infrastructure.Repositories;
using Shouldly;

namespace Nouz.Infrastructure.Unit.Tests.Repositories;

public class NotebookRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IDbContextFactory<NouzDbContext> _contextFactory;
    private readonly NotebookRepository _repository;

    public NotebookRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<NouzDbContext>()
            .UseSqlite(_connection)
            .Options;

        _contextFactory = new TestDbContextFactory(options);

        // Create the schema
        using var context = _contextFactory.CreateDbContext();
        context.Database.EnsureCreated();

        _repository = new NotebookRepository(_contextFactory);
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WhenNoNotebooks_ShouldReturnEmptyList()
    {
        // Act
        var result = await _repository.GetAll();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAll_WhenNotebooksExist_ShouldReturnAllNotebooks()
    {
        // Arrange
        var notebook1 = CreateNotebook("Notebook 1");
        var notebook2 = CreateNotebook("Notebook 2");
        await AddNotebook(notebook1);
        await AddNotebook(notebook2);

        // Act
        var result = await _repository.GetAll();

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(n => n.Name == "Notebook 1");
        result.ShouldContain(n => n.Name == "Notebook 2");
    }

    [Fact]
    public async Task GetAll_ShouldRespectCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            _repository.GetAll(cts.Token));
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WhenNotebookExists_ShouldReturnNotebook()
    {
        // Arrange
        var notebook = CreateNotebook("Test Notebook");
        await AddNotebook(notebook);

        // Act
        var result = await _repository.GetById(notebook.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(notebook.Id);
        result.Name.ShouldBe("Test Notebook");
    }

    [Fact]
    public async Task GetById_WhenNotebookDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetById(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region Add Tests

    [Fact]
    public async Task Add_ShouldPersistNotebook()
    {
        // Arrange
        var notebook = CreateNotebook("New Notebook");

        // Act
        await _repository.Add(notebook);

        // Assert
        var result = await _repository.GetById(notebook.Id);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("New Notebook");
    }

    [Fact]
    public async Task Add_ShouldPersistAllProperties()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow.AddDays(-1);
        var lastModified = DateTimeOffset.UtcNow;
        var notebook = new Notebook
        {
            Id = Guid.NewGuid(),
            Name = "Full Notebook",
            CreatedAt = createdAt,
            LastModifiedAt = lastModified,
            SortOrder = 5
        };

        // Act
        await _repository.Add(notebook);

        // Assert
        var result = await _repository.GetById(notebook.Id);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Full Notebook");
        result.CreatedAt.ShouldBe(createdAt);
        result.LastModifiedAt.ShouldBe(lastModified);
        result.SortOrder.ShouldBe(5);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldUpdateExistingNotebook()
    {
        // Arrange
        var notebook = CreateNotebook("Original Name");
        await AddNotebook(notebook);
        var updatedNotebook = notebook with { Name = "Updated Name" };

        // Act
        await _repository.Update(updatedNotebook);

        // Assert
        var result = await _repository.GetById(notebook.Id);
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Updated Name");
    }

    [Fact]
    public async Task Update_ShouldUpdateLastModifiedAt()
    {
        // Arrange
        var originalTime = DateTimeOffset.UtcNow.AddDays(-1);
        var notebook = new Notebook
        {
            Id = Guid.NewGuid(),
            Name = "Test",
            CreatedAt = originalTime,
            LastModifiedAt = originalTime
        };
        await AddNotebook(notebook);

        var newTime = DateTimeOffset.UtcNow;
        var updatedNotebook = notebook with { LastModifiedAt = newTime };

        // Act
        await _repository.Update(updatedNotebook);

        // Assert
        var result = await _repository.GetById(notebook.Id);
        result.ShouldNotBeNull();
        result.LastModifiedAt.ShouldBe(newTime);
        result.CreatedAt.ShouldBe(originalTime);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WhenNotebookExists_ShouldRemoveNotebook()
    {
        // Arrange
        var notebook = CreateNotebook("To Delete");
        await AddNotebook(notebook);

        // Act
        await _repository.Delete(notebook.Id);

        // Assert
        var result = await _repository.GetById(notebook.Id);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task Delete_WhenNotebookDoesNotExist_ShouldNotThrow()
    {
        // Act & Assert
        await Should.NotThrowAsync(() => _repository.Delete(Guid.NewGuid()));
    }

    [Fact]
    public async Task Delete_ShouldOnlyDeleteSpecifiedNotebook()
    {
        // Arrange
        var notebook1 = CreateNotebook("Notebook 1");
        var notebook2 = CreateNotebook("Notebook 2");
        await AddNotebook(notebook1);
        await AddNotebook(notebook2);

        // Act
        await _repository.Delete(notebook1.Id);

        // Assert
        var result = await _repository.GetAll();
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(notebook2.Id);
    }

    #endregion

    private static Notebook CreateNotebook(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        CreatedAt = DateTimeOffset.UtcNow,
        LastModifiedAt = DateTimeOffset.UtcNow
    };

    private async Task AddNotebook(Notebook notebook)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Notebooks.Add(notebook);
        await context.SaveChangesAsync();
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
