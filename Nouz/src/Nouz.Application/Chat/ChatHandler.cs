using Mediator;
using Nouz.Application.Logger;
using Nouz.Application.Notifications;
using Nouz.Application.Store;

namespace Nouz.Application.Chat;

internal sealed class ChatHandler :
    ICommandHandler<ChatCommands.SendMessage>,
    ICommandHandler<ChatCommands.ClearChat>
{
    private readonly IMediator _mediator;
    private readonly IChatService _chatService;
    private readonly IStateProvider _stateProvider;
    private readonly IActionDispatcher _actionDispatcher;
    private readonly ILoggerAdapter<ChatHandler> _logger;

    public ChatHandler(
        IMediator mediator,
        IChatService chatService,
        IStateProvider stateProvider,
        IActionDispatcher actionDispatcher,
        ILoggerAdapter<ChatHandler> logger)
    {
        _mediator = mediator;
        _chatService = chatService;
        _stateProvider = stateProvider;
        _actionDispatcher = actionDispatcher;
        _logger = logger;
    }

    public async ValueTask<Unit> Handle(ChatCommands.SendMessage command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Sending chat message");

        try
        {
            // Create and add user message to state
            var userMessage = new ChatMessage(
                Guid.NewGuid(),
                command.Content,
                ChatMessageRole.User,
                DateTimeOffset.UtcNow);

            await _actionDispatcher.Dispatch(new ChatActions.UserMessageAdded(userMessage)).ConfigureAwait(false);

            // Show typing indicator
            await _actionDispatcher.Dispatch(new ChatActions.AssistantTypingStarted()).ConfigureAwait(false);

            // Get conversation history for context
            var conversationHistory = _stateProvider.State.Chat.Messages;

            // Get response from chat service
            var response = await _chatService.GetResponseAsync(
                command.Content,
                conversationHistory,
                cancellationToken).ConfigureAwait(false);

            // Create and add assistant message to state
            var assistantMessage = new ChatMessage(
                Guid.NewGuid(),
                response,
                ChatMessageRole.Assistant,
                DateTimeOffset.UtcNow);

            await _actionDispatcher.Dispatch(new ChatActions.AssistantMessageReceived(assistantMessage)).ConfigureAwait(false);

            _logger.LogDebug("Chat message sent and response received");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send chat message");

            // Clear typing indicator on error
            await _actionDispatcher.Dispatch(new ChatActions.AssistantMessageReceived(
                new ChatMessage(
                    Guid.NewGuid(),
                    "Sorry, I encountered an error processing your message. Please try again.",
                    ChatMessageRole.Assistant,
                    DateTimeOffset.UtcNow))).ConfigureAwait(false);

            await _mediator.Send(new NotificationCommands.ShowNotification(
                "Error",
                "Failed to get a response from the assistant.",
                NotificationSeverity.Error), cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(ChatCommands.ClearChat command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Clearing chat history");

        await _actionDispatcher.Dispatch(new ChatActions.ChatCleared()).ConfigureAwait(false);

        _logger.LogDebug("Chat history cleared");

        return Unit.Value;
    }
}
