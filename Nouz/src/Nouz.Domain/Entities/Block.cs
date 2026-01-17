namespace Nouz.Domain.Entities;

public sealed record Block
{
    public required Guid Id { get; init; }
    public required BlockType Type { get; init; }
    public string Content { get; init; } = string.Empty;
    public Dictionary<string, object> Metadata { get; init; } = [];
}