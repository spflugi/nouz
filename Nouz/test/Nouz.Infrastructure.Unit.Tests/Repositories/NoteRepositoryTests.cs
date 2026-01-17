using System.Collections.Immutable;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Nouz.Domain.Entities;
using Nouz.Infrastructure.Repositories;
using Shouldly;

namespace Nouz.Infrastructure.Unit.Tests.Repositories;

public class NoteRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IDbContextFactory<NouzDbContext> _contextFactory;
    private readonly NoteRepository _repository;
    private readonly Notebook _defaultNotebook;

    public NoteRepositoryTests()
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

        // Replace the regular BlockSearch table with FTS5 virtual table and triggers
        SetupFts5(context);

        // Create a default notebook for tests (notes require a notebook)
        _defaultNotebook = CreateNotebook("Default Notebook");
        context.Notebooks.Add(_defaultNotebook);
        context.SaveChanges();

        _repository = new NoteRepository(_contextFactory);
    }

    private static void SetupFts5(NouzDbContext context)
    {
        // Drop the regular BlockSearch table created by EnsureCreated
        context.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS BlockSearch;");

        // Create FTS5 virtual table
        context.Database.ExecuteSqlRaw("""
            CREATE VIRTUAL TABLE BlockSearch USING fts5(
                BlockId,
                NoteId,
                Content
            );
            """);

        // Create triggers to sync Block table with FTS5 BlockSearch table
        context.Database.ExecuteSqlRaw("""
            CREATE TRIGGER blocks_ai AFTER INSERT ON Block
            BEGIN
                INSERT INTO BlockSearch (BlockId, NoteId, Content)
                VALUES (new.Id, new.NoteId, new.Content);
            END;
            """);

        context.Database.ExecuteSqlRaw("""
            CREATE TRIGGER blocks_au AFTER UPDATE ON Block
            BEGIN
                UPDATE BlockSearch
                SET Content = new.Content
                WHERE BlockId = new.Id;
            END;
            """);

        context.Database.ExecuteSqlRaw("""
            CREATE TRIGGER blocks_ad AFTER DELETE ON Block
            BEGIN
                DELETE FROM BlockSearch WHERE BlockId = old.Id;
            END;
            """);
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    #region GetAllByNotebook Tests

    [Fact]
    public async Task GetAllByNotebook_WhenNoNotes_ShouldReturnEmptyList()
    {
        // Act
        var result = await _repository.GetAllByNotebook(_defaultNotebook.Id);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllByNotebook_WhenNotesExist_ShouldReturnAllNotesForNotebook()
    {
        // Arrange
        var note1 = CreateNote(_defaultNotebook.Id);
        var note2 = CreateNote(_defaultNotebook.Id);
        await AddNote(note1);
        await AddNote(note2);

        // Act
        var result = await _repository.GetAllByNotebook(_defaultNotebook.Id);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(n => n.Id == note1.Id);
        result.ShouldContain(n => n.Id == note2.Id);
    }

    [Fact]
    public async Task GetAllByNotebook_ShouldOnlyReturnNotesForSpecifiedNotebook()
    {
        // Arrange
        var otherNotebook = CreateNotebook("Other Notebook");
        await AddNotebook(otherNotebook);

        var noteInDefault = CreateNote(_defaultNotebook.Id);
        var noteInOther = CreateNote(otherNotebook.Id);
        await AddNote(noteInDefault);
        await AddNote(noteInOther);

        // Act
        var result = await _repository.GetAllByNotebook(_defaultNotebook.Id);

        // Assert
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(noteInDefault.Id);
    }

    [Fact]
    public async Task GetAllByNotebook_WhenNotebookHasNoNotes_ShouldReturnEmptyList()
    {
        // Arrange
        var emptyNotebook = CreateNotebook("Empty Notebook");
        await AddNotebook(emptyNotebook);

        var noteInDefault = CreateNote(_defaultNotebook.Id);
        await AddNote(noteInDefault);

        // Act
        var result = await _repository.GetAllByNotebook(emptyNotebook.Id);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllByNotebook_ShouldRespectCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            _repository.GetAllByNotebook(_defaultNotebook.Id, cts.Token));
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_WhenNoteExists_ShouldReturnNote()
    {
        // Arrange
        var note = CreateNote(_defaultNotebook.Id);
        await AddNote(note);

        // Act
        var result = await _repository.GetById(note.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(note.Id);
        result.NotebookId.ShouldBe(_defaultNotebook.Id);
    }

    [Fact]
    public async Task GetById_WhenNoteDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetById(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetById_ShouldReturnAllProperties()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow.AddDays(-1);
        var lastModified = DateTimeOffset.UtcNow;
        var note = new Note
        {
            Id = Guid.NewGuid(),
            NotebookId = _defaultNotebook.Id,
            CreatedAt = createdAt,
            LastModifiedAt = lastModified
        };
        await AddNote(note);

        // Act
        var result = await _repository.GetById(note.Id);

        // Assert
        result.ShouldNotBeNull();
        result.CreatedAt.ShouldBe(createdAt);
        result.LastModifiedAt.ShouldBe(lastModified);
        result.NotebookId.ShouldBe(_defaultNotebook.Id);
    }

    #endregion

    #region Add Tests

    [Fact]
    public async Task Add_ShouldPersistNote()
    {
        // Arrange
        var note = CreateNote(_defaultNotebook.Id);

        // Act
        await _repository.Add(note);

        // Assert
        var result = await _repository.GetById(note.Id);
        result.ShouldNotBeNull();
        result.Id.ShouldBe(note.Id);
    }

    [Fact]
    public async Task Add_ShouldPersistAllProperties()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow.AddDays(-1);
        var lastModified = DateTimeOffset.UtcNow;
        var note = new Note
        {
            Id = Guid.NewGuid(),
            NotebookId = _defaultNotebook.Id,
            CreatedAt = createdAt,
            LastModifiedAt = lastModified
        };

        // Act
        await _repository.Add(note);

        // Assert
        var result = await _repository.GetById(note.Id);
        result.ShouldNotBeNull();
        result.NotebookId.ShouldBe(_defaultNotebook.Id);
        result.CreatedAt.ShouldBe(createdAt);
        result.LastModifiedAt.ShouldBe(lastModified);
    }

    [Fact]
    public async Task Add_ShouldRespectCancellationToken()
    {
        // Arrange
        var note = CreateNote(_defaultNotebook.Id);
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            _repository.Add(note, cts.Token));
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldUpdateExistingNote()
    {
        // Arrange
        var note = CreateNote(_defaultNotebook.Id);
        await AddNote(note);

        var newLastModified = DateTimeOffset.UtcNow.AddHours(1);
        var updatedNote = note with { LastModifiedAt = newLastModified };

        // Act
        await _repository.Update(updatedNote);

        // Assert
        var result = await _repository.GetById(note.Id);
        result.ShouldNotBeNull();
        result.LastModifiedAt.ShouldBe(newLastModified);
    }

    [Fact]
    public async Task Update_ShouldPreserveCreatedAt()
    {
        // Arrange
        var originalTime = DateTimeOffset.UtcNow.AddDays(-1);
        var note = new Note
        {
            Id = Guid.NewGuid(),
            NotebookId = _defaultNotebook.Id,
            CreatedAt = originalTime,
            LastModifiedAt = originalTime
        };
        await AddNote(note);

        var newTime = DateTimeOffset.UtcNow;
        var updatedNote = note with { LastModifiedAt = newTime };

        // Act
        await _repository.Update(updatedNote);

        // Assert
        var result = await _repository.GetById(note.Id);
        result.ShouldNotBeNull();
        result.LastModifiedAt.ShouldBe(newTime);
        result.CreatedAt.ShouldBe(originalTime);
    }

    [Fact]
    public async Task Update_ShouldAllowMovingToAnotherNotebook()
    {
        // Arrange
        var note = CreateNote(_defaultNotebook.Id);
        await AddNote(note);

        var otherNotebook = CreateNotebook("Other Notebook");
        await AddNotebook(otherNotebook);

        var updatedNote = note with { NotebookId = otherNotebook.Id };

        // Act
        await _repository.Update(updatedNote);

        // Assert
        var result = await _repository.GetById(note.Id);
        result.ShouldNotBeNull();
        result.NotebookId.ShouldBe(otherNotebook.Id);

        // Verify it's no longer in default notebook
        var notesInDefault = await _repository.GetAllByNotebook(_defaultNotebook.Id);
        notesInDefault.ShouldBeEmpty();

        // Verify it's in the other notebook
        var notesInOther = await _repository.GetAllByNotebook(otherNotebook.Id);
        notesInOther.Count.ShouldBe(1);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WhenNoteExists_ShouldRemoveNote()
    {
        // Arrange
        var note = CreateNote(_defaultNotebook.Id);
        await AddNote(note);

        // Act
        await _repository.Delete(note.Id);

        // Assert
        var result = await _repository.GetById(note.Id);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task Delete_WhenNoteDoesNotExist_ShouldNotThrow()
    {
        // Act & Assert
        await Should.NotThrowAsync(() => _repository.Delete(Guid.NewGuid()));
    }

    [Fact]
    public async Task Delete_ShouldOnlyDeleteSpecifiedNote()
    {
        // Arrange
        var note1 = CreateNote(_defaultNotebook.Id);
        var note2 = CreateNote(_defaultNotebook.Id);
        await AddNote(note1);
        await AddNote(note2);

        // Act
        await _repository.Delete(note1.Id);

        // Assert
        var result = await _repository.GetAllByNotebook(_defaultNotebook.Id);
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(note2.Id);
    }

    [Fact]
    public async Task Delete_ShouldRespectCancellationToken()
    {
        // Arrange
        var note = CreateNote(_defaultNotebook.Id);
        await AddNote(note);
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            _repository.Delete(note.Id, cts.Token));
    }

    #endregion

    #region Cascade Delete Tests

    [Fact]
    public async Task Delete_WhenNotebookIsDeleted_ShouldCascadeDeleteNotes()
    {
        // Arrange
        var note1 = CreateNote(_defaultNotebook.Id);
        var note2 = CreateNote(_defaultNotebook.Id);
        await AddNote(note1);
        await AddNote(note2);

        // Act - Delete the notebook
        await using var context = await _contextFactory.CreateDbContextAsync();
        var notebook = await context.Notebooks.FindAsync(_defaultNotebook.Id);
        context.Notebooks.Remove(notebook!);
        await context.SaveChangesAsync();

        // Assert - Notes should be deleted too
        var remainingNotes = await _repository.GetAllByNotebook(_defaultNotebook.Id);
        remainingNotes.ShouldBeEmpty();
    }

    #endregion

    #region Notes With Blocks Tests

    [Fact]
    public async Task GetById_ShouldReturnNoteWithBlocks()
    {
        // Arrange
        var block1 = CreateBlock(BlockType.H1, "Heading");
        var block2 = CreateBlock(BlockType.Paragraph, "Content");
        var note = CreateNoteWithBlocks(_defaultNotebook.Id, [block1, block2]);
        await _repository.Add(note);

        // Act
        var result = await _repository.GetById(note.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Blocks.Count.ShouldBe(2);
        result.Blocks.ShouldContain(b => b.Id == block1.Id && b.Type == BlockType.H1);
        result.Blocks.ShouldContain(b => b.Id == block2.Id && b.Type == BlockType.Paragraph);
    }

    [Fact]
    public async Task GetById_WhenNoteHasNoBlocks_ShouldReturnEmptyBlocksList()
    {
        // Arrange
        var note = CreateNote(_defaultNotebook.Id);
        await _repository.Add(note);

        // Act
        var result = await _repository.GetById(note.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Blocks.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllByNotebook_ShouldReturnNotesWithBlocks()
    {
        // Arrange
        var block1 = CreateBlock(BlockType.H1, "Heading 1");
        var block2 = CreateBlock(BlockType.Paragraph, "Content 1");
        var note1 = CreateNoteWithBlocks(_defaultNotebook.Id, [block1, block2]);

        var block3 = CreateBlock(BlockType.Code, "var x = 1;");
        var note2 = CreateNoteWithBlocks(_defaultNotebook.Id, [block3]);

        await _repository.Add(note1);
        await _repository.Add(note2);

        // Act
        var result = await _repository.GetAllByNotebook(_defaultNotebook.Id);

        // Assert
        result.Count.ShouldBe(2);
        var retrievedNote1 = result.First(n => n.Id == note1.Id);
        var retrievedNote2 = result.First(n => n.Id == note2.Id);

        retrievedNote1.Blocks.Count.ShouldBe(2);
        retrievedNote2.Blocks.Count.ShouldBe(1);
        retrievedNote2.Blocks[0].Type.ShouldBe(BlockType.Code);
    }

    [Fact]
    public async Task Add_ShouldPersistNoteWithBlocks()
    {
        // Arrange
        var block1 = CreateBlock(BlockType.H1, "Title");
        var block2 = CreateBlock(BlockType.Paragraph, "First paragraph");
        var block3 = CreateBlock(BlockType.Paragraph, "Second paragraph");
        var note = CreateNoteWithBlocks(_defaultNotebook.Id, [block1, block2, block3]);

        // Act
        await _repository.Add(note);

        // Assert
        var result = await _repository.GetById(note.Id);
        result.ShouldNotBeNull();
        result.Blocks.Count.ShouldBe(3);
        result.Blocks.ShouldContain(b => b.Content == "Title");
        result.Blocks.ShouldContain(b => b.Content == "First paragraph");
        result.Blocks.ShouldContain(b => b.Content == "Second paragraph");
    }

    [Fact]
    public async Task Add_ShouldPersistBlockMetadata()
    {
        // Arrange
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.TodoItem,
            Content = "Task to complete",
            Metadata = new Dictionary<string, object>
            {
                ["checked"] = true,
                ["priority"] = "high"
            }
        };
        var note = CreateNoteWithBlocks(_defaultNotebook.Id, [block]);

        // Act
        await _repository.Add(note);

        // Assert
        var result = await _repository.GetById(note.Id);
        result.ShouldNotBeNull();
        result.Blocks.Count.ShouldBe(1);
        result.Blocks[0].Metadata.ShouldContainKey("checked");
        result.Blocks[0].Metadata.ShouldContainKey("priority");
    }

    [Fact]
    public async Task Update_ShouldAddNewBlocks()
    {
        // Arrange
        var block1 = CreateBlock(BlockType.H1, "Title");
        var note = CreateNoteWithBlocks(_defaultNotebook.Id, [block1]);
        await _repository.Add(note);

        var block2 = CreateBlock(BlockType.Paragraph, "New paragraph");
        var updatedNote = note with { Blocks = [block1, block2] };

        // Act
        await _repository.Update(updatedNote);

        // Assert
        var result = await _repository.GetById(note.Id);
        result.ShouldNotBeNull();
        result.Blocks.Count.ShouldBe(2);
        result.Blocks.ShouldContain(b => b.Id == block1.Id);
        result.Blocks.ShouldContain(b => b.Id == block2.Id);
    }

    [Fact]
    public async Task Update_ShouldUpdateExistingBlocks()
    {
        // Arrange
        var block = CreateBlock(BlockType.Paragraph, "Original content");
        var note = CreateNoteWithBlocks(_defaultNotebook.Id, [block]);
        await _repository.Add(note);

        var updatedBlock = block with { Content = "Updated content" };
        var updatedNote = note with { Blocks = [updatedBlock] };

        // Act
        await _repository.Update(updatedNote);

        // Assert
        var result = await _repository.GetById(note.Id);
        result.ShouldNotBeNull();
        result.Blocks.Count.ShouldBe(1);
        result.Blocks[0].Content.ShouldBe("Updated content");
    }

    [Fact]
    public async Task Update_ShouldRemoveDeletedBlocks()
    {
        // Arrange
        var block1 = CreateBlock(BlockType.H1, "Title");
        var block2 = CreateBlock(BlockType.Paragraph, "Content to remove");
        var note = CreateNoteWithBlocks(_defaultNotebook.Id, [block1, block2]);
        await _repository.Add(note);

        var updatedNote = note with { Blocks = [block1] }; // Remove block2

        // Act
        await _repository.Update(updatedNote);

        // Assert
        var result = await _repository.GetById(note.Id);
        result.ShouldNotBeNull();
        result.Blocks.Count.ShouldBe(1);
        result.Blocks[0].Id.ShouldBe(block1.Id);
    }

    [Fact]
    public async Task Update_ShouldHandleComplexBlockChanges()
    {
        // Arrange - Start with 3 blocks
        var block1 = CreateBlock(BlockType.H1, "Title");
        var block2 = CreateBlock(BlockType.Paragraph, "Para 1");
        var block3 = CreateBlock(BlockType.Paragraph, "Para 2");
        var note = CreateNoteWithBlocks(_defaultNotebook.Id, [block1, block2, block3]);
        await _repository.Add(note);

        // Update: keep block1, modify block2, remove block3, add block4
        var updatedBlock2 = block2 with { Content = "Modified Para 1" };
        var block4 = CreateBlock(BlockType.Code, "new code");
        var updatedNote = note with { Blocks = [block1, updatedBlock2, block4] };

        // Act
        await _repository.Update(updatedNote);

        // Assert
        var result = await _repository.GetById(note.Id);
        result.ShouldNotBeNull();
        result.Blocks.Count.ShouldBe(3);
        result.Blocks.ShouldContain(b => b.Id == block1.Id && b.Content == "Title");
        result.Blocks.ShouldContain(b => b.Id == block2.Id && b.Content == "Modified Para 1");
        result.Blocks.ShouldContain(b => b.Id == block4.Id && b.Type == BlockType.Code);
        result.Blocks.ShouldNotContain(b => b.Id == block3.Id);
    }

    [Fact]
    public async Task Delete_ShouldCascadeDeleteBlocks()
    {
        // Arrange
        var block1 = CreateBlock(BlockType.H1, "Title");
        var block2 = CreateBlock(BlockType.Paragraph, "Content");
        var note = CreateNoteWithBlocks(_defaultNotebook.Id, [block1, block2]);
        await _repository.Add(note);

        // Act
        await _repository.Delete(note.Id);

        // Assert - Verify blocks are also deleted
        await using var context = await _contextFactory.CreateDbContextAsync();
        var remainingBlocks = await context.Set<Block>()
            .Where(b => EF.Property<Guid>(b, "NoteId") == note.Id)
            .ToListAsync();
        remainingBlocks.ShouldBeEmpty();
    }

    #endregion

    #region MoveToNotebook Tests

    [Fact]
    public async Task MoveToNotebook_ShouldChangeNotebookId()
    {
        // Arrange
        var note = CreateNote(_defaultNotebook.Id);
        await _repository.Add(note);

        var targetNotebook = CreateNotebook("Target Notebook");
        await AddNotebook(targetNotebook);

        // Act
        await _repository.MoveToNotebook(note.Id, targetNotebook.Id);

        // Assert
        var result = await _repository.GetById(note.Id);
        result.ShouldNotBeNull();
        result.NotebookId.ShouldBe(targetNotebook.Id);
    }

    [Fact]
    public async Task MoveToNotebook_ShouldUpdateLastModifiedAt()
    {
        // Arrange
        var originalTime = DateTimeOffset.UtcNow.AddDays(-1);
        var note = new Note
        {
            Id = Guid.NewGuid(),
            NotebookId = _defaultNotebook.Id,
            CreatedAt = originalTime,
            LastModifiedAt = originalTime
        };
        await _repository.Add(note);

        var targetNotebook = CreateNotebook("Target Notebook");
        await AddNotebook(targetNotebook);

        var beforeMove = DateTimeOffset.UtcNow;

        // Act
        await _repository.MoveToNotebook(note.Id, targetNotebook.Id);

        // Assert
        var result = await _repository.GetById(note.Id);
        result.ShouldNotBeNull();
        result.LastModifiedAt.ShouldBeGreaterThanOrEqualTo(beforeMove);
        result.CreatedAt.ShouldBe(originalTime);
    }

    [Fact]
    public async Task MoveToNotebook_ShouldPreserveBlocks()
    {
        // Arrange
        var block1 = CreateBlock(BlockType.H1, "Title");
        var block2 = CreateBlock(BlockType.Paragraph, "Content");
        var note = CreateNoteWithBlocks(_defaultNotebook.Id, [block1, block2]);
        await _repository.Add(note);

        var targetNotebook = CreateNotebook("Target Notebook");
        await AddNotebook(targetNotebook);

        // Act
        await _repository.MoveToNotebook(note.Id, targetNotebook.Id);

        // Assert
        var result = await _repository.GetById(note.Id);
        result.ShouldNotBeNull();
        result.Blocks.Count.ShouldBe(2);
        result.Blocks.ShouldContain(b => b.Id == block1.Id);
        result.Blocks.ShouldContain(b => b.Id == block2.Id);
    }

    [Fact]
    public async Task MoveToNotebook_ShouldRemoveNoteFromOriginalNotebook()
    {
        // Arrange
        var note = CreateNote(_defaultNotebook.Id);
        await _repository.Add(note);

        var targetNotebook = CreateNotebook("Target Notebook");
        await AddNotebook(targetNotebook);

        // Act
        await _repository.MoveToNotebook(note.Id, targetNotebook.Id);

        // Assert
        var notesInOriginal = await _repository.GetAllByNotebook(_defaultNotebook.Id);
        notesInOriginal.ShouldBeEmpty();

        var notesInTarget = await _repository.GetAllByNotebook(targetNotebook.Id);
        notesInTarget.Count.ShouldBe(1);
        notesInTarget[0].Id.ShouldBe(note.Id);
    }

    [Fact]
    public async Task MoveToNotebook_WhenNoteDoesNotExist_ShouldNotThrow()
    {
        // Arrange
        var targetNotebook = CreateNotebook("Target Notebook");
        await AddNotebook(targetNotebook);

        // Act & Assert
        await Should.NotThrowAsync(() =>
            _repository.MoveToNotebook(Guid.NewGuid(), targetNotebook.Id));
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task SearchInNotebook_WhenQueryMatchesBlockContent_ShouldReturnNote()
    {
        // Arrange
        var block = CreateBlock(BlockType.Paragraph, "This is a unique searchable content");
        var note = CreateNoteWithBlocks(_defaultNotebook.Id, [block]);
        await _repository.Add(note);

        // Act
        var results = await _repository.SearchInNotebook(_defaultNotebook.Id, "unique");

        // Assert
        results.Count.ShouldBe(1);
        results[0].Id.ShouldBe(note.Id);
    }

    [Fact]
    public async Task SearchInNotebook_ShouldReturnNotesWithAllBlocks()
    {
        // Arrange
        var block1 = CreateBlock(BlockType.H1, "Searchable title");
        var block2 = CreateBlock(BlockType.Paragraph, "Some other content");
        var note = CreateNoteWithBlocks(_defaultNotebook.Id, [block1, block2]);
        await _repository.Add(note);

        // Act
        var results = await _repository.SearchInNotebook(_defaultNotebook.Id, "Searchable");

        // Assert
        results.Count.ShouldBe(1);
        results[0].Blocks.Count.ShouldBe(2);
    }

    [Fact]
    public async Task SearchInNotebook_WhenNoMatch_ShouldReturnEmptyList()
    {
        // Arrange
        var block = CreateBlock(BlockType.Paragraph, "Some content");
        var note = CreateNoteWithBlocks(_defaultNotebook.Id, [block]);
        await _repository.Add(note);

        // Act
        var results = await _repository.SearchInNotebook(_defaultNotebook.Id, "nonexistent");

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchInNotebook_WhenQueryIsEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        var block = CreateBlock(BlockType.Paragraph, "Some content");
        var note = CreateNoteWithBlocks(_defaultNotebook.Id, [block]);
        await _repository.Add(note);

        // Act
        var results = await _repository.SearchInNotebook(_defaultNotebook.Id, "");

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchInNotebook_WhenQueryIsWhitespace_ShouldReturnEmptyList()
    {
        // Arrange
        var block = CreateBlock(BlockType.Paragraph, "Some content");
        var note = CreateNoteWithBlocks(_defaultNotebook.Id, [block]);
        await _repository.Add(note);

        // Act
        var results = await _repository.SearchInNotebook(_defaultNotebook.Id, "   ");

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchInNotebook_ShouldReturnMultipleMatchingNotes()
    {
        // Arrange
        var block1 = CreateBlock(BlockType.Paragraph, "First note with keyword");
        var note1 = CreateNoteWithBlocks(_defaultNotebook.Id, [block1]);

        var block2 = CreateBlock(BlockType.Paragraph, "Second note with keyword");
        var note2 = CreateNoteWithBlocks(_defaultNotebook.Id, [block2]);

        var block3 = CreateBlock(BlockType.Paragraph, "Third note without match");
        var note3 = CreateNoteWithBlocks(_defaultNotebook.Id, [block3]);

        await _repository.Add(note1);
        await _repository.Add(note2);
        await _repository.Add(note3);

        // Act
        var results = await _repository.SearchInNotebook(_defaultNotebook.Id, "keyword");

        // Assert
        results.Count.ShouldBe(2);
        results.ShouldContain(n => n.Id == note1.Id);
        results.ShouldContain(n => n.Id == note2.Id);
        results.ShouldNotContain(n => n.Id == note3.Id);
    }

    [Fact]
    public async Task SearchInNotebook_ShouldMatchAnyBlockInNote()
    {
        // Arrange
        var block1 = CreateBlock(BlockType.H1, "Title without match");
        var block2 = CreateBlock(BlockType.Paragraph, "Paragraph with searchterm");
        var block3 = CreateBlock(BlockType.Code, "code without match");
        var note = CreateNoteWithBlocks(_defaultNotebook.Id, [block1, block2, block3]);
        await _repository.Add(note);

        // Act
        var results = await _repository.SearchInNotebook(_defaultNotebook.Id, "searchterm");

        // Assert
        results.Count.ShouldBe(1);
        results[0].Id.ShouldBe(note.Id);
    }

    [Fact]
    public async Task SearchInNotebook_ShouldOnlyReturnNotesFromSpecifiedNotebook()
    {
        // Arrange
        var notebook2 = CreateNotebook("Second Notebook");
        await AddNotebook(notebook2);

        var block1 = CreateBlock(BlockType.Paragraph, "Content with searchword in first");
        var note1 = CreateNoteWithBlocks(_defaultNotebook.Id, [block1]);

        var block2 = CreateBlock(BlockType.Paragraph, "Content with searchword in second");
        var note2 = CreateNoteWithBlocks(notebook2.Id, [block2]);

        await _repository.Add(note1);
        await _repository.Add(note2);

        // Act
        var results = await _repository.SearchInNotebook(_defaultNotebook.Id, "searchword");

        // Assert
        results.Count.ShouldBe(1);
        results.ShouldContain(n => n.NotebookId == _defaultNotebook.Id);
        results.ShouldNotContain(n => n.NotebookId == notebook2.Id);
    }

    [Fact]
    public async Task SearchInNotebook_ShouldReturnNoteOnlyOnceEvenWithMultipleMatchingBlocks()
    {
        // Arrange
        var block1 = CreateBlock(BlockType.H1, "Title with keyword");
        var block2 = CreateBlock(BlockType.Paragraph, "Paragraph also with keyword");
        var block3 = CreateBlock(BlockType.Quote, "Quote with keyword too");
        var note = CreateNoteWithBlocks(_defaultNotebook.Id, [block1, block2, block3]);
        await _repository.Add(note);

        // Act
        var results = await _repository.SearchInNotebook(_defaultNotebook.Id, "keyword");

        // Assert
        results.Count.ShouldBe(1);
        results[0].Id.ShouldBe(note.Id);
    }

    #endregion

    private static Note CreateNote(Guid notebookId) => new()
    {
        Id = Guid.NewGuid(),
        NotebookId = notebookId,
        CreatedAt = DateTimeOffset.UtcNow,
        LastModifiedAt = DateTimeOffset.UtcNow
    };

    private static Note CreateNoteWithBlocks(Guid notebookId, ImmutableList<Block> blocks) => new()
    {
        Id = Guid.NewGuid(),
        NotebookId = notebookId,
        CreatedAt = DateTimeOffset.UtcNow,
        LastModifiedAt = DateTimeOffset.UtcNow,
        Blocks = blocks
    };

    private static Block CreateBlock(BlockType type, string content) => new()
    {
        Id = Guid.NewGuid(),
        Type = type,
        Content = content
    };

    private static Notebook CreateNotebook(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        CreatedAt = DateTimeOffset.UtcNow,
        LastModifiedAt = DateTimeOffset.UtcNow
    };

    private async Task AddNote(Note note)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Notes.Add(note);
        await context.SaveChangesAsync();
    }

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
