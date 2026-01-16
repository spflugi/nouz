using Nouz.Application.Store;

namespace Nouz.Application.Notifications;

public static class NotificationActions
{
    /// <summary>
    /// Triggered when a notification of any type is shown to the user.
    /// </summary>
    public sealed record NotificationShown(Notification Notification) : IAction;

    /// <summary>
    /// Triggered when a notification is dismissed by the user or automatically.
    /// </summary>
    public sealed record NotificationDismissed(Guid Id) : IAction;
}