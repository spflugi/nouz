using Mediator;

namespace Nouz.Application.Chat;

/// <summary>
/// Commands for chat operations.
/// </summary>
public static class ChatCommands
{
    /// <summary>
    /// Send a message to the chat assistant.
    /// </summary>
    public sealed record SendMessage(string Content) : ICommand;

    /// <summary>
    /// Clear the chat conversation history.
    /// </summary>
    public sealed record ClearChat : ICommand;
}
