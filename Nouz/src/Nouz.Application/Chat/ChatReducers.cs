using System.Collections.Immutable;
using Nouz.ReduxSimple;

namespace Nouz.Application.Chat;

/// <summary>
/// Reducers for chat state changes.
/// </summary>
public static class ChatReducers
{
    public static IEnumerable<On<T>> Create<T>(Func<T, ChatState> selector) where T : class, new()
    {
        return Reducers.CreateSubReducers(selector)
            .On<ChatActions.UserMessageAdded>((state, action) =>
            {
                var updatedMessages = state.Messages.Add(action.Message);
                return state with { Messages = updatedMessages };
            })
            .On<ChatActions.AssistantTypingStarted>((state, _) =>
                state with { IsTyping = true })
            .On<ChatActions.AssistantMessageReceived>((state, action) =>
            {
                var updatedMessages = state.Messages.Add(action.Message);
                return state with { Messages = updatedMessages, IsTyping = false, StreamingMessageId = null };
            })
            .On<ChatActions.ChatCleared>((state, _) =>
                state with { Messages = [], IsTyping = false, StreamingMessageId = null })
            .On<ChatActions.StreamingMessageStarted>((state, action) =>
            {
                var emptyMessage = new ChatMessage(
                    action.MessageId,
                    string.Empty,
                    ChatMessageRole.Assistant,
                    DateTimeOffset.UtcNow,
                    action.ContextNoteTitles);
                return state with
                {
                    Messages = state.Messages.Add(emptyMessage),
                    // Keep IsTyping true until first chunk arrives
                    StreamingMessageId = action.MessageId
                };
            })
            .On<ChatActions.StreamingChunkReceived>((state, action) =>
            {
                var messageIndex = state.Messages.FindIndex(m => m.Id == action.MessageId);
                if (messageIndex < 0)
                {
                    return state;
                }

                var existingMessage = state.Messages[messageIndex];
                var updatedMessage = existingMessage with
                {
                    Content = existingMessage.Content + action.Chunk
                };

                return state with
                {
                    Messages = state.Messages.SetItem(messageIndex, updatedMessage),
                    IsTyping = false // Turn off typing indicator when first chunk arrives
                };
            })
            .On<ChatActions.StreamingMessageCompleted>((state, _) =>
                state with { StreamingMessageId = null })
            .On<ChatActions.ToolCallStarted>((state, action) =>
            {
                var messageIndex = state.Messages.FindIndex(m => m.Id == action.MessageId);
                if (messageIndex < 0)
                {
                    return state;
                }

                var existingMessage = state.Messages[messageIndex];
                var existingCalls = existingMessage.ToolCalls ?? ImmutableList<ToolCallActivity>.Empty;

                // Deduplicate: if same function is called again, reset it to running instead of adding a duplicate
                var existingIndex = existingCalls.FindIndex(a => a.FunctionName == action.Activity.FunctionName);
                var updatedToolCalls = existingIndex >= 0
                    ? existingCalls.SetItem(existingIndex, existingCalls[existingIndex] with { IsCompleted = false })
                    : existingCalls.Add(action.Activity);

                var updatedMessage = existingMessage with { ToolCalls = updatedToolCalls };

                return state with { Messages = state.Messages.SetItem(messageIndex, updatedMessage) };
            })
            .On<ChatActions.ToolCallCompleted>((state, action) =>
            {
                var messageIndex = state.Messages.FindIndex(m => m.Id == action.MessageId);
                if (messageIndex < 0)
                {
                    return state;
                }

                var existingMessage = state.Messages[messageIndex];
                if (existingMessage.ToolCalls is null)
                {
                    return state;
                }

                // Find the first incomplete activity with this function name
                var activityIndex = existingMessage.ToolCalls.FindIndex(a => a.FunctionName == action.FunctionName && !a.IsCompleted);
                if (activityIndex < 0)
                {
                    return state;
                }

                var updatedActivity = existingMessage.ToolCalls[activityIndex] with { IsCompleted = true };
                var updatedToolCalls = existingMessage.ToolCalls.SetItem(activityIndex, updatedActivity);
                var updatedMessage = existingMessage with { ToolCalls = updatedToolCalls };

                return state with { Messages = state.Messages.SetItem(messageIndex, updatedMessage) };
            })
            .ToList();
    }
}
