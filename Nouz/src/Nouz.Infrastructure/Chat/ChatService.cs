using System.Collections.Immutable;
using Nouz.Application.Chat;

namespace Nouz.Infrastructure.Chat;

/// <summary>
/// Chat service implementation.
/// TODO: Integrate with Semantic Kernel for actual AI responses.
/// </summary>
internal sealed class ChatService : IChatService
{
    public async Task<string> GetResponseAsync(
        string message,
        ImmutableList<ChatMessage> conversationHistory,
        CancellationToken cancellationToken = default)
    {
        // TODO: Replace with Semantic Kernel integration
        // This is a placeholder implementation

        await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

        return "This is a placeholder response. Semantic Kernel integration coming soon! " +
               $"You said: \"{message}\"";
    }
}
