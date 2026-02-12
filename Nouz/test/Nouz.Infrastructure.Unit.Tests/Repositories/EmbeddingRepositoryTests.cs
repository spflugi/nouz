using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Nouz.Domain.Entities;
using Nouz.Infrastructure.Repositories;
using Shouldly;

namespace Nouz.Infrastructure.Unit.Tests.Repositories;

public class EmbeddingRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IDbContextFactory<NouzDbContext> _contextFactory;
    private readonly EmbeddingRepository _repository;
    private readonly Notebook _defaultNotebook;
    private readonly Note _defaultNote;

    public EmbeddingRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<NouzDbContext>()
            .UseSqlite(_connection)
            .Options;

        _contextFactory = new TestDbContextFactory(options);

        using var context = _contextFactory.CreateDbContext();
        context.Database.EnsureCreated();

        // Create default notebook and note for tests
        _defaultNotebook = CreateNotebook("Default Notebook");
        context.Notebooks.Add(_defaultNotebook);

        _defaultNote = CreateNote(_defaultNotebook.Id);
        context.Notes.Add(_defaultNote);
        context.SaveChanges();

        _repository = new EmbeddingRepository(_contextFactory);
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    #region GetByNoteIdAsync Tests

    [Fact]
    public async Task GetByNoteIdAsync_WhenEmbeddingExists_ShouldReturnEmbedding()
    {
        // Arrange
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };
        await _repository.UpsertAsync(_defaultNote.Id, embedding, TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByNoteIdAsync(_defaultNote.Id, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldNotBeNull();
        result.NoteId.ShouldBe(_defaultNote.Id);
        result.Embedding.ShouldBe(embedding);
    }

    [Fact]
    public async Task GetByNoteIdAsync_WhenEmbeddingDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByNoteIdAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region UpsertAsync Tests

    [Fact]
    public async Task UpsertAsync_WhenEmbeddingDoesNotExist_ShouldInsert()
    {
        // Arrange
        var embedding = new float[] { 0.5f, 0.6f, 0.7f };

        // Act
        await _repository.UpsertAsync(_defaultNote.Id, embedding, TestContext.Current.CancellationToken);

        // Assert
        var result = await _repository.GetByNoteIdAsync(_defaultNote.Id, TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Embedding.ShouldBe(embedding);
    }

    [Fact]
    public async Task UpsertAsync_WhenEmbeddingExists_ShouldUpdate()
    {
        // Arrange
        var initialEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        await _repository.UpsertAsync(_defaultNote.Id, initialEmbedding, TestContext.Current.CancellationToken);

        var updatedEmbedding = new float[] { 0.9f, 0.8f, 0.7f };

        // Act
        await _repository.UpsertAsync(_defaultNote.Id, updatedEmbedding, TestContext.Current.CancellationToken);

        // Assert
        var result = await _repository.GetByNoteIdAsync(_defaultNote.Id, TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.Embedding.ShouldBe(updatedEmbedding);
    }

    [Fact]
    public async Task UpsertAsync_ShouldUpdateLastUpdatedAt()
    {
        // Arrange
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };
        var beforeInsert = DateTimeOffset.UtcNow;

        // Act
        await _repository.UpsertAsync(_defaultNote.Id, embedding, TestContext.Current.CancellationToken);

        // Assert
        var result = await _repository.GetByNoteIdAsync(_defaultNote.Id, TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.LastUpdatedAt.ShouldBeGreaterThanOrEqualTo(beforeInsert);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenEmbeddingExists_ShouldDelete()
    {
        // Arrange
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };
        await _repository.UpsertAsync(_defaultNote.Id, embedding, TestContext.Current.CancellationToken);

        // Act
        await _repository.DeleteAsync(_defaultNote.Id, TestContext.Current.CancellationToken);

        // Assert
        var result = await _repository.GetByNoteIdAsync(_defaultNote.Id, TestContext.Current.CancellationToken);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenEmbeddingDoesNotExist_ShouldNotThrow()
    {
        // Act & Assert
        await Should.NotThrowAsync(() => _repository.DeleteAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));
    }

    #endregion

    #region FindSimilarAsync Tests

    [Fact]
    public async Task FindSimilarAsync_WhenNoEmbeddings_ShouldReturnEmptyList()
    {
        // Arrange
        var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };

        // Act
        var result = await _repository.FindSimilarAsync(queryEmbedding, 5, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task FindSimilarAsync_ShouldReturnSimilarNotes()
    {
        // Arrange
        var note1 = CreateNote(_defaultNotebook.Id);
        var note2 = CreateNote(_defaultNotebook.Id);
        await AddNote(note1);
        await AddNote(note2);

        // Insert embeddings
        var embedding1 = new float[] { 1.0f, 0.0f, 0.0f };
        var embedding2 = new float[] { 0.9f, 0.1f, 0.0f };
        await _repository.UpsertAsync(note1.Id, embedding1, TestContext.Current.CancellationToken);
        await _repository.UpsertAsync(note2.Id, embedding2, TestContext.Current.CancellationToken);

        var queryEmbedding = new float[] { 1.0f, 0.0f, 0.0f };

        // Act
        var result = await _repository.FindSimilarAsync(queryEmbedding, 5, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(r => r.NoteId == note1.Id);
        result.ShouldContain(r => r.NoteId == note2.Id);
    }

    [Fact]
    public async Task FindSimilarAsync_ShouldOrderBySimilarityDescending()
    {
        // Arrange
        var note1 = CreateNote(_defaultNotebook.Id);
        var note2 = CreateNote(_defaultNotebook.Id);
        await AddNote(note1);
        await AddNote(note2);

        // Note1 is more similar to query
        var embedding1 = new float[] { 1.0f, 0.0f, 0.0f };
        var embedding2 = new float[] { 0.5f, 0.5f, 0.0f };
        await _repository.UpsertAsync(note1.Id, embedding1, TestContext.Current.CancellationToken);
        await _repository.UpsertAsync(note2.Id, embedding2, TestContext.Current.CancellationToken);

        var queryEmbedding = new float[] { 1.0f, 0.0f, 0.0f };

        // Act
        var result = await _repository.FindSimilarAsync(queryEmbedding, 5, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);
        result[0].NoteId.ShouldBe(note1.Id);
        result[0].Similarity.ShouldBeGreaterThan(result[1].Similarity);
    }

    [Fact]
    public async Task FindSimilarAsync_ShouldRespectTopN()
    {
        // Arrange
        var note1 = CreateNote(_defaultNotebook.Id);
        var note2 = CreateNote(_defaultNotebook.Id);
        var note3 = CreateNote(_defaultNotebook.Id);
        await AddNote(note1);
        await AddNote(note2);
        await AddNote(note3);

        await _repository.UpsertAsync(note1.Id, new float[] { 1.0f, 0.0f, 0.0f }, TestContext.Current.CancellationToken);
        await _repository.UpsertAsync(note2.Id, new float[] { 0.9f, 0.1f, 0.0f }, TestContext.Current.CancellationToken);
        await _repository.UpsertAsync(note3.Id, new float[] { 0.8f, 0.2f, 0.0f }, TestContext.Current.CancellationToken);

        var queryEmbedding = new float[] { 1.0f, 0.0f, 0.0f };

        // Act
        var result = await _repository.FindSimilarAsync(queryEmbedding, 2, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task FindSimilarAsync_ShouldHandleDifferentVectorLengths()
    {
        // Arrange - embeddings of different length than query should work
        var note1 = CreateNote(_defaultNotebook.Id);
        await AddNote(note1);

        var embedding = new float[] { 0.5f, 0.5f, 0.5f, 0.5f };
        await _repository.UpsertAsync(note1.Id, embedding, TestContext.Current.CancellationToken);

        var queryEmbedding = new float[] { 0.5f, 0.5f, 0.5f, 0.5f };

        // Act
        var result = await _repository.FindSimilarAsync(queryEmbedding, 5, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(1);
    }

    #endregion

    #region Cascade Delete Tests

    [Fact]
    public async Task WhenNoteIsDeleted_EmbeddingShouldBeCascadeDeleted()
    {
        // Arrange
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };
        await _repository.UpsertAsync(_defaultNote.Id, embedding, TestContext.Current.CancellationToken);

        // Act - Delete the note
        await using var context = await _contextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var note = await context.Notes.FindAsync([_defaultNote.Id], cancellationToken: TestContext.Current.CancellationToken);
        context.Notes.Remove(note!);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert - Embedding should be deleted too
        var result = await _repository.GetByNoteIdAsync(_defaultNote.Id, TestContext.Current.CancellationToken);
        result.ShouldBeNull();
    }

    #endregion

    private static Notebook CreateNotebook(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        CreatedAt = DateTimeOffset.UtcNow,
        LastModifiedAt = DateTimeOffset.UtcNow
    };

    private static Note CreateNote(Guid notebookId) => new()
    {
        Id = Guid.NewGuid(),
        NotebookId = notebookId,
        CreatedAt = DateTimeOffset.UtcNow,
        LastModifiedAt = DateTimeOffset.UtcNow
    };

    private async Task AddNote(Note note)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        context.Notes.Add(note);
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
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
