using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Nouz.Domain.Entities;
using Nouz.Infrastructure.Repositories;
using Shouldly;

namespace Nouz.Infrastructure.Unit.Tests.Repositories;

public class BlockPersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IDbContextFactory<NouzDbContext> _contextFactory;
    private readonly Notebook _defaultNotebook;
    private readonly Note _defaultNote;

    public BlockPersistenceTests()
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

        // Create a default notebook and note for tests
        _defaultNotebook = CreateNotebook("Default Notebook");
        context.Notebooks.Add(_defaultNotebook);
        context.SaveChanges();

        _defaultNote = CreateNote(_defaultNotebook.Id);
        context.Notes.Add(_defaultNote);
        context.SaveChanges();
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    #region Block Creation Tests

    [Fact]
    public async Task AddBlock_ShouldPersistBlock()
    {
        // Arrange
        var block = CreateBlock(BlockType.Paragraph, "Test content");

        // Act
        await AddBlockToNote(block, _defaultNote.Id);

        // Assert
        var result = await GetBlockById(block.Id);
        result.ShouldNotBeNull();
        result.Id.ShouldBe(block.Id);
    }

    [Fact]
    public async Task AddBlock_ShouldPersistContent()
    {
        // Arrange
        var block = CreateBlock(BlockType.Paragraph, "This is the block content");

        // Act
        await AddBlockToNote(block, _defaultNote.Id);

        // Assert
        var result = await GetBlockById(block.Id);
        result.ShouldNotBeNull();
        result.Content.ShouldBe("This is the block content");
    }

    [Fact]
    public async Task AddBlock_ShouldPersistEmptyContent()
    {
        // Arrange
        var block = CreateBlock(BlockType.Divider, string.Empty);

        // Act
        await AddBlockToNote(block, _defaultNote.Id);

        // Assert
        var result = await GetBlockById(block.Id);
        result.ShouldNotBeNull();
        result.Content.ShouldBe(string.Empty);
    }

    #endregion

    #region BlockType Conversion Tests

    [Theory]
    [InlineData(BlockType.Paragraph)]
    [InlineData(BlockType.H1)]
    [InlineData(BlockType.H2)]
    [InlineData(BlockType.H3)]
    [InlineData(BlockType.H4)]
    [InlineData(BlockType.ListItem)]
    [InlineData(BlockType.TodoItem)]
    [InlineData(BlockType.Code)]
    [InlineData(BlockType.Quote)]
    [InlineData(BlockType.Divider)]
    public async Task AddBlock_ShouldPersistAllBlockTypes(BlockType blockType)
    {
        // Arrange
        var block = CreateBlock(blockType, "Content");

        // Act
        await AddBlockToNote(block, _defaultNote.Id);

        // Assert
        var result = await GetBlockById(block.Id);
        result.ShouldNotBeNull();
        result.Type.ShouldBe(blockType);
    }

    [Fact]
    public async Task BlockType_ShouldRoundTripCorrectly()
    {
        // Arrange - create blocks with different types
        var paragraphBlock = CreateBlock(BlockType.Paragraph, "P");
        var headingBlock = CreateBlock(BlockType.H1, "H");
        var codeBlock = CreateBlock(BlockType.Code, "C");

        await AddBlockToNote(paragraphBlock, _defaultNote.Id);
        await AddBlockToNote(headingBlock, _defaultNote.Id);
        await AddBlockToNote(codeBlock, _defaultNote.Id);

        // Act - retrieve and verify types are preserved
        var retrievedParagraph = await GetBlockById(paragraphBlock.Id);
        var retrievedHeading = await GetBlockById(headingBlock.Id);
        var retrievedCode = await GetBlockById(codeBlock.Id);

        // Assert
        retrievedParagraph!.Type.ShouldBe(BlockType.Paragraph);
        retrievedHeading!.Type.ShouldBe(BlockType.H1);
        retrievedCode!.Type.ShouldBe(BlockType.Code);
    }

    #endregion

    #region Metadata JSON Serialization Tests

    [Fact]
    public async Task AddBlock_ShouldPersistEmptyMetadata()
    {
        // Arrange
        var block = CreateBlock(BlockType.Paragraph, "Content");

        // Act
        await AddBlockToNote(block, _defaultNote.Id);

        // Assert
        var result = await GetBlockById(block.Id);
        result.ShouldNotBeNull();
        result.Metadata.ShouldNotBeNull();
        result.Metadata.ShouldBeEmpty();
    }

    [Fact]
    public async Task AddBlock_ShouldPersistMetadataWithStringValues()
    {
        // Arrange
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.Code,
            Content = "var x = 1;",
            Metadata = new Dictionary<string, object>
            {
                ["language"] = "csharp",
                ["filename"] = "example.cs"
            }
        };

        // Act
        await AddBlockToNote(block, _defaultNote.Id);

        // Assert
        var result = await GetBlockById(block.Id);
        result.ShouldNotBeNull();
        result.Metadata.ShouldContainKey("language");
        result.Metadata.ShouldContainKey("filename");
    }

    [Fact]
    public async Task AddBlock_ShouldPersistMetadataWithNumericValues()
    {
        // Arrange
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.ListItem,
            Content = "Item",
            Metadata = new Dictionary<string, object>
            {
                ["indent"] = 2,
                ["order"] = 1
            }
        };

        // Act
        await AddBlockToNote(block, _defaultNote.Id);

        // Assert
        var result = await GetBlockById(block.Id);
        result.ShouldNotBeNull();
        result.Metadata.ShouldContainKey("indent");
        result.Metadata.ShouldContainKey("order");
    }

    [Fact]
    public async Task AddBlock_ShouldPersistMetadataWithBooleanValues()
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
                ["important"] = false
            }
        };

        // Act
        await AddBlockToNote(block, _defaultNote.Id);

        // Assert
        var result = await GetBlockById(block.Id);
        result.ShouldNotBeNull();
        result.Metadata.ShouldContainKey("checked");
        result.Metadata.ShouldContainKey("important");
    }

    [Fact]
    public async Task TodoItem_CheckedState_ShouldPersistAsTrue()
    {
        // Arrange
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.TodoItem,
            Content = "Completed task",
            Metadata = new Dictionary<string, object> { ["checked"] = true }
        };

        // Act
        await AddBlockToNote(block, _defaultNote.Id);

        // Assert
        var result = await GetBlockById(block.Id);
        result.ShouldNotBeNull();
        var checkedValue = result.Metadata["checked"];
        // After JSON deserialization, value will be a JsonElement
        if (checkedValue is System.Text.Json.JsonElement jsonElement)
        {
            jsonElement.GetBoolean().ShouldBeTrue();
        }
        else
        {
            Convert.ToBoolean(checkedValue).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task TodoItem_CheckedState_ShouldPersistAsFalse()
    {
        // Arrange
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.TodoItem,
            Content = "Pending task",
            Metadata = new Dictionary<string, object> { ["checked"] = false }
        };

        // Act
        await AddBlockToNote(block, _defaultNote.Id);

        // Assert
        var result = await GetBlockById(block.Id);
        result.ShouldNotBeNull();
        var checkedValue = result.Metadata["checked"];
        if (checkedValue is System.Text.Json.JsonElement jsonElement)
        {
            jsonElement.GetBoolean().ShouldBeFalse();
        }
        else
        {
            Convert.ToBoolean(checkedValue).ShouldBeFalse();
        }
    }

    [Fact]
    public async Task TodoItem_CheckedState_ShouldUpdateFromFalseToTrue()
    {
        // Arrange
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.TodoItem,
            Content = "Task",
            Metadata = new Dictionary<string, object> { ["checked"] = false }
        };
        await AddBlockToNote(block, _defaultNote.Id);

        // Act - Update the checked state
        await UpdateBlockMetadata(block.Id, new Dictionary<string, object> { ["checked"] = true });

        // Assert
        var result = await GetBlockById(block.Id);
        result.ShouldNotBeNull();
        var checkedValue = result.Metadata["checked"];
        if (checkedValue is System.Text.Json.JsonElement jsonElement)
        {
            jsonElement.GetBoolean().ShouldBeTrue();
        }
        else
        {
            Convert.ToBoolean(checkedValue).ShouldBeTrue();
        }
    }

    [Fact]
    public async Task CodeBlock_ShouldPersistWithLanguageMetadata()
    {
        // Arrange
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.Code,
            Content = "console.log('Hello');",
            Metadata = new Dictionary<string, object> { ["language"] = "javascript" }
        };

        // Act
        await AddBlockToNote(block, _defaultNote.Id);

        // Assert
        var result = await GetBlockById(block.Id);
        result.ShouldNotBeNull();
        result.Type.ShouldBe(BlockType.Code);
        var languageValue = result.Metadata["language"];
        if (languageValue is System.Text.Json.JsonElement jsonElement)
        {
            jsonElement.GetString().ShouldBe("javascript");
        }
        else
        {
            languageValue.ToString().ShouldBe("javascript");
        }
    }

    [Fact]
    public async Task CodeBlock_ShouldPersistMultilineContent()
    {
        // Arrange
        var multilineCode = "function hello() {\n    console.log('Hello');\n    return true;\n}";
        var block = new Block
        {
            Id = Guid.NewGuid(),
            Type = BlockType.Code,
            Content = multilineCode
        };

        // Act
        await AddBlockToNote(block, _defaultNote.Id);

        // Assert
        var result = await GetBlockById(block.Id);
        result.ShouldNotBeNull();
        result.Content.ShouldBe(multilineCode);
        result.Content.ShouldContain("\n");
    }

    #endregion

    #region Block-Note Relationship Tests

    [Fact]
    public async Task AddBlock_ShouldAssociateWithNote()
    {
        // Arrange
        var block = CreateBlock(BlockType.Paragraph, "Content");

        // Act
        await AddBlockToNote(block, _defaultNote.Id);

        // Assert
        var blocks = await GetBlocksByNoteId(_defaultNote.Id);
        blocks.Count.ShouldBe(1);
        blocks[0].Id.ShouldBe(block.Id);
    }

    [Fact]
    public async Task GetBlocksByNoteId_ShouldReturnOnlyBlocksForSpecifiedNote()
    {
        // Arrange
        var note2 = CreateNote(_defaultNotebook.Id);
        await AddNote(note2);

        var block1 = CreateBlock(BlockType.Paragraph, "Block 1");
        var block2 = CreateBlock(BlockType.Paragraph, "Block 2");
        var block3 = CreateBlock(BlockType.Paragraph, "Block 3");

        await AddBlockToNote(block1, _defaultNote.Id);
        await AddBlockToNote(block2, _defaultNote.Id);
        await AddBlockToNote(block3, note2.Id);

        // Act
        var blocksInDefault = await GetBlocksByNoteId(_defaultNote.Id);
        var blocksInNote2 = await GetBlocksByNoteId(note2.Id);

        // Assert
        blocksInDefault.Count.ShouldBe(2);
        blocksInNote2.Count.ShouldBe(1);
        blocksInNote2[0].Id.ShouldBe(block3.Id);
    }

    [Fact]
    public async Task GetBlocksByNoteId_WhenNoBlocks_ShouldReturnEmptyList()
    {
        // Act
        var blocks = await GetBlocksByNoteId(_defaultNote.Id);

        // Assert
        blocks.ShouldBeEmpty();
    }

    #endregion

    #region Block Update Tests

    [Fact]
    public async Task UpdateBlock_ShouldUpdateContent()
    {
        // Arrange
        var block = CreateBlock(BlockType.Paragraph, "Original content");
        await AddBlockToNote(block, _defaultNote.Id);

        // Act
        await UpdateBlockContent(block.Id, "Updated content");

        // Assert
        var result = await GetBlockById(block.Id);
        result.ShouldNotBeNull();
        result.Content.ShouldBe("Updated content");
    }

    [Fact]
    public async Task UpdateBlock_ShouldUpdateType()
    {
        // Arrange
        var block = CreateBlock(BlockType.Paragraph, "Content");
        await AddBlockToNote(block, _defaultNote.Id);

        // Act
        await UpdateBlockType(block.Id, BlockType.H1);

        // Assert
        var result = await GetBlockById(block.Id);
        result.ShouldNotBeNull();
        result.Type.ShouldBe(BlockType.H1);
    }

    #endregion

    #region Block Delete Tests

    [Fact]
    public async Task DeleteBlock_ShouldRemoveBlock()
    {
        // Arrange
        var block = CreateBlock(BlockType.Paragraph, "Content");
        await AddBlockToNote(block, _defaultNote.Id);

        // Act
        await DeleteBlock(block.Id);

        // Assert
        var result = await GetBlockById(block.Id);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteBlock_ShouldOnlyDeleteSpecifiedBlock()
    {
        // Arrange
        var block1 = CreateBlock(BlockType.Paragraph, "Block 1");
        var block2 = CreateBlock(BlockType.Paragraph, "Block 2");
        await AddBlockToNote(block1, _defaultNote.Id);
        await AddBlockToNote(block2, _defaultNote.Id);

        // Act
        await DeleteBlock(block1.Id);

        // Assert
        var blocks = await GetBlocksByNoteId(_defaultNote.Id);
        blocks.Count.ShouldBe(1);
        blocks[0].Id.ShouldBe(block2.Id);
    }

    #endregion

    #region Multiple Blocks Tests

    [Fact]
    public async Task AddMultipleBlocks_ShouldPersistAll()
    {
        // Arrange
        var blocks = new[]
        {
            CreateBlock(BlockType.H1, "Heading"),
            CreateBlock(BlockType.Paragraph, "First paragraph"),
            CreateBlock(BlockType.Paragraph, "Second paragraph"),
            CreateBlock(BlockType.Code, "var x = 1;"),
            CreateBlock(BlockType.Quote, "A quote")
        };

        // Act
        foreach (var block in blocks)
        {
            await AddBlockToNote(block, _defaultNote.Id);
        }

        // Assert
        var result = await GetBlocksByNoteId(_defaultNote.Id);
        result.Count.ShouldBe(5);
    }

    #endregion

    private static Block CreateBlock(BlockType type, string content) => new()
    {
        Id = Guid.NewGuid(),
        Type = type,
        Content = content
    };

    private static Note CreateNote(Guid notebookId) => new()
    {
        Id = Guid.NewGuid(),
        NotebookId = notebookId,
        CreatedAt = DateTimeOffset.UtcNow,
        LastModifiedAt = DateTimeOffset.UtcNow
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

    private async Task AddBlockToNote(Block block, Guid noteId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Entry(block).Property("NoteId").CurrentValue = noteId;
        context.Set<Block>().Add(block);
        await context.SaveChangesAsync();
    }

    private async Task<Block?> GetBlockById(Guid blockId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<Block>()
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == blockId);
    }

    private async Task<List<Block>> GetBlocksByNoteId(Guid noteId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<Block>()
            .AsNoTracking()
            .Where(b => EF.Property<Guid>(b, "NoteId") == noteId)
            .ToListAsync();
    }

    private async Task UpdateBlockContent(Guid blockId, string newContent)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var block = await context.Set<Block>().FindAsync(blockId);
        if (block is not null)
        {
            context.Entry(block).CurrentValues.SetValues(block with { Content = newContent });
            await context.SaveChangesAsync();
        }
    }

    private async Task UpdateBlockType(Guid blockId, BlockType newType)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var block = await context.Set<Block>().FindAsync(blockId);
        if (block is not null)
        {
            context.Entry(block).CurrentValues.SetValues(block with { Type = newType });
            await context.SaveChangesAsync();
        }
    }

    private async Task UpdateBlockMetadata(Guid blockId, Dictionary<string, object> newMetadata)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var block = await context.Set<Block>().FindAsync(blockId);
        if (block is not null)
        {
            context.Entry(block).CurrentValues.SetValues(block with { Metadata = newMetadata });
            await context.SaveChangesAsync();
        }
    }

    private async Task DeleteBlock(Guid blockId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var block = await context.Set<Block>().FindAsync(blockId);
        if (block is not null)
        {
            context.Set<Block>().Remove(block);
            await context.SaveChangesAsync();
        }
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
