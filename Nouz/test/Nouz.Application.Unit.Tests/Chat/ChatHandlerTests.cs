using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Mediator;
using Nouz.Application.Chat;
using Nouz.Application.Logger;
using Nouz.Application.Notifications;
using Nouz.Application.Store;
using Nouz.Domain.Entities;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Nouz.Application.Unit.Tests.Chat;

public class ChatHandlerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IChatService _chatService = Substitute.For<IChatService>();
    private readonly INoteContextService _noteContextService = Substitute.For<INoteContextService>();
    private readonly IStateProvider _stateProvider = Substitute.For<IStateProvider>();
    private readonly IActionDispatcher _actionDispatcher = Substitute.For<IActionDispatcher>();
    private readonly ILoggerAdapter<ChatHandler> _logger = Substitute.For<ILoggerAdapter<ChatHandler>>();
    private readonly ChatHandler _handler;

    public ChatHandlerTests()
    {
        _handler = new ChatHandler(_mediator, _chatService, _noteContextService, _stateProvider, _actionDispatcher, _logger);

        // Default state setup
        _stateProvider.State.Returns(new RootState());

        // Default: no relevant notes
        _noteContextService.GetRelevantNotesAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<float>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    #region SendMessage Tests

    [Fact]
    public async Task SendMessage_ShouldDispatchUserMessageAdded()
    {
        // Arrange
        var content = "Hello, assistant!";
        _chatService.GetResponseAsync(content, Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<IReadOnlyList<Note>?>(), Arg.Any<CancellationToken>())
            .Returns("Hello, user!");

        // Act
        await _handler.Handle(new ChatCommands.SendMessage(content), TestContext.Current.CancellationToken);

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
        _chatService.GetResponseAsync(Arg.Any<string>(), Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<IReadOnlyList<Note>?>(), Arg.Any<CancellationToken>())
            .Returns("Response");

        // Act
        await _handler.Handle(new ChatCommands.SendMessage("Hello"), TestContext.Current.CancellationToken);

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
        _chatService.GetResponseAsync(Arg.Any<string>(), Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<IReadOnlyList<Note>?>(), Arg.Any<CancellationToken>())
            .Returns("Response");

        // Act
        await _handler.Handle(new ChatCommands.SendMessage("New message"), TestContext.Current.CancellationToken);

        // Assert
        await _chatService.Received(1).GetResponseAsync(
            "New message",
            Arg.Is<ImmutableList<ChatMessage>>(h => h.Count == 1),
            Arg.Any<IReadOnlyList<Note>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendMessage_ShouldDispatchAssistantMessageReceived()
    {
        // Arrange
        var response = "This is the assistant response";
        _chatService.GetResponseAsync(Arg.Any<string>(), Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<IReadOnlyList<Note>?>(), Arg.Any<CancellationToken>())
            .Returns(response);

        // Act
        await _handler.Handle(new ChatCommands.SendMessage("Hello"), TestContext.Current.CancellationToken);

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
        _chatService.GetResponseAsync(Arg.Any<string>(), Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<IReadOnlyList<Note>?>(), Arg.Any<CancellationToken>())
            .Throws(exception);

        // Act
        await _handler.Handle(new ChatCommands.SendMessage("Hello"), TestContext.Current.CancellationToken);

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
        _chatService.GetResponseAsync(Arg.Any<string>(), Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<IReadOnlyList<Note>?>(), Arg.Any<CancellationToken>())
            .Throws(new Exception("Error"));

        // Act
        await _handler.Handle(new ChatCommands.SendMessage("Hello"), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<ChatActions.AssistantMessageReceived>(a =>
                a.Message.Content.Contains("error")));
    }

    [Fact]
    public async Task SendMessage_ShouldCallNoteContextService()
    {
        // Arrange
        _chatService.GetResponseAsync(Arg.Any<string>(), Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<IReadOnlyList<Note>?>(), Arg.Any<CancellationToken>())
            .Returns("Response");

        // Act
        await _handler.Handle(new ChatCommands.SendMessage("Hello"), TestContext.Current.CancellationToken);

        // Assert
        await _noteContextService.Received(1).GetRelevantNotesAsync(
            "Hello",
            3,
            0.3f,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendMessage_WhenNotesReturned_ShouldIncludeContextNoteTitles()
    {
        // Arrange
        var note = new Note
        {
            Id = Guid.NewGuid(),
            NotebookId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            LastModifiedAt = DateTimeOffset.UtcNow
        };
        var contextResults = new List<NoteContextResult>
        {
            new(note, "My Note", 0.9f)
        };
        _noteContextService.GetRelevantNotesAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<float>(), Arg.Any<CancellationToken>())
            .Returns(contextResults);
        _chatService.GetResponseAsync(Arg.Any<string>(), Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<IReadOnlyList<Note>?>(), Arg.Any<CancellationToken>())
            .Returns("Response");

        // Act
        await _handler.Handle(new ChatCommands.SendMessage("Hello"), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<ChatActions.AssistantMessageReceived>(a =>
                a.Message.ContextNoteTitles != null &&
                a.Message.ContextNoteTitles.Count == 1 &&
                a.Message.ContextNoteTitles[0] == "My Note"));
    }

    #endregion

    #region SendMessageStreaming Tests

    [Fact]
    public async Task SendMessageStreaming_ShouldDispatchUserMessageAdded()
    {
        // Arrange
        var content = "Hello, assistant!";
        _chatService.GetStreamingResponseAsync(content, Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<IReadOnlyList<Note>?>(), Arg.Any<CancellationToken>())
            .Returns(CreateAsyncEnumerable("Hello"));

        // Act
        await _handler.Handle(new ChatCommands.SendMessageStreaming(content), TestContext.Current.CancellationToken);

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
        _chatService.GetStreamingResponseAsync(Arg.Any<string>(), Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<IReadOnlyList<Note>?>(), Arg.Any<CancellationToken>())
            .Returns(CreateAsyncEnumerable("chunk1", "chunk2"));

        // Act
        await _handler.Handle(new ChatCommands.SendMessageStreaming("Hello"), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(Arg.Any<ChatActions.StreamingMessageStarted>());
    }

    [Fact]
    public async Task SendMessageStreaming_ShouldDispatchChunksReceived()
    {
        // Arrange
        var chunks = new[] { "Hello", " ", "World" };
        _chatService.GetStreamingResponseAsync(Arg.Any<string>(), Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<IReadOnlyList<Note>?>(), Arg.Any<CancellationToken>())
            .Returns(CreateAsyncEnumerable(chunks));

        // Act
        await _handler.Handle(new ChatCommands.SendMessageStreaming("Hello"), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(3).Dispatch(Arg.Any<ChatActions.StreamingChunkReceived>());
    }

    [Fact]
    public async Task SendMessageStreaming_ShouldDispatchStreamingMessageCompleted()
    {
        // Arrange
        _chatService.GetStreamingResponseAsync(Arg.Any<string>(), Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<IReadOnlyList<Note>?>(), Arg.Any<CancellationToken>())
            .Returns(CreateAsyncEnumerable("chunk"));

        // Act
        await _handler.Handle(new ChatCommands.SendMessageStreaming("Hello"), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(Arg.Any<ChatActions.StreamingMessageCompleted>());
    }

    [Fact]
    public async Task SendMessageStreaming_WhenChatServiceThrows_ShouldShowErrorNotification()
    {
        // Arrange
        var exception = new Exception("Streaming error");
        _chatService.GetStreamingResponseAsync(Arg.Any<string>(), Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<IReadOnlyList<Note>?>(), Arg.Any<CancellationToken>())
            .Throws(exception);

        // Act
        await _handler.Handle(new ChatCommands.SendMessageStreaming("Hello"), TestContext.Current.CancellationToken);

        // Assert
        _logger.Received(1).LogError(exception, "Failed to send streaming chat message");
        await _mediator.Received(1).Send(
            Arg.Is<NotificationCommands.ShowNotification>(n =>
                n.Title == "Error" &&
                n.Severity == NotificationSeverity.Error),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendMessageStreaming_ShouldCallNoteContextService()
    {
        // Arrange
        _chatService.GetStreamingResponseAsync(Arg.Any<string>(), Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<IReadOnlyList<Note>?>(), Arg.Any<CancellationToken>())
            .Returns(CreateAsyncEnumerable("chunk"));

        // Act
        await _handler.Handle(new ChatCommands.SendMessageStreaming("Hello"), TestContext.Current.CancellationToken);

        // Assert
        await _noteContextService.Received(1).GetRelevantNotesAsync(
            "Hello",
            3,
            0.3f,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendMessageStreaming_WhenNotesReturned_ShouldPassContextNoteTitlesToStreamingStarted()
    {
        // Arrange
        var note = new Note
        {
            Id = Guid.NewGuid(),
            NotebookId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            LastModifiedAt = DateTimeOffset.UtcNow
        };
        var contextResults = new List<NoteContextResult>
        {
            new(note, "Test Note", 0.8f)
        };
        _noteContextService.GetRelevantNotesAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<float>(), Arg.Any<CancellationToken>())
            .Returns(contextResults);
        _chatService.GetStreamingResponseAsync(Arg.Any<string>(), Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<IReadOnlyList<Note>?>(), Arg.Any<CancellationToken>())
            .Returns(CreateAsyncEnumerable("chunk"));

        // Act
        await _handler.Handle(new ChatCommands.SendMessageStreaming("Hello"), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(
            Arg.Is<ChatActions.StreamingMessageStarted>(a =>
                a.ContextNoteTitles != null &&
                a.ContextNoteTitles.Count == 1 &&
                a.ContextNoteTitles[0] == "Test Note"));
    }

    [Fact]
    public async Task SendMessageStreaming_WhenNotesReturned_ShouldPassRelevantNotesToChatService()
    {
        // Arrange
        var note = new Note
        {
            Id = Guid.NewGuid(),
            NotebookId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            LastModifiedAt = DateTimeOffset.UtcNow
        };
        var contextResults = new List<NoteContextResult>
        {
            new(note, "Test Note", 0.8f)
        };
        _noteContextService.GetRelevantNotesAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<float>(), Arg.Any<CancellationToken>())
            .Returns(contextResults);
        _chatService.GetStreamingResponseAsync(Arg.Any<string>(), Arg.Any<ImmutableList<ChatMessage>>(), Arg.Any<IReadOnlyList<Note>?>(), Arg.Any<CancellationToken>())
            .Returns(CreateAsyncEnumerable("chunk"));

        // Act
        await _handler.Handle(new ChatCommands.SendMessageStreaming("Hello"), TestContext.Current.CancellationToken);

        // Assert
        _chatService.Received(1).GetStreamingResponseAsync(
            "Hello",
            Arg.Any<ImmutableList<ChatMessage>>(),
            Arg.Is<IReadOnlyList<Note>?>(notes => notes != null && notes.Count == 1 && notes[0].Id == note.Id),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region ClearChat Tests

    [Fact]
    public async Task ClearChat_ShouldDispatchChatCleared()
    {
        // Act
        await _handler.Handle(new ChatCommands.ClearChat(), TestContext.Current.CancellationToken);

        // Assert
        await _actionDispatcher.Received(1).Dispatch(Arg.Any<ChatActions.ChatCleared>());
    }

    [Fact]
    public async Task ClearChat_ShouldLogDebugMessage()
    {
        // Act
        await _handler.Handle(new ChatCommands.ClearChat(), TestContext.Current.CancellationToken);

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
