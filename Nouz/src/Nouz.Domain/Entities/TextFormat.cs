namespace Nouz.Domain.Entities;

public sealed record TextFormat
{
    public required int Start { get; init; }
    public required int End { get; init; }
    public required TextFormatType Type { get; init; }
    public string? Value { get; init; }
}
