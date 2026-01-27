using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Nouz.Application.Chat;
using Nouz.Application.Preferences;
using Nouz.Application.Store;
using Nouz.Domain.Entities;
using Nouz.Infrastructure.Chat.Plugins;
using IPreferences = Nouz.Application.Preferences.IPreferences;

namespace Nouz.Infrastructure.Chat;

internal sealed class ChatService : IChatService
{
    private const string DefaultChatModel = "gpt-4o-mini";
    private const string BaseSystemPrompt = """
        You are a helpful assistant integrated into a note-taking application called Nouz.
        Be concise and helpful. You can help users with their notes, answer questions, and provide information.

        You have access to note management functions that allow you to:
        - List and create notebooks
        - Create, read, edit, and delete notes
        - Search for notes across all notebooks

        When creating or editing notes, use structured blocks. Available block types:
        - h1, h2, h3, h4: Headings (use h1 for main title)
        - paragraph: Regular text
        - listitem: Bullet point
        - todoitem: Checkbox/task item
        - agendaitem: Agenda item
        - code: Code block
        - quote: Block quote
        - decision: Decision block (for recording decisions)
        - warning: Warning/alert block

        IMPORTANT RULES:
        1. When the user asks to create a note but doesn't specify which notebook, first use ListNotebooks to show available options and ask which one to use.
        2. For EDIT operations: First use GetNoteContent to read the current note, describe the proposed changes clearly to the user, and ask "Should I apply these changes?" Only call EditNote after the user confirms (e.g., "yes", "go ahead", "do it").
        3. For DELETE operations: First use GetNoteContent to show what will be deleted, ask "Are you sure you want to delete this note?" Only call DeleteNote after the user explicitly confirms.
        4. Always confirm successful operations to the user.
        """;

    private readonly IPreferences _preferences;
    private readonly INoteContextService _noteContextService;
    private readonly IStateProvider _stateProvider;
    private readonly NoteManagementPlugin _noteManagementPlugin;

    public ChatService(
        IPreferences preferences,
        INoteContextService noteContextService,
        IStateProvider stateProvider,
        NoteManagementPlugin noteManagementPlugin)
    {
        _preferences = preferences;
        _noteContextService = noteContextService;
        _stateProvider = stateProvider;
        _noteManagementPlugin = noteManagementPlugin;
    }

    private async Task<string> GetChatModel()
    {
        var model = await _preferences.Get(PreferenceKeys.OpenAiChatModel).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(model) ? DefaultChatModel : model;
    }

    public async Task<string> GetResponseAsync(
        string message,
        ImmutableList<ChatMessage> conversationHistory,
        CancellationToken cancellationToken = default)
    {
        var apiKey = await _preferences.Get(PreferenceKeys.OpenAiApiKey).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "Please configure your OpenAI API key in Settings to use the assistant.";
        }

        var kernel = CreateKernel(apiKey, await GetChatModel().ConfigureAwait(false));
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = BuildChatHistory(conversationHistory, message);

        // Enable automatic function calling
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var response = await chatService.GetChatMessageContentAsync(
            chatHistory,
            executionSettings,
            kernel,
            cancellationToken).ConfigureAwait(false);

        return response.Content ?? "I'm sorry, I couldn't generate a response.";
    }

    public async IAsyncEnumerable<string> GetStreamingResponseAsync(
        string message,
        ImmutableList<ChatMessage> conversationHistory,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var apiKey = await _preferences.Get(PreferenceKeys.OpenAiApiKey).ConfigureAwait(false);

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

        var kernel = CreateKernel(apiKey, await GetChatModel().ConfigureAwait(false));
        var chatService = kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = BuildChatHistory(conversationHistory, message, relevantNotes);

        // Enable automatic function calling
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(
            chatHistory,
            executionSettings,
            kernel,
            cancellationToken).ConfigureAwait(false))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                yield return chunk.Content;
            }
        }
    }

    private Kernel CreateKernel(string apiKey, string modelId)
    {
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(
            modelId: modelId,
            apiKey: apiKey);

        var kernel = builder.Build();

        // Register the note management plugin for function calling
        kernel.Plugins.AddFromObject(_noteManagementPlugin, "NoteManagement");

        return kernel;
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
