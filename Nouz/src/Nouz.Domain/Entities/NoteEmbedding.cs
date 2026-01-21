namespace Nouz.Domain.Entities;

public sealed record NoteEmbedding
{
    public required Guid NoteId { get; init; }
    public required float[] Embedding { get; init; }
    public required DateTimeOffset LastUpdatedAt { get; init; }
    public Note Note { get; init; } = null!;
}
