namespace Nouz.Application.Chat.Models;

/// <summary>
/// Represents a block definition used by the AI for creating or editing notes.
/// This is the format the AI uses when specifying note content structure.
/// </summary>
public sealed record BlockDefinition
{
    /// <summary>
    /// The type of block: paragraph, h1, h2, h3, h4, listitem, todoitem, agendaitem, code, quote, decision, warning.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// The text content of the block.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Optional metadata for the block (e.g., checked state for todoitem).
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
