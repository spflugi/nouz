using Nouz.Application.Store;

namespace Nouz.Application.Chat;

/// <summary>
/// Actions for chat state changes.
/// </summary>
public static class ChatActions
{
    /// <summary>
    /// Triggered when a user message is added to the conversation.
    /// </summary>
    public sealed record UserMessageAdded(ChatMessage Message) : IAction;

    /// <summary>
    /// Triggered when the assistant starts typing a response.
    /// </summary>
    public sealed record AssistantTypingStarted : IAction;

    /// <summary>
    /// Triggered when an assistant message is received.
    /// </summary>
    public sealed record AssistantMessageReceived(ChatMessage Message) : IAction;

    /// <summary>
    /// Triggered when the chat conversation is cleared.
    /// </summary>
    public sealed record ChatCleared : IAction;
}
