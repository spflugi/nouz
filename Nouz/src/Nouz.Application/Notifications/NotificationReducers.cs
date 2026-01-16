using Nouz.ReduxSimple;

namespace Nouz.Application.Notifications;

public static class NotificationReducers
{
    public static IEnumerable<On<T>> Create<T>(Func<T, NotificationState> selector) where T : class, new()
    {
        return Reducers.CreateSubReducers(selector)
            .On<NotificationActions.NotificationShown>((state, action) =>
            {
                var updatedNotifications = state.Notifications.Add(action.Notification.Id, action.Notification);
                return state with { Notifications = updatedNotifications };
            })
            .On<NotificationActions.NotificationDismissed>((state, action) =>
            {
                var updatedNotifications = state.Notifications.Remove(action.Id);
                return state with { Notifications = updatedNotifications };
            })
            .ToList();
    }
}