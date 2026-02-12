using Nouz.Application.Logger;
using Nouz.Application.Notifications;
using Nouz.Application.Store;
using NSubstitute;
using Shouldly;

namespace Nouz.Application.Unit.Tests.Notifications;

public class NotificationHandlerTests
{
    private readonly IActionDispatcher _actionDispatcher = Substitute.For<IActionDispatcher>();
    private readonly ILoggerAdapter<NotificationHandler> _logger = Substitute.For<ILoggerAdapter<NotificationHandler>>();
    private readonly NotificationHandler _handler;

    public NotificationHandlerTests()
    {
        _handler = new NotificationHandler(_actionDispatcher, _logger);
    }

    #region ShowNotification Tests

    [Fact]
    public async Task ShowNotification_ShouldDispatchNotificationShownAction()
    {
        // Arrange
        var command = new NotificationCommands.ShowNotification("Test Title", "Test Message", NotificationSeverity.Info);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NotificationActions.NotificationShown>(a =>
                a.Notification.Title == "Test Title" &&
                a.Notification.Message == "Test Message" &&
                a.Notification.Severity == NotificationSeverity.Info));
    }

    [Fact]
    public async Task ShowNotification_ShouldGenerateUniqueId()
    {
        // Arrange
        var command = new NotificationCommands.ShowNotification("Title", "Message", NotificationSeverity.Warning);
        Guid? capturedId = null;
        await _actionDispatcher.Dispatch(Arg.Do<NotificationActions.NotificationShown>(a => capturedId = a.Notification.Id));

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        capturedId.ShouldNotBeNull();
        capturedId.Value.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task ShowNotification_ShouldLogNotificationDetails()
    {
        // Arrange
        var command = new NotificationCommands.ShowNotification("Title", "Message", NotificationSeverity.Error);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _logger.Received(1).LogInformation(
            "Showing notification: {Title} - {Message} (Severity: {Severity})",
            "Title", "Message", NotificationSeverity.Error);
    }

    [Theory]
    [InlineData(NotificationSeverity.Info)]
    [InlineData(NotificationSeverity.Success)]
    [InlineData(NotificationSeverity.Warning)]
    [InlineData(NotificationSeverity.Error)]
    public async Task ShowNotification_ShouldHandleAllSeverityLevels(NotificationSeverity severity)
    {
        // Arrange
        var command = new NotificationCommands.ShowNotification("Title", "Message", severity);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NotificationActions.NotificationShown>(a => a.Notification.Severity == severity));
    }

    #endregion

    #region DismissNotification Tests

    [Fact]
    public async Task DismissNotification_ShouldDispatchNotificationDismissedAction()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var command = new NotificationCommands.DismissNotification(notificationId);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<NotificationActions.NotificationDismissed>(a => a.Id == notificationId));
    }

    [Fact]
    public async Task DismissNotification_ShouldLogDismissal()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var command = new NotificationCommands.DismissNotification(notificationId);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        _logger.Received(1).LogInformation("Dismiss notification with id {NotificationId}", notificationId);
    }

    #endregion
}
