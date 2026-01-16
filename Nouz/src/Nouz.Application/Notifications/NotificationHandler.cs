using Mediator;
using Nouz.Application.Logger;
using Nouz.Application.Store;

namespace Nouz.Application.Notifications;

internal sealed class NotificationHandler : ICommandHandler<NotificationCommands.ShowNotification>, ICommandHandler<NotificationCommands.DismissNotification>
{
    private readonly IActionDispatcher _actionDispatcher;
    private readonly ILoggerAdapter<NotificationHandler> _logger;

    public NotificationHandler(IActionDispatcher actionDispatcher, ILoggerAdapter<NotificationHandler> logger)
    {
        _actionDispatcher = actionDispatcher;
        _logger = logger;
    }

    public async ValueTask<Unit> Handle(NotificationCommands.ShowNotification command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Showing notification: {Title} - {Message} (Severity: {Severity})",
            command.Title, command.Message, command.Severity);

        await _actionDispatcher
            .Dispatch(new NotificationActions.NotificationShown(new Notification(Guid.NewGuid(), command.Title,
                command.Message, command.Severity))).ConfigureAwait(false);

        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(NotificationCommands.DismissNotification command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Dismiss notification with id {NotificationId}", command.Id);

        await _actionDispatcher
            .Dispatch(new NotificationActions.NotificationDismissed(command.Id));

        return Unit.Value;
    }
}