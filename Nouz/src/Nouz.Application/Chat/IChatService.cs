using System.Collections.Immutable;
using Nouz.Domain.Entities;

namespace Nouz.Application.Chat;

/// <summary>
/// Service for interacting with the AI chat assistant.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Gets a response from the AI assistant.
    /// </summary>
    /// <param name="message">The user's message.</param>
    /// <param name="conversationHistory">The previous messages in the conversation for context.</param>
    /// <param name="relevantNotes">Optional relevant notes to include as context in the system prompt.</param>
    /// <param name="onToolCall">Optional callback invoked when a tool is called. Arguments: functionName, isCompleted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The assistant's response.</returns>
    Task<string> GetResponseAsync(
        string message,
        ImmutableList<ChatMessage> conversationHistory,
        IReadOnlyList<Note>? relevantNotes = null,
        Action<string, bool>? onToolCall = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a streaming response from the AI assistant.
    /// </summary>
    /// <param name="message">The user's message.</param>
    /// <param name="conversationHistory">The previous messages in the conversation for context.</param>
    /// <param name="relevantNotes">Optional relevant notes to include as context in the system prompt.</param>
    /// <param name="onToolCall">Optional callback invoked when a tool is called. Arguments: functionName, isCompleted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of response chunks.</returns>
    IAsyncEnumerable<string> GetStreamingResponseAsync(
        string message,
        ImmutableList<ChatMessage> conversationHistory,
        IReadOnlyList<Note>? relevantNotes = null,
        Action<string, bool>? onToolCall = null,
        CancellationToken cancellationToken = default);
}
