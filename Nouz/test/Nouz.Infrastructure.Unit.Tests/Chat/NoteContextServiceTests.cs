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
        var result = await _service.GetRelevantNotesAsync("", 5);

        // Assert
        result.ShouldBeEmpty();
        await _embeddingService.DidNotReceive().GenerateEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetRelevantNotesAsync_WhenQueryIsWhitespace_ShouldReturnEmptyList()
    {
        // Act
        var result = await _service.GetRelevantNotesAsync("   ", 5);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetRelevantNotesAsync_WhenTopNIsZero_ShouldReturnEmptyList()
    {
        // Act
        var result = await _service.GetRelevantNotesAsync("test query", 0);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetRelevantNotesAsync_WhenTopNIsNegative_ShouldReturnEmptyList()
    {
        // Act
        var result = await _service.GetRelevantNotesAsync("test query", -1);

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
        var result = await _service.GetRelevantNotesAsync("test query", 5);

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
        var result = await _service.GetRelevantNotesAsync("test query", 5);

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetRelevantNotesAsync_WhenSimilarNotesFound_ShouldReturnNotes()
    {
        // Arrange
        var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        var noteId1 = Guid.NewGuid();
        var noteId2 = Guid.NewGuid();
        var note1 = CreateNote(noteId1);
        var note2 = CreateNote(noteId2);

        _embeddingService.GenerateEmbeddingAsync("test query", Arg.Any<CancellationToken>())
            .Returns(queryEmbedding);
        _embeddingRepository.FindSimilarAsync(queryEmbedding, 5, Arg.Any<CancellationToken>())
            .Returns(new List<(Guid, float)> { (noteId1, 0.9f), (noteId2, 0.8f) });
        _noteRepository.GetById(noteId1, Arg.Any<CancellationToken>()).Returns(note1);
        _noteRepository.GetById(noteId2, Arg.Any<CancellationToken>()).Returns(note2);

        // Act
        var result = await _service.GetRelevantNotesAsync("test query", 5);

        // Assert
        result.Count.ShouldBe(2);
        result.ShouldContain(n => n.Id == noteId1);
        result.ShouldContain(n => n.Id == noteId2);
    }

    [Fact]
    public async Task GetRelevantNotesAsync_WhenSomeNotesNotFound_ShouldReturnOnlyFoundNotes()
    {
        // Arrange
        var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        var noteId1 = Guid.NewGuid();
        var noteId2 = Guid.NewGuid();
        var note1 = CreateNote(noteId1);

        _embeddingService.GenerateEmbeddingAsync("test query", Arg.Any<CancellationToken>())
            .Returns(queryEmbedding);
        _embeddingRepository.FindSimilarAsync(queryEmbedding, 5, Arg.Any<CancellationToken>())
            .Returns(new List<(Guid, float)> { (noteId1, 0.9f), (noteId2, 0.8f) });
        _noteRepository.GetById(noteId1, Arg.Any<CancellationToken>()).Returns(note1);
        _noteRepository.GetById(noteId2, Arg.Any<CancellationToken>()).Returns((Note?)null);

        // Act
        var result = await _service.GetRelevantNotesAsync("test query", 5);

        // Assert
        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(noteId1);
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
        await _service.GetRelevantNotesAsync("test query", 3);

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
            _service.GetRelevantNotesAsync("test", 5, cts.Token));
    }

    private static Note CreateNote(Guid id) => new()
    {
        Id = id,
        NotebookId = Guid.NewGuid(),
        CreatedAt = DateTimeOffset.UtcNow,
        LastModifiedAt = DateTimeOffset.UtcNow
    };
}
