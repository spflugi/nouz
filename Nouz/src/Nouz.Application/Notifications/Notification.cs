namespace Nouz.Application.Notifications;

public record Notification(Guid Id, string Title, string Message, NotificationSeverity Severity = NotificationSeverity.Info);