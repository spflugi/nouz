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
                return state with { Messages = updatedMessages, IsTyping = false };
            })
            .On<ChatActions.ChatCleared>((state, _) =>
                state with { Messages = [], IsTyping = false })
            .ToList();
    }
}
