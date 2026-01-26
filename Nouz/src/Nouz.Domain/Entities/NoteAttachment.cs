namespace Nouz.Domain.Entities;

public sealed record NoteAttachment
{
    public required Guid Id { get; init; }
    public required Guid NoteId { get; init; }
    public required string FileName { get; init; }
    public required string Extension { get; init; }
    public required long FileSizeBytes { get; init; }
    public required string ContentHash { get; init; }
    public required DateTimeOffset AddedAt { get; init; }
    public Note Note { get; init; } = null!;
}
