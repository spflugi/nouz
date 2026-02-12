using Nouz.Application.Chat;
using Nouz.Application.Embeddings;
using Nouz.Domain.Entities;
using Nouz.Domain.Repositories;
using Nouz.Infrastructure.Chat;
using NSubstitute;
using Shouldly;

namespace Nouz.Infrastructure.Unit.Tests.Chat;

public class NoteContextServiceTests
{
    private readonly IEmbeddingService _embeddingService = Substitute.For<IEmbeddingService>();
    private readonly IEmbeddingRepository _embeddingRepository = Substitute.For<IEmbeddingRepository>();
    private readonly INoteRepository _noteRepository = Substitute.For<INoteRepository>();
    private readonly NoteContextService _service;

    public NoteContextServiceTests()
    {
        _service = new NoteContextService(_embeddingService, _embeddingRepository, _noteRepository);
    }

    [Fact]
    public async Task GetRelevantNotesAsync_WhenQueryIsEmpty_ShouldReturnEmptyList()
    {
        // Act
        var result = await _service.GetRelevantNotesAsync("", 5, 0f, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
        await _embeddingService.DidNotReceive().GenerateEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRelevantNotesAsync_WhenQueryIsWhitespace_ShouldReturnEmptyList()
    {
        // Act
        var result = await _service.GetRelevantNotesAsync("   ", 5, 0f, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetRelevantNotesAsync_WhenTopNIsZero_ShouldReturnEmptyList()
    {
        // Act
        var result = await _service.GetRelevantNotesAsync("test query", 0, 0f, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetRelevantNotesAsync_WhenTopNIsNegative_ShouldReturnEmptyList()
    {
        // Act
        var result = await _service.GetRelevantNotesAsync("test query", -1, 0f, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetRelevantNotesAsync_WhenEmbeddingGenerationReturnsEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        _embeddingService.GenerateEmbeddingAsync("test query", Arg.Any<CancellationToken>())
            .Returns([]);

        // Act
        var result = await _service.GetRelevantNotesAsync("test query", 5, 0f, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetRelevantNotesAsync_WhenNoSimilarNotesFound_ShouldReturnEmptyList()
    {
        // Arrange
        var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        _embeddingService.GenerateEmbeddingAsync("test query", Arg.Any<CancellationToken>())
            .Returns(queryEmbedding);
        _embeddingRepository.FindSimilarAsync(queryEmbedding, 5, Arg.Any<CancellationToken>())
            .Returns(new List<(Guid, float)>());

        // Act
        var result = await _service.GetRelevantNotesAsync("test query", 5, 0f, TestContext.Current.CancellationToken);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetRelevantNotesAsync_WhenSimilarNotesFound_ShouldReturnNoteContextResults()
    {
        // Arrange
        var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        var noteId1 = Guid.NewGuid();
        var noteId2 = Guid.NewGuid();
        var note1 = CreateNoteWithTitle(noteId1, "First Note");
        var note2 = CreateNoteWithTitle(noteId2, "Second Note");

        _embeddingService.GenerateEmbeddingAsync("test query", Arg.Any<CancellationToken>())
            .Returns(queryEmbedding);
        _embeddingRepository.FindSimilarAsync(queryEmbedding, 5, Arg.Any<CancellationToken>())
            .Returns(new List<(Guid, float)> { (noteId1, 0.9f), (noteId2, 0.8f) });
        _noteRepository.GetById(noteId1, Arg.Any<CancellationToken>()).Returns(note1);
        _noteRepository.GetById(noteId2, Arg.Any<CancellationToken>()).Returns(note2);

        // Act
        var result = await _service.GetRelevantNotesAsync("test query", 5, 0f, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);
        result[0].Note.Id.ShouldBe(noteId1);
        result[0].Title.ShouldBe("First Note");
        result[0].Similarity.ShouldBe(0.9f);
        result[1].Note.Id.ShouldBe(noteId2);
        result[1].Title.ShouldBe("Second Note");
        result[1].Similarity.ShouldBe(0.8f);
    }

    [Fact]
    public async Task GetRelevantNotesAsync_WhenSomeNotesNotFound_ShouldReturnOnlyFoundNotes()
    {
        // Arrange
        var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        var noteId1 = Guid.NewGuid();
        var noteId2 = Guid.NewGuid();
        var note1 = CreateNoteWithTitle(noteId1, "Found Note");

        _embeddingService.GenerateEmbeddingAsync("test query", Arg.Any<CancellationToken>())
            .Returns(queryEmbedding);
        _embeddingRepository.FindSimilarAsync(queryEmbedding, 5, Arg.Any<CancellationToken>())
            .Returns(new List<(Guid, float)> { (noteId1, 0.9f), (noteId2, 0.8f) });
        _noteRepository.GetById(noteId1, Arg.Any<CancellationToken>()).Returns(note1);
        _noteRepository.GetById(noteId2, Arg.Any<CancellationToken>()).Returns((Note?)null);

        // Act
        var result = await _service.GetRelevantNotesAsync("test query", 5, 0f, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(1);
        result[0].Note.Id.ShouldBe(noteId1);
    }

    [Fact]
    public async Task GetRelevantNotesAsync_ShouldPassCorrectTopNToRepository()
    {
        // Arrange
        var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        _embeddingService.GenerateEmbeddingAsync("test query", Arg.Any<CancellationToken>())
            .Returns(queryEmbedding);
        _embeddingRepository.FindSimilarAsync(queryEmbedding, 3, Arg.Any<CancellationToken>())
            .Returns(new List<(Guid, float)>());

        // Act
        await _service.GetRelevantNotesAsync("test query", 3, 0f, TestContext.Current.CancellationToken);

        // Assert
        await _embeddingRepository.Received(1).FindSimilarAsync(queryEmbedding, 3, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRelevantNotesAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        _embeddingService.GenerateEmbeddingAsync("test", cts.Token)
            .Returns<float[]>(_ => throw new OperationCanceledException());

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() =>
            _service.GetRelevantNotesAsync("test", 5, 0f, cts.Token));
    }

    [Fact]
    public async Task GetRelevantNotesAsync_WhenBelowMinSimilarity_ShouldFilterOut()
    {
        // Arrange
        var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        var noteId1 = Guid.NewGuid();
        var noteId2 = Guid.NewGuid();
        var note1 = CreateNoteWithTitle(noteId1, "Relevant Note");
        var note2 = CreateNoteWithTitle(noteId2, "Irrelevant Note");

        _embeddingService.GenerateEmbeddingAsync("test query", Arg.Any<CancellationToken>())
            .Returns(queryEmbedding);
        _embeddingRepository.FindSimilarAsync(queryEmbedding, 5, Arg.Any<CancellationToken>())
            .Returns(new List<(Guid, float)> { (noteId1, 0.9f), (noteId2, 0.2f) });
        _noteRepository.GetById(noteId1, Arg.Any<CancellationToken>()).Returns(note1);
        _noteRepository.GetById(noteId2, Arg.Any<CancellationToken>()).Returns(note2);

        // Act
        var result = await _service.GetRelevantNotesAsync("test query", 5, 0.3f, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(1);
        result[0].Note.Id.ShouldBe(noteId1);
        result[0].Similarity.ShouldBe(0.9f);
    }

    [Fact]
    public async Task GetRelevantNotesAsync_WhenAboveMinSimilarity_ShouldIncludeWithCorrectTitles()
    {
        // Arrange
        var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        var noteId1 = Guid.NewGuid();
        var noteId2 = Guid.NewGuid();
        var note1 = CreateNoteWithTitle(noteId1, "Meeting Notes");
        var note2 = CreateNoteWithTitle(noteId2, "Project Plan");

        _embeddingService.GenerateEmbeddingAsync("test query", Arg.Any<CancellationToken>())
            .Returns(queryEmbedding);
        _embeddingRepository.FindSimilarAsync(queryEmbedding, 5, Arg.Any<CancellationToken>())
            .Returns(new List<(Guid, float)> { (noteId1, 0.8f), (noteId2, 0.5f) });
        _noteRepository.GetById(noteId1, Arg.Any<CancellationToken>()).Returns(note1);
        _noteRepository.GetById(noteId2, Arg.Any<CancellationToken>()).Returns(note2);

        // Act
        var result = await _service.GetRelevantNotesAsync("test query", 5, 0.3f, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(2);
        result[0].Title.ShouldBe("Meeting Notes");
        result[1].Title.ShouldBe("Project Plan");
    }

    [Fact]
    public async Task GetRelevantNotesAsync_WhenNoteHasNoBlocks_ShouldReturnUntitledTitle()
    {
        // Arrange
        var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        var noteId = Guid.NewGuid();
        var note = CreateNote(noteId);

        _embeddingService.GenerateEmbeddingAsync("test query", Arg.Any<CancellationToken>())
            .Returns(queryEmbedding);
        _embeddingRepository.FindSimilarAsync(queryEmbedding, 5, Arg.Any<CancellationToken>())
            .Returns(new List<(Guid, float)> { (noteId, 0.9f) });
        _noteRepository.GetById(noteId, Arg.Any<CancellationToken>()).Returns(note);

        // Act
        var result = await _service.GetRelevantNotesAsync("test query", 5, 0f, TestContext.Current.CancellationToken);

        // Assert
        result.Count.ShouldBe(1);
        result[0].Title.ShouldBe("(Untitled)");
    }

    private static Note CreateNote(Guid id) => new()
    {
        Id = id,
        NotebookId = Guid.NewGuid(),
        CreatedAt = DateTimeOffset.UtcNow,
        LastModifiedAt = DateTimeOffset.UtcNow
    };

    private static Note CreateNoteWithTitle(Guid id, string title) => new()
    {
        Id = id,
        NotebookId = Guid.NewGuid(),
        CreatedAt = DateTimeOffset.UtcNow,
        LastModifiedAt = DateTimeOffset.UtcNow,
        Blocks =
        [
            new Block
            {
                Id = Guid.NewGuid(),
                Type = BlockType.H1,
                Content = title,
                Order = 0
            }
        ]
    };
}
