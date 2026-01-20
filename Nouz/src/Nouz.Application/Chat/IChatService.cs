using System.Collections.Immutable;

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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The assistant's response.</returns>
    Task<string> GetResponseAsync(
        string message,
        ImmutableList<ChatMessage> conversationHistory,
        CancellationToken cancellationToken = default);
}
