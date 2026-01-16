using System.Collections.Immutable;

namespace Nouz.Application.Notifications;

public sealed record NotificationState
{
    public ImmutableDictionary<Guid, Notification> Notifications { get; init; } = [];
}