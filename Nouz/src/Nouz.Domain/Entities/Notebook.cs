namespace Nouz.Domain.Entities;

public sealed record Notebook
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset LastModifiedAt { get; init; }

    public int SortOrder { get; init; }

    public ICollection<Note> Notes { get; init; } = [];
}