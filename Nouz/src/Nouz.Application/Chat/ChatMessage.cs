using System.Collections.Immutable;

namespace Nouz.Application.Chat;

/// <summary>
/// Represents a message in a chat conversation.
/// </summary>
public sealed record ChatMessage(
    Guid Id,
    string Content,
    ChatMessageRole Role,
    DateTimeOffset Timestamp,
    ImmutableList<string>? ContextNoteTitles = null);

/// <summary>
/// The role of a chat message sender.
/// </summary>
public enum ChatMessageRole
{
    User,
    Assistant
}
