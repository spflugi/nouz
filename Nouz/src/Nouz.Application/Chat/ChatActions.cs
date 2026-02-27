using System.Collections.Immutable;
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

    /// <summary>
    /// Triggered when a streaming message starts.
    /// </summary>
    public sealed record StreamingMessageStarted(Guid MessageId, ImmutableList<string>? ContextNoteTitles = null) : IAction;

    /// <summary>
    /// Triggered when a streaming chunk is received.
    /// </summary>
    public sealed record StreamingChunkReceived(Guid MessageId, string Chunk) : IAction;

    /// <summary>
    /// Triggered when a streaming message is completed.
    /// </summary>
    public sealed record StreamingMessageCompleted(Guid MessageId) : IAction;

    /// <summary>
    /// Triggered when a tool call is started by the AI assistant.
    /// </summary>
    public sealed record ToolCallStarted(Guid MessageId, ToolCallActivity Activity) : IAction;

    /// <summary>
    /// Triggered when a tool call is completed by the AI assistant.
    /// </summary>
    public sealed record ToolCallCompleted(Guid MessageId, string FunctionName) : IAction;
}
