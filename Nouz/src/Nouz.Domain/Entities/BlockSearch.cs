namespace Nouz.Domain.Entities;

public sealed record BlockSearch
{
    public Guid BlockId { get; init; }
    public Guid NoteId { get; init; }
    public string Content { get; init; } = string.Empty;
}