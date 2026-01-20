using System.Reactive.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Nouz.Application.Chat;
using Nouz.Extensions;

namespace Nouz.Components.Chat;

public partial class ChatBot
{
    private readonly List<ChatMessage> _messages = [];
    private ElementReference _messagesContainer;
    private string _inputText = string.Empty;
    private bool _isTyping;


    /// <summary>
    /// Adds a user message to the chat.
    /// </summary>
    public void AddUserMessage(string content)
    {
        _messages.Add(new ChatMessage(content, ChatMessageRole.User, DateTime.Now));
        StateHasChanged();
        _ = ScrollToBottom();
    }

    /// <summary>
    /// Adds an assistant message to the chat.
    /// </summary>
    public void AddAssistantMessage(string content)
    {
        _messages.Add(new ChatMessage(content, ChatMessageRole.Assistant, DateTime.Now));
        StateHasChanged();
        _ = ScrollToBottom();
    }

    /// <summary>
    /// Sets the typing indicator visibility.
    /// </summary>
    public void SetTyping(bool isTyping)
    {
        _isTyping = isTyping;
        StateHasChanged();
        if (isTyping)
        {
            _ = ScrollToBottom();
        }
    }

    /// <summary>
    /// Clears all messages from the chat.
    /// </summary>
    public void ClearMessages()
    {
        _messages.Clear();
        StateHasChanged();
    }

    protected override void OnInitialized()
    {
        StateProvider.StateObservable
            .Select(s => s.Chat.Messages)
            .Where(m => m.Count > 0)
            .DistinctUntilChanged()
            .TakeUntilDisappearing(this)
            .Subscribe(messages =>
            {
                var lastMessage = messages.Last();

                switch (lastMessage.Role)
                {
                    case Nouz.Application.Chat.ChatMessageRole.User:
                        AddUserMessage(lastMessage.Content);
                        break;
                    case Nouz.Application.Chat.ChatMessageRole.Assistant:
                        AddAssistantMessage(lastMessage.Content);
                        break;
                }

                StateHasChanged();
            });

        StateProvider.StateObservable
            .Select(s => s.Chat.IsTyping)
            .DistinctUntilChanged()
            .TakeUntilDisappearing(this)
            .Subscribe(isTyping =>
            {
                SetTyping(isTyping);
                StateHasChanged();
            });
    }

    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(_inputText))
        {
            return;
        }

        var message = _inputText.Trim();
        _inputText = string.Empty;

        await Mediator.Send(new ChatCommands.SendMessage(message));
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            await SendMessage();
        }
    }

    private async Task ScrollToBottom()
    {
        try
        {
            await Task.Delay(50); // Small delay to ensure DOM is updated
            await JsRuntime.InvokeVoidAsync("nouz.scrollToBottom", _messagesContainer);
        }
        catch
        {
            // Ignore scroll errors
        }
    }
}
