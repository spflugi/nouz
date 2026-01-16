using System.Reactive.Linq;
using Nouz.Application.Notifications;
using Nouz.Extensions;

namespace Nouz.Components.Layout;

public partial class NotificationContainer
{
    private const int NotificationDisplayTimeInSeconds = 5;
    private readonly List<Notification> _notifications = new();
    private readonly Dictionary<Guid, CancellationTokenSource> _timers = new();

    protected override void OnInitialized()
    {
        StateProvider.StateObservable
            .Select(s => s.Notifications.Notifications)
            .DistinctUntilChanged()
            .TakeUntilDisappearing(this)
            .Subscribe(notifications =>
            {
                ShowNotifications(notifications.Values);
            });
    }

    private void ShowNotifications(IEnumerable<Notification> notifications)
    {
        foreach (var notification in notifications)
        {
            if (_notifications.Contains(notification))
            {
                continue;
            }

            _notifications.Add(notification);
            StateHasChanged();
            StartTimer(notification);
        }
    }

    private void StartTimer(Notification notification)
    {
        _timers[notification.Id] = new CancellationTokenSource();

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(NotificationDisplayTimeInSeconds), _timers[notification.Id].Token);
                await InvokeAsync(() =>
                {
                    Close(notification);
                });
            } catch (TaskCanceledException)
            {
                // Timer was canceled, do nothing
            }
        });
    }

    private async void Close(Notification notification)
    {
        _notifications.Remove(notification);
        StateHasChanged();

        _timers.Remove(notification.Id);
        StateHasChanged();

        try
        {
            await Mediator.Send(new NotificationCommands.DismissNotification(notification.Id));
        }
        catch (Exception)
        {
            // Do nothing
        }
    }
}