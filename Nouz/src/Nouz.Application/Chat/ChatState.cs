using System.Collections.Immutable;

namespace Nouz.Application.Chat;

/// <summary>
/// Represents the state of the chat conversation.
/// </summary>
public sealed record ChatState
{
    /// <summary>
    /// Gets the list of messages in the conversation.
    /// </summary>
    public ImmutableList<ChatMessage> Messages { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether the assistant is currently typing.
    /// </summary>
    public bool IsTyping { get; init; }
}
