using System.Collections.Immutable;
using Nouz.Application.Chat;
using Nouz.Application.Settings;
using Shouldly;

namespace Nouz.Application.Unit.Tests.Chat;

public class ChatReducersTests
{
    private readonly IEnumerable<Nouz.ReduxSimple.On<TestState>> _reducers;

    public ChatReducersTests()
    {
        _reducers = ChatReducers.Create<TestState>(s => s.Chat);
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

    #region UserMessageAdded Tests

    [Fact]
    public void UserMessageAdded_ShouldAddMessageToList()
    {
        // Arrange
        var state = new TestState();
        var message = CreateMessage(ChatMessageRole.User, "Hello");

        // Act
        var newState = ApplyAction(state, new ChatActions.UserMessageAdded(message));

        // Assert
        newState.Chat.Messages.Count.ShouldBe(1);
        newState.Chat.Messages[0].ShouldBe(message);
    }

    [Fact]
    public void UserMessageAdded_ShouldAppendToExistingMessages()
    {
        // Arrange
        var existingMessage = CreateMessage(ChatMessageRole.Assistant, "Hi there");
        var state = new TestState
        {
            Chat = new ChatState { Messages = ImmutableList.Create(existingMessage) }
        };
        var newMessage = CreateMessage(ChatMessageRole.User, "How are you?");

        // Act
        var newState = ApplyAction(state, new ChatActions.UserMessageAdded(newMessage));

        // Assert
        newState.Chat.Messages.Count.ShouldBe(2);
        newState.Chat.Messages[0].ShouldBe(existingMessage);
        newState.Chat.Messages[1].ShouldBe(newMessage);
    }

    #endregion

    #region AssistantTypingStarted Tests

    [Fact]
    public void AssistantTypingStarted_ShouldSetIsTypingTrue()
    {
        // Arrange
        var state = new TestState
        {
            Chat = new ChatState { IsTyping = false }
        };

        // Act
        var newState = ApplyAction(state, new ChatActions.AssistantTypingStarted());

        // Assert
        newState.Chat.IsTyping.ShouldBeTrue();
    }

    [Fact]
    public void AssistantTypingStarted_ShouldNotAffectMessages()
    {
        // Arrange
        var message = CreateMessage(ChatMessageRole.User, "Hello");
        var state = new TestState
        {
            Chat = new ChatState { Messages = ImmutableList.Create(message) }
        };

        // Act
        var newState = ApplyAction(state, new ChatActions.AssistantTypingStarted());

        // Assert
        newState.Chat.Messages.Count.ShouldBe(1);
    }

    #endregion

    #region AssistantMessageReceived Tests

    [Fact]
    public void AssistantMessageReceived_ShouldAddMessageAndStopTyping()
    {
        // Arrange
        var state = new TestState
        {
            Chat = new ChatState { IsTyping = true }
        };
        var message = CreateMessage(ChatMessageRole.Assistant, "Hello!");

        // Act
        var newState = ApplyAction(state, new ChatActions.AssistantMessageReceived(message));

        // Assert
        newState.Chat.Messages.Count.ShouldBe(1);
        newState.Chat.Messages[0].ShouldBe(message);
        newState.Chat.IsTyping.ShouldBeFalse();
    }

    [Fact]
    public void AssistantMessageReceived_ShouldClearStreamingMessageId()
    {
        // Arrange
        var state = new TestState
        {
            Chat = new ChatState
            {
                IsTyping = true,
                StreamingMessageId = Guid.NewGuid()
            }
        };
        var message = CreateMessage(ChatMessageRole.Assistant, "Response");

        // Act
        var newState = ApplyAction(state, new ChatActions.AssistantMessageReceived(message));

        // Assert
        newState.Chat.StreamingMessageId.ShouldBeNull();
    }

    #endregion

    #region ChatCleared Tests

    [Fact]
    public void ChatCleared_ShouldClearAllMessages()
    {
        // Arrange
        var messages = ImmutableList.Create(
            CreateMessage(ChatMessageRole.User, "Hello"),
            CreateMessage(ChatMessageRole.Assistant, "Hi")
        );
        var state = new TestState
        {
            Chat = new ChatState { Messages = messages }
        };

        // Act
        var newState = ApplyAction(state, new ChatActions.ChatCleared());

        // Assert
        newState.Chat.Messages.ShouldBeEmpty();
    }

    [Fact]
    public void ChatCleared_ShouldResetIsTypingAndStreamingMessageId()
    {
        // Arrange
        var state = new TestState
        {
            Chat = new ChatState
            {
                IsTyping = true,
                StreamingMessageId = Guid.NewGuid()
            }
        };

        // Act
        var newState = ApplyAction(state, new ChatActions.ChatCleared());

        // Assert
        newState.Chat.IsTyping.ShouldBeFalse();
        newState.Chat.StreamingMessageId.ShouldBeNull();
    }

    #endregion

    #region StreamingMessageStarted Tests

    [Fact]
    public void StreamingMessageStarted_ShouldAddEmptyMessageAndSetStreamingId()
    {
        // Arrange
        var state = new TestState();
        var messageId = Guid.NewGuid();

        // Act
        var newState = ApplyAction(state, new ChatActions.StreamingMessageStarted(messageId));

        // Assert
        newState.Chat.Messages.Count.ShouldBe(1);
        newState.Chat.Messages[0].Id.ShouldBe(messageId);
        newState.Chat.Messages[0].Content.ShouldBe(string.Empty);
        newState.Chat.Messages[0].Role.ShouldBe(ChatMessageRole.Assistant);
        newState.Chat.StreamingMessageId.ShouldBe(messageId);
    }

    [Fact]
    public void StreamingMessageStarted_ShouldPreserveExistingMessages()
    {
        // Arrange
        var existingMessage = CreateMessage(ChatMessageRole.User, "Hello");
        var state = new TestState
        {
            Chat = new ChatState { Messages = ImmutableList.Create(existingMessage) }
        };
        var messageId = Guid.NewGuid();

        // Act
        var newState = ApplyAction(state, new ChatActions.StreamingMessageStarted(messageId));

        // Assert
        newState.Chat.Messages.Count.ShouldBe(2);
        newState.Chat.Messages[0].ShouldBe(existingMessage);
    }

    [Fact]
    public void StreamingMessageStarted_ShouldSetContextNoteTitles()
    {
        // Arrange
        var state = new TestState();
        var messageId = Guid.NewGuid();
        var titles = ImmutableList.Create("Note 1", "Note 2");

        // Act
        var newState = ApplyAction(state, new ChatActions.StreamingMessageStarted(messageId, titles));

        // Assert
        newState.Chat.Messages.Count.ShouldBe(1);
        newState.Chat.Messages[0].ContextNoteTitles.ShouldNotBeNull();
        newState.Chat.Messages[0].ContextNoteTitles!.Count.ShouldBe(2);
        newState.Chat.Messages[0].ContextNoteTitles![0].ShouldBe("Note 1");
        newState.Chat.Messages[0].ContextNoteTitles![1].ShouldBe("Note 2");
    }

    [Fact]
    public void StreamingMessageStarted_WhenNoContextNotes_ShouldHaveNullContextNoteTitles()
    {
        // Arrange
        var state = new TestState();
        var messageId = Guid.NewGuid();

        // Act
        var newState = ApplyAction(state, new ChatActions.StreamingMessageStarted(messageId));

        // Assert
        newState.Chat.Messages[0].ContextNoteTitles.ShouldBeNull();
    }

    #endregion

    #region StreamingChunkReceived Tests

    [Fact]
    public void StreamingChunkReceived_ShouldAppendChunkToMessage()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ChatMessage(messageId, "Hello", ChatMessageRole.Assistant, DateTimeOffset.UtcNow);
        var state = new TestState
        {
            Chat = new ChatState
            {
                Messages = ImmutableList.Create(message),
                StreamingMessageId = messageId,
                IsTyping = true
            }
        };

        // Act
        var newState = ApplyAction(state, new ChatActions.StreamingChunkReceived(messageId, " World"));

        // Assert
        newState.Chat.Messages[0].Content.ShouldBe("Hello World");
    }

    [Fact]
    public void StreamingChunkReceived_ShouldSetIsTypingFalse()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ChatMessage(messageId, "", ChatMessageRole.Assistant, DateTimeOffset.UtcNow);
        var state = new TestState
        {
            Chat = new ChatState
            {
                Messages = ImmutableList.Create(message),
                StreamingMessageId = messageId,
                IsTyping = true
            }
        };

        // Act
        var newState = ApplyAction(state, new ChatActions.StreamingChunkReceived(messageId, "Hi"));

        // Assert
        newState.Chat.IsTyping.ShouldBeFalse();
    }

    [Fact]
    public void StreamingChunkReceived_WhenMessageNotFound_ShouldReturnUnchangedState()
    {
        // Arrange
        var state = new TestState
        {
            Chat = new ChatState { Messages = ImmutableList<ChatMessage>.Empty }
        };

        // Act
        var newState = ApplyAction(state, new ChatActions.StreamingChunkReceived(Guid.NewGuid(), "chunk"));

        // Assert
        newState.Chat.Messages.ShouldBeEmpty();
    }

    [Fact]
    public void StreamingChunkReceived_ShouldOnlyUpdateMatchingMessage()
    {
        // Arrange
        var messageId1 = Guid.NewGuid();
        var messageId2 = Guid.NewGuid();
        var message1 = new ChatMessage(messageId1, "First", ChatMessageRole.User, DateTimeOffset.UtcNow);
        var message2 = new ChatMessage(messageId2, "Second", ChatMessageRole.Assistant, DateTimeOffset.UtcNow);
        var state = new TestState
        {
            Chat = new ChatState
            {
                Messages = ImmutableList.Create(message1, message2),
                StreamingMessageId = messageId2
            }
        };

        // Act
        var newState = ApplyAction(state, new ChatActions.StreamingChunkReceived(messageId2, " chunk"));

        // Assert
        newState.Chat.Messages[0].Content.ShouldBe("First");
        newState.Chat.Messages[1].Content.ShouldBe("Second chunk");
    }

    #endregion

    #region StreamingMessageCompleted Tests

    [Fact]
    public void StreamingMessageCompleted_ShouldClearStreamingMessageId()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var state = new TestState
        {
            Chat = new ChatState { StreamingMessageId = messageId }
        };

        // Act
        var newState = ApplyAction(state, new ChatActions.StreamingMessageCompleted(messageId));

        // Assert
        newState.Chat.StreamingMessageId.ShouldBeNull();
    }

    [Fact]
    public void StreamingMessageCompleted_ShouldNotAffectMessages()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ChatMessage(messageId, "Complete", ChatMessageRole.Assistant, DateTimeOffset.UtcNow);
        var state = new TestState
        {
            Chat = new ChatState
            {
                Messages = ImmutableList.Create(message),
                StreamingMessageId = messageId
            }
        };

        // Act
        var newState = ApplyAction(state, new ChatActions.StreamingMessageCompleted(messageId));

        // Assert
        newState.Chat.Messages.Count.ShouldBe(1);
        newState.Chat.Messages[0].Content.ShouldBe("Complete");
    }

    #endregion

    #region ToolCallStarted Tests

    [Fact]
    public void ToolCallStarted_AddsActivityToMessage()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var message = new ChatMessage(messageId, "", ChatMessageRole.Assistant, DateTimeOffset.UtcNow);
        var state = new TestState
        {
            Chat = new ChatState { Messages = ImmutableList.Create(message) }
        };
        var activity = new ToolCallActivity("SearchNotes", "Searched notes", false);

        // Act
        var newState = ApplyAction(state, new ChatActions.ToolCallStarted(messageId, activity));

        // Assert
        newState.Chat.Messages[0].ToolCalls.ShouldNotBeNull();
        newState.Chat.Messages[0].ToolCalls!.Count.ShouldBe(1);
        newState.Chat.Messages[0].ToolCalls![0].FunctionName.ShouldBe("SearchNotes");
        newState.Chat.Messages[0].ToolCalls![0].IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public void ToolCallStarted_DoesNothing_WhenMessageNotFound()
    {
        // Arrange
        var state = new TestState
        {
            Chat = new ChatState { Messages = ImmutableList<ChatMessage>.Empty }
        };
        var activity = new ToolCallActivity("SearchNotes", "Searched notes", false);

        // Act
        var newState = ApplyAction(state, new ChatActions.ToolCallStarted(Guid.NewGuid(), activity));

        // Assert
        newState.Chat.Messages.ShouldBeEmpty();
    }

    [Fact]
    public void ToolCallStarted_WhenSameFunctionCalledAgain_ResetsExistingToRunning()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var completedActivity = new ToolCallActivity("GetNoteContent", "Read note", true);
        var message = new ChatMessage(messageId, "", ChatMessageRole.Assistant, DateTimeOffset.UtcNow,
            ToolCalls: ImmutableList.Create(completedActivity));
        var state = new TestState
        {
            Chat = new ChatState { Messages = ImmutableList.Create(message) }
        };
        var newActivity = new ToolCallActivity("GetNoteContent", "Read note", false);

        // Act
        var newState = ApplyAction(state, new ChatActions.ToolCallStarted(messageId, newActivity));

        // Assert - no duplicate added, existing entry reset to running
        newState.Chat.Messages[0].ToolCalls!.Count.ShouldBe(1);
        newState.Chat.Messages[0].ToolCalls![0].IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public void ToolCallStarted_DifferentFunctions_AddsNewEntry()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var existingActivity = new ToolCallActivity("SearchNotes", "Searched notes", false);
        var message = new ChatMessage(messageId, "", ChatMessageRole.Assistant, DateTimeOffset.UtcNow,
            ToolCalls: ImmutableList.Create(existingActivity));
        var state = new TestState
        {
            Chat = new ChatState { Messages = ImmutableList.Create(message) }
        };
        var newActivity = new ToolCallActivity("GetNoteContent", "Read note", false);

        // Act
        var newState = ApplyAction(state, new ChatActions.ToolCallStarted(messageId, newActivity));

        // Assert - different function name adds new entry
        newState.Chat.Messages[0].ToolCalls!.Count.ShouldBe(2);
    }

    #endregion

    #region ToolCallCompleted Tests

    [Fact]
    public void ToolCallCompleted_MarksActivityAsCompleted()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var activity = new ToolCallActivity("SearchNotes", "Searched notes", false);
        var message = new ChatMessage(messageId, "", ChatMessageRole.Assistant, DateTimeOffset.UtcNow,
            ToolCalls: ImmutableList.Create(activity));
        var state = new TestState
        {
            Chat = new ChatState { Messages = ImmutableList.Create(message) }
        };

        // Act
        var newState = ApplyAction(state, new ChatActions.ToolCallCompleted(messageId, "SearchNotes"));

        // Assert
        newState.Chat.Messages[0].ToolCalls.ShouldNotBeNull();
        newState.Chat.Messages[0].ToolCalls![0].IsCompleted.ShouldBeTrue();
    }

    [Fact]
    public void ToolCallCompleted_DoesNothing_WhenActivityNotFound()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var activity = new ToolCallActivity("SearchNotes", "Searched notes", false);
        var message = new ChatMessage(messageId, "", ChatMessageRole.Assistant, DateTimeOffset.UtcNow,
            ToolCalls: ImmutableList.Create(activity));
        var state = new TestState
        {
            Chat = new ChatState { Messages = ImmutableList.Create(message) }
        };

        // Act
        var newState = ApplyAction(state, new ChatActions.ToolCallCompleted(messageId, "CreateNote"));

        // Assert
        newState.Chat.Messages[0].ToolCalls![0].IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public void ToolCallCompleted_DoesNothing_WhenMessageNotFound()
    {
        // Arrange
        var state = new TestState
        {
            Chat = new ChatState { Messages = ImmutableList<ChatMessage>.Empty }
        };

        // Act
        var newState = ApplyAction(state, new ChatActions.ToolCallCompleted(Guid.NewGuid(), "SearchNotes"));

        // Assert
        newState.Chat.Messages.ShouldBeEmpty();
    }

    #endregion

    private static ChatMessage CreateMessage(ChatMessageRole role, string content)
    {
        return new ChatMessage(Guid.NewGuid(), content, role, DateTimeOffset.UtcNow);
    }

    private sealed record TestState
    {
        public ChatState Chat { get; init; } = new();
    }
}
