using Mediator;

namespace Nouz.Application.Notifications;

public static class NotificationCommands
{
    /// <summary>
    /// Show a notification to the user.
    /// </summary>
    public sealed record ShowNotification(
        string Title,
        string Message,
        NotificationSeverity Severity) : ICommand;

    /// <summary>
    /// Dismiss a currently showing notification.
    /// </summary>
    public sealed record DismissNotification(Guid Id) : ICommand;
}