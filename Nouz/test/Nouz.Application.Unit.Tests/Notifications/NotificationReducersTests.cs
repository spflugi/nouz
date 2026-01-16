using System.Collections.Immutable;
using Nouz.Application.Notifications;
using Shouldly;

namespace Nouz.Application.Unit.Tests.Notifications;

public class NotificationReducersTests
{
    private readonly IEnumerable<Nouz.ReduxSimple.On<TestState>> _reducers;

    public NotificationReducersTests()
    {
        _reducers = NotificationReducers.Create<TestState>(s => s.Notifications);
    }

    private TestState ApplyAction(TestState state, object action)
    {
        foreach (var reducer in _reducers)
        {
            if (reducer.Reduce != null)
            {
                state = reducer.Reduce(state, action);
            }
        }
        return state;
    }

    #region NotificationShown Tests

    [Fact]
    public void NotificationShown_ShouldAddNotificationToState()
    {
        // Arrange
        var notification = new Notification(Guid.NewGuid(), "Title", "Message", NotificationSeverity.Info);
        var state = new TestState();

        // Act
        var newState = ApplyAction(state, new NotificationActions.NotificationShown(notification));

        // Assert
        newState.Notifications.Notifications.Count.ShouldBe(1);
        newState.Notifications.Notifications.ContainsKey(notification.Id).ShouldBeTrue();
        newState.Notifications.Notifications[notification.Id].ShouldBe(notification);
    }

    [Fact]
    public void NotificationShown_ShouldAddToExistingNotifications()
    {
        // Arrange
        var existingNotification = new Notification(Guid.NewGuid(), "Existing", "Message", NotificationSeverity.Warning);
        var newNotification = new Notification(Guid.NewGuid(), "New", "Message", NotificationSeverity.Error);
        var state = new TestState
        {
            Notifications = new NotificationState
            {
                Notifications = ImmutableDictionary<Guid, Notification>.Empty.Add(existingNotification.Id, existingNotification)
            }
        };

        // Act
        var newState = ApplyAction(state, new NotificationActions.NotificationShown(newNotification));

        // Assert
        newState.Notifications.Notifications.Count.ShouldBe(2);
        newState.Notifications.Notifications.ContainsKey(existingNotification.Id).ShouldBeTrue();
        newState.Notifications.Notifications.ContainsKey(newNotification.Id).ShouldBeTrue();
    }

    [Theory]
    [InlineData(NotificationSeverity.Info)]
    [InlineData(NotificationSeverity.Success)]
    [InlineData(NotificationSeverity.Warning)]
    [InlineData(NotificationSeverity.Error)]
    public void NotificationShown_ShouldPreserveSeverity(NotificationSeverity severity)
    {
        // Arrange
        var notification = new Notification(Guid.NewGuid(), "Title", "Message", severity);
        var state = new TestState();

        // Act
        var newState = ApplyAction(state, new NotificationActions.NotificationShown(notification));

        // Assert
        newState.Notifications.Notifications[notification.Id].Severity.ShouldBe(severity);
    }

    #endregion

    #region NotificationDismissed Tests

    [Fact]
    public void NotificationDismissed_ShouldRemoveNotificationFromState()
    {
        // Arrange
        var notification = new Notification(Guid.NewGuid(), "Title", "Message", NotificationSeverity.Info);
        var state = new TestState
        {
            Notifications = new NotificationState
            {
                Notifications = ImmutableDictionary<Guid, Notification>.Empty.Add(notification.Id, notification)
            }
        };

        // Act
        var newState = ApplyAction(state, new NotificationActions.NotificationDismissed(notification.Id));

        // Assert
        newState.Notifications.Notifications.ShouldBeEmpty();
    }

    [Fact]
    public void NotificationDismissed_WhenNotificationNotFound_ShouldReturnUnchangedState()
    {
        // Arrange
        var existingNotification = new Notification(Guid.NewGuid(), "Existing", "Message", NotificationSeverity.Info);
        var state = new TestState
        {
            Notifications = new NotificationState
            {
                Notifications = ImmutableDictionary<Guid, Notification>.Empty.Add(existingNotification.Id, existingNotification)
            }
        };

        // Act
        var newState = ApplyAction(state, new NotificationActions.NotificationDismissed(Guid.NewGuid()));

        // Assert
        newState.Notifications.Notifications.Count.ShouldBe(1);
        newState.Notifications.Notifications.ContainsKey(existingNotification.Id).ShouldBeTrue();
    }

    [Fact]
    public void NotificationDismissed_ShouldOnlyRemoveTargetedNotification()
    {
        // Arrange
        var notification1 = new Notification(Guid.NewGuid(), "Notification 1", "Message", NotificationSeverity.Info);
        var notification2 = new Notification(Guid.NewGuid(), "Notification 2", "Message", NotificationSeverity.Warning);
        var state = new TestState
        {
            Notifications = new NotificationState
            {
                Notifications = ImmutableDictionary<Guid, Notification>.Empty
                    .Add(notification1.Id, notification1)
                    .Add(notification2.Id, notification2)
            }
        };

        // Act
        var newState = ApplyAction(state, new NotificationActions.NotificationDismissed(notification1.Id));

        // Assert
        newState.Notifications.Notifications.Count.ShouldBe(1);
        newState.Notifications.Notifications.ContainsKey(notification1.Id).ShouldBeFalse();
        newState.Notifications.Notifications.ContainsKey(notification2.Id).ShouldBeTrue();
    }

    #endregion

    private sealed record TestState
    {
        public NotificationState Notifications { get; init; } = new();
    }
}
