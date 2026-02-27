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
    private readonly HashSet<Guid> _expandedNoteContextIds = [];
    private readonly HashSet<Guid> _expandedToolCallIds = [];
    private ElementReference _messagesContainer;
    private string _inputText = string.Empty;
    private bool _isTyping;
    private bool _isStreaming;
    private Guid? _streamingMessageId;

    protected override void OnInitialized()
    {
        // Subscribe to messages from state and sync with local list
        StateProvider.StateObservable
            .Select(s => s.Chat.Messages)
            .DistinctUntilChanged()
            .TakeUntilDisappearing(this)
            .Subscribe(messages =>
            {
                _messages.Clear();
                foreach (var msg in messages)
                {
                    _messages.Add(msg);
                }
                StateHasChanged();
                _ = ScrollToBottom();
            });

        // Subscribe to typing state
        StateProvider.StateObservable
            .Select(s => s.Chat.IsTyping)
            .DistinctUntilChanged()
            .TakeUntilDisappearing(this)
            .Subscribe(isTyping =>
            {
                _isTyping = isTyping;
                StateHasChanged();
                if (isTyping)
                {
                    _ = ScrollToBottom();
                }
            });

        // Subscribe to streaming state
        StateProvider.StateObservable
            .Select(s => s.Chat.StreamingMessageId)
            .DistinctUntilChanged()
            .TakeUntilDisappearing(this)
            .Subscribe(streamingMessageId =>
            {
                _streamingMessageId = streamingMessageId;
                _isStreaming = streamingMessageId.HasValue;
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

        // Use streaming command for real-time updates
        await Mediator.Send(new ChatCommands.SendMessageStreaming(message));
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e is { Key: "Enter", ShiftKey: false })
        {
            await SendMessage();
        }
    }

    private async Task ClearChat()
    {
        await Mediator.Send(new ChatCommands.ClearChat());
    }

    private void ToggleNoteContext(Guid messageId)
    {
        if (!_expandedNoteContextIds.Add(messageId))
        {
            _expandedNoteContextIds.Remove(messageId);
        }
    }

    private void ToggleToolCalls(Guid messageId)
    {
        if (!_expandedToolCallIds.Add(messageId))
        {
            _expandedToolCallIds.Remove(messageId);
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
