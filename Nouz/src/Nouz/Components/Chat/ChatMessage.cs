namespace Nouz.Components.Chat;

public sealed record ChatMessage(
    string Content,
    ChatMessageRole Role,
    DateTime Timestamp);

public enum ChatMessageRole
{
    User,
    Assistant
}
