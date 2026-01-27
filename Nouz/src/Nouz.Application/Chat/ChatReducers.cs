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
                    DateTimeOffset.UtcNow);
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
            .ToList();
    }
}
