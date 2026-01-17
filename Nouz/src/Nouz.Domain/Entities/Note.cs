using System.Collections.Immutable;

namespace Nouz.Domain.Entities;

public sealed record Note
{
    public required Guid Id { get; init; }
    public required Guid NotebookId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset LastModifiedAt { get; init; }
    public ImmutableList<Block> Blocks { get; init; } = [];
    public Notebook Notebook { get; init; } = null!;
}