using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Mediator;
using Nouz.Application.Chat;
using Nouz.Application.Logger;
using Nouz.Application.Notifications;
using Nouz.Application.Store;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Nouz.Application.Unit.Tests.Chat;

public class ChatHandlerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IChatService _chatService = Substitute.For<IChatService>();
    private readonly IStateProvider _stateProvider = Substitute.For<IStateProvider>();
    private readonly IActionDispatcher _actionDispatcher = Substitute.For<IActionDispatcher>();
    private readonly ILoggerAdapter<ChatHandler> _logger = Substitute.For<ILoggerAdapter<ChatHandler>>();
    private readonly ChatHandler _handler;

    public ChatHandlerTests()
    {
        _handler = new ChatHandler(_mediator, _chatService, _stateProvider, _actionDispatcher, _logger);

        // Default state setup
        _stateProvider.State.Returns(new RootState());
    }

    #region SendMessage Tests

    [Fact]
    public async Task SendMessage_ShouldDispatchUserMessageAdded()
    {
        // Arrange
        var content = "Hello, assistant!";
        _chatService.GetResponseAsync(content, Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns("Hello, user!");

        // Act
        await _handler.Handle(new ChatCommands.SendMessage(content), CancellationToken.None);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<ChatActions.UserMessageAdded>(a =>
                a.Message.Content == content &&
                a.Message.Role == ChatMessageRole.User));
    }

    [Fact]
    public async Task SendMessage_ShouldDispatchTypingStarted()
    {
        // Arrange
        _chatService.GetResponseAsync(Arg.Any<string>(), Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns("Response");

        // Act
        await _handler.Handle(new ChatCommands.SendMessage("Hello"), CancellationToken.None);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(Arg.Any<ChatActions.AssistantTypingStarted>());
    }

    [Fact]
    public async Task SendMessage_ShouldCallChatServiceWithHistory()
    {
        // Arrange
        var existingMessage = new ChatMessage(Guid.NewGuid(), "Previous", ChatMessageRole.User, DateTimeOffset.UtcNow);
        var state = new RootState
        {
            Chat = new ChatState { Messages = ImmutableList.Create(existingMessage) }
        };
        _stateProvider.State.Returns(state);
        _chatService.GetResponseAsync(Arg.Any<string>(), Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns("Response");

        // Act
        await _handler.Handle(new ChatCommands.SendMessage("New message"), CancellationToken.None);

        // Assert
        await _chatService.Received(1).GetResponseAsync(
            "New message",
            Arg.Is<ImmutableList<ChatMessage>>(h => h.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendMessage_ShouldDispatchAssistantMessageReceived()
    {
        // Arrange
        var response = "This is the assistant response";
        _chatService.GetResponseAsync(Arg.Any<string>(), Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns(response);

        // Act
        await _handler.Handle(new ChatCommands.SendMessage("Hello"), CancellationToken.None);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<ChatActions.AssistantMessageReceived>(a =>
                a.Message.Content == response &&
                a.Message.Role == ChatMessageRole.Assistant));
    }

    [Fact]
    public async Task SendMessage_WhenChatServiceThrows_ShouldShowErrorNotification()
    {
        // Arrange
        var exception = new Exception("API Error");
        _chatService.GetResponseAsync(Arg.Any<string>(), Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Throws(exception);

        // Act
        await _handler.Handle(new ChatCommands.SendMessage("Hello"), CancellationToken.None);

        // Assert
        _logger.Received(1).LogError(exception, "Failed to send chat message");
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Error" &&
                n.Severity == NotificationSeverity.Error),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendMessage_WhenChatServiceThrows_ShouldDispatchErrorMessage()
    {
        // Arrange
        _chatService.GetResponseAsync(Arg.Any<string>(), Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Error"));

        // Act
        await _handler.Handle(new ChatCommands.SendMessage("Hello"), CancellationToken.None);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<ChatActions.AssistantMessageReceived>(a =>
                a.Message.Content.Contains("error")));
    }

    #endregion

    #region SendMessageStreaming Tests

    [Fact]
    public async Task SendMessageStreaming_ShouldDispatchUserMessageAdded()
    {
        // Arrange
        var content = "Hello, assistant!";
        _chatService.GetStreamingResponseAsync(content, Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns(CreateAsyncEnumerable("Hello"));

        // Act
        await _handler.Handle(new ChatCommands.SendMessageStreaming(content), CancellationToken.None);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<ChatActions.UserMessageAdded>(a =>
                a.Message.Content == content &&
                a.Message.Role == ChatMessageRole.User));
    }

    [Fact]
    public async Task SendMessageStreaming_ShouldDispatchStreamingMessageStarted()
    {
        // Arrange
        _chatService.GetStreamingResponseAsync(Arg.Any<string>(), Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns(CreateAsyncEnumerable("chunk1", "chunk2"));

        // Act
        await _handler.Handle(new ChatCommands.SendMessageStreaming("Hello"), CancellationToken.None);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(Arg.Any<ChatActions.StreamingMessageStarted>());
    }

    [Fact]
    public async Task SendMessageStreaming_ShouldDispatchChunksReceived()
    {
        // Arrange
        var chunks = new[] { "Hello", " ", "World" };
        _chatService.GetStreamingResponseAsync(Arg.Any<string>(), Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns(CreateAsyncEnumerable(chunks));

        // Act
        await _handler.Handle(new ChatCommands.SendMessageStreaming("Hello"), CancellationToken.None);

        // Assert
        await _actionDispatcher.Received(3).Dispatch(Arg.Any<ChatActions.StreamingChunkReceived>());
    }

    [Fact]
    public async Task SendMessageStreaming_ShouldDispatchStreamingMessageCompleted()
    {
        // Arrange
        _chatService.GetStreamingResponseAsync(Arg.Any<string>(), Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Returns(CreateAsyncEnumerable("chunk"));

        // Act
        await _handler.Handle(new ChatCommands.SendMessageStreaming("Hello"), CancellationToken.None);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(Arg.Any<ChatActions.StreamingMessageCompleted>());
    }

    [Fact]
    public async Task SendMessageStreaming_WhenChatServiceThrows_ShouldShowErrorNotification()
    {
        // Arrange
        var exception = new Exception("Streaming error");
        _chatService.GetStreamingResponseAsync(Arg.Any<string>(), Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<CancellationToken>())
            .Throws(exception);

        // Act
        await _handler.Handle(new ChatCommands.SendMessageStreaming("Hello"), CancellationToken.None);

        // Assert
        _logger.Received(1).LogError(exception, "Failed to send streaming chat message");
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Error" &&
                n.Severity == NotificationSeverity.Error),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region ClearChat Tests

    [Fact]
    public async Task ClearChat_ShouldDispatchChatCleared()
    {
        // Act
        await _handler.Handle(new ChatCommands.ClearChat(), CancellationToken.None);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(Arg.Any<ChatActions.ChatCleared>());
    }

    [Fact]
    public async Task ClearChat_ShouldLogDebugMessage()
    {
        // Act
        await _handler.Handle(new ChatCommands.ClearChat(), CancellationToken.None);

        // Assert
        _logger.Received(1).LogDebug("Clearing chat history");
        _logger.Received(1).LogDebug("Chat history cleared");
    }

    #endregion

    private static async IAsyncEnumerable<string> CreateAsyncEnumerable(params string[] items)
    {
        foreach (var item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }
}
