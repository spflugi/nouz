using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Nouz.Application.Chat;
using Nouz.Application.Preferences;
using Nouz.Application.Store;
using Nouz.Domain.Entities;
using IPreferences = Nouz.Application.Preferences.IPreferences;

namespace Nouz.Infrastructure.Chat;

internal sealed class ChatService : IChatService
{
    private const string DefaultChatModel = "gpt-4o-mini";
    private const string BaseSystemPrompt =
        "You are a helpful assistant integrated into a note-taking application. " +
        "Be concise and helpful. You can help users with their notes, answer questions, " +
        "and provide information.";

    private readonly IPreferences _preferences;
    private readonly INoteContextService _noteContextService;
    private readonly IStateProvider _stateProvider;

    public ChatService(
        IPreferences preferences,
        INoteContextService noteContextService,
        IStateProvider stateProvider)
    {
        _preferences = preferences;
        _noteContextService = noteContextService;
        _stateProvider = stateProvider;
    }

    private string GetChatModel()
    {
        var model = _preferences.Get(PreferenceKeys.OpenAiChatModel);
        return string.IsNullOrWhiteSpace(model) ? DefaultChatModel : model;
    }

    public async Task<string> GetResponseAsync(
        string message,
        ImmutableList<ChatMessage> conversationHistory,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _preferences.Get(PreferenceKeys.OpenAiApiKey);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "Please configure your OpenAI API key in Settings to use the assistant.";
        }

        var kernel = CreateKernel(apiKey, GetChatModel());
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = BuildChatHistory(conversationHistory, message);

        var response = await chatService.GetChatMessageContentAsync(
            chatHistory,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return response.Content ?? "I'm sorry, I couldn't generate a response.";
    }

    public async IAsyncEnumerable<string> GetStreamingResponseAsync(
        string message,
        ImmutableList<ChatMessage> conversationHistory,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var apiKey = _preferences.Get(PreferenceKeys.OpenAiApiKey);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            yield return "Please configure your OpenAI API key in Settings to use the assistant.";
            yield break;
        }

        // Get relevant notes for RAG context
        var topN = _stateProvider.State.Settings.TopNRelevantNotes;
        IReadOnlyList<Note> relevantNotes = [];
        if (topN > 0)
        {
            relevantNotes = await _noteContextService.GetRelevantNotesAsync(message, topN, cancellationToken)
                .ConfigureAwait(false);
        }

        var kernel = CreateKernel(apiKey, GetChatModel());
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = BuildChatHistory(conversationHistory, message, relevantNotes);

        await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(
            chatHistory,
            cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                yield return chunk.Content;
            }
        }
    }

    private static Kernel CreateKernel(string apiKey, string modelId)
    {
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(
            modelId: modelId,
            apiKey: apiKey);

        return builder.Build();
    }

    private static ChatHistory BuildChatHistory(
        ImmutableList<ChatMessage> conversationHistory,
        string currentMessage,
        IReadOnlyList<Note>? relevantNotes = null)
    {
        var chatHistory = new ChatHistory();

        var systemPrompt = BuildSystemPrompt(relevantNotes);
        chatHistory.AddSystemMessage(systemPrompt);

        foreach (var msg in conversationHistory)
        {
            switch (msg.Role)
            {
                case ChatMessageRole.User:
                    chatHistory.AddUserMessage(msg.Content);
                    break;
                case ChatMessageRole.Assistant:
                    chatHistory.AddAssistantMessage(msg.Content);
                    break;
            }
        }

        chatHistory.AddUserMessage(currentMessage);

        return chatHistory;
    }

    private static string BuildSystemPrompt(IReadOnlyList<Note>? relevantNotes)
    {
        if (relevantNotes is null || relevantNotes.Count == 0)
        {
            return BaseSystemPrompt;
        }

        var sb = new StringBuilder(BaseSystemPrompt);
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("Here are some relevant notes from the user's notebook that may help you answer their question:");
        sb.AppendLine();

        for (var i = 0; i < relevantNotes.Count; i++)
        {
            var note = relevantNotes[i];
            sb.AppendLine($"--- Note {i + 1} ---");

            foreach (var block in note.Blocks.OrderBy(b => b.Order))
            {
                if (!string.IsNullOrWhiteSpace(block.Content))
                {
                    sb.AppendLine(block.Content);
                }
            }

            sb.AppendLine();
        }

        sb.AppendLine("Use the above notes as context when relevant to the user's question. " +
                      "If the notes don't contain relevant information, rely on your general knowledge.");

        return sb.ToString();
    }
}
