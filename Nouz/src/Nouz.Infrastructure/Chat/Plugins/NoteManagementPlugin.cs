using Microsoft.SemanticKernel;
using Nouz.Application.Chat;
using Nouz.Application.Chat.Models;
using Nouz.Application.Logger;
using Nouz.Domain.Entities;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;

namespace Nouz.Infrastructure.Chat.Plugins;

/// <summary>
/// Semantic Kernel plugin for managing notes and notebooks.
/// Provides functions that the AI can call to create, read, update, and delete notes.
/// </summary>
internal sealed class NoteManagementPlugin
{
    private readonly INoteManagementService _noteManagementService;
    private readonly ILoggerAdapter<NoteManagementPlugin> _logger;

    public NoteManagementPlugin(
        INoteManagementService noteManagementService,
        ILoggerAdapter<NoteManagementPlugin> logger)
    {
        _noteManagementService = noteManagementService;
        _logger = logger;
    }

    [KernelFunction("ListNotebooks")]
    [Description("Lists all available notebooks with their IDs and names. Use this to find out which notebooks exist before creating or finding notes.")]
    public async Task<string> ListNotebooksAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AI calling ListNotebooks");

        var notebooks = await _noteManagementService.GetAllNotebooksAsync(cancellationToken).ConfigureAwait(false);

        if (notebooks.Count == 0)
        {
            return "No notebooks found. You can create a new notebook using CreateNotebook.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Found {notebooks.Count} notebook(s):");
        foreach (var notebook in notebooks)
        {
            sb.AppendLine($"- {notebook.Name} (ID: {notebook.Id})");
        }

        return sb.ToString();
    }

    [KernelFunction("CreateNotebook")]
    [Description("Creates a new notebook with the specified name.")]
    public async Task<string> CreateNotebookAsync(
        [Description("The name for the new notebook")] string name,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AI calling CreateNotebook with name '{Name}'", name);

        if (string.IsNullOrWhiteSpace(name))
        {
            return "Error: Notebook name cannot be empty.";
        }

        try
        {
            var notebook = await _noteManagementService.CreateNotebookAsync(name, cancellationToken).ConfigureAwait(false);
            return $"Successfully created notebook \"{notebook.Name}\" (ID: {notebook.Id}).";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notebook");
            return $"Error creating notebook: {ex.Message}";
        }
    }

    [KernelFunction("GetNotebookNotes")]
    [Description("Gets all notes in a specific notebook with preview content. Use this to see what notes exist in a notebook.")]
    public async Task<string> GetNotebookNotesAsync(
        [Description("The ID of the notebook to get notes from")] Guid notebookId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AI calling GetNotebookNotes for notebook '{NotebookId}'", notebookId);

        try
        {
            var notes = await _noteManagementService.GetNotebookNotesAsync(notebookId, cancellationToken).ConfigureAwait(false);

            if (notes.Count == 0)
            {
                return "This notebook has no notes yet.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Found {notes.Count} note(s):");

            foreach (var note in notes.OrderByDescending(n => n.LastModifiedAt).Take(20))
            {
                var preview = GetNotePreview(note, 100);
                var title = GetNoteTitle(note) ?? "(Untitled)";
                sb.AppendLine($"- {title} (ID: {note.Id})");
                if (!string.IsNullOrWhiteSpace(preview))
                {
                    sb.AppendLine($"  Preview: {preview}");
                }
                sb.AppendLine($"  Modified: {note.LastModifiedAt:g}");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notebook notes");
            return $"Error getting notes: {ex.Message}";
        }
    }

    [KernelFunction("CreateNote")]
    [Description("Creates a new note in the specified notebook with structured content. The blocks parameter must be ONLY a valid JSON array with no additional text before or after. Block types: paragraph, h1, h2, h3, h4, listitem, todoitem, agendaitem, code, quote, decision, warning.")]
    public async Task<string> CreateNoteAsync(
        [Description("The ID of the notebook to create the note in")] Guid notebookId,
        [Description("ONLY a JSON array of blocks with no other text. Example: [{\"type\":\"h1\",\"content\":\"Title\"},{\"type\":\"paragraph\",\"content\":\"Content\"}]")] string blocks,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AI calling CreateNote in notebook '{NotebookId}'", notebookId);

        try
        {
            var blockDefinitions = ParseBlocks(blocks);

            if (blockDefinitions.Count == 0)
            {
                return "Error: At least one block is required.";
            }

            var note = await _noteManagementService.CreateNoteAsync(notebookId, blockDefinitions, cancellationToken).ConfigureAwait(false);
            var title = GetNoteTitle(note) ?? "New note";

            return $"Successfully created note \"{title}\" (ID: {note.Id}) with {note.Blocks.Count} block(s).";
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse blocks JSON");
            return $"Error: Invalid blocks JSON format. Expected array of objects with 'type' and 'content' properties. Details: {ex.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create note");
            return $"Error creating note: {ex.Message}";
        }
    }

    [KernelFunction("GetNoteContent")]
    [Description("Gets the full content of a specific note including all blocks. Use this to read a note before editing or to show the user what a note contains.")]
    public async Task<string> GetNoteContentAsync(
        [Description("The ID of the note to read")] Guid noteId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AI calling GetNoteContent for note '{NoteId}'", noteId);

        try
        {
            var note = await _noteManagementService.GetNoteAsync(noteId, cancellationToken).ConfigureAwait(false);

            if (note is null)
            {
                return $"Note with ID {noteId} not found.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Note ID: {note.Id}");
            sb.AppendLine($"Created: {note.CreatedAt:g}");
            sb.AppendLine($"Modified: {note.LastModifiedAt:g}");
            sb.AppendLine($"Blocks ({note.Blocks.Count}):");
            sb.AppendLine();

            foreach (var block in note.Blocks.OrderBy(b => b.Order))
            {
                switch (block.Type)
                {
                    case BlockType.AgendaItem:
                        sb.AppendLine($"[Agenda item] - {block.Content}");
                        break;
                    case BlockType.Paragraph:
                        sb.AppendLine(block.Content);
                        break;
                    case BlockType.H1:
                        sb.AppendLine($"# {block.Content}");
                        break;
                    case BlockType.H2:
                        sb.AppendLine($"## {block.Content}");
                        break;
                    case BlockType.H3:
                        sb.AppendLine($"### {block.Content}");
                        break;
                    case BlockType.H4:
                        sb.AppendLine($"#### {block.Content}");
                        break;
                    case BlockType.ListItem:
                        sb.AppendLine($"- {block.Content}");
                        break;
                    case BlockType.TodoItem:
                        if (block.Metadata.TryGetValue("checked", out var checkedValue) && bool.TryParse(checkedValue!.ToString(), out var isChecked))
                        {
                            var status = isChecked ? "x" : " ";
                            sb.AppendLine($"- [{status}] {block.Content}");
                        }
                        else
                        {
                            sb.AppendLine($"- [ ] {block.Content}");
                        }
                        break;
                    case BlockType.Code:
                        sb.AppendLine($"```{block.Content}```");
                        break;
                    case BlockType.Quote:
                        sb.AppendLine($"> {block.Content}");
                        break;
                    case BlockType.Decision:
                        sb.AppendLine($"[Decision] - {block.Content}");
                        break;
                    case BlockType.Warning:
                        sb.AppendLine($"[Warning] - {block.Content}");
                        break;
                    case BlockType.Idea:
                        sb.AppendLine($"[Idea] - {block.Content}");
                        break;
                    case BlockType.Mermaid:
                        sb.AppendLine($"```mermaid\n{block.Content}\n```");
                        break;
                    case BlockType.Table:
                        sb.AppendLine(block.Content);
                        break;
                    case BlockType.Divider:
                        break;
                    case BlockType.Image:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get note content");
            return $"Error getting note: {ex.Message}";
        }
    }

    [KernelFunction("EditNote")]
    [Description("Updates an existing note with new content. IMPORTANT: Only call this after the user has confirmed they want to make the changes. First use GetNoteContent to show the current content, describe your proposed changes, and ask for confirmation.")]
    public async Task<string> EditNoteAsync(
        [Description("The ID of the note to edit")] Guid noteId,
        [Description("ONLY a JSON array of the complete new block structure with no other text. Example: [{\"type\":\"h1\",\"content\":\"Title\"},{\"type\":\"paragraph\",\"content\":\"Content\"}]")] string newBlocks,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AI calling EditNote for note '{NoteId}'", noteId);

        try
        {
            var blockDefinitions = ParseBlocks(newBlocks);

            if (blockDefinitions.Count == 0)
            {
                return "Error: At least one block is required.";
            }

            var note = await _noteManagementService.EditNoteAsync(noteId, blockDefinitions, cancellationToken).ConfigureAwait(false);
            var title = GetNoteTitle(note) ?? "Note";

            return $"Successfully updated \"{title}\" with {note.Blocks.Count} block(s).";
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse blocks JSON");
            return $"Error: Invalid blocks JSON format. Details: {ex.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to edit note");
            return $"Error editing note: {ex.Message}";
        }
    }

    [KernelFunction("DeleteNote")]
    [Description("Permanently deletes a note. IMPORTANT: Only call this after the user has explicitly confirmed deletion. First use GetNoteContent to show what will be deleted and ask for confirmation.")]
    public async Task<string> DeleteNoteAsync(
        [Description("The ID of the note to delete")] Guid noteId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AI calling DeleteNote for note '{NoteId}'", noteId);

        try
        {
            // First get the note to show what was deleted
            var note = await _noteManagementService.GetNoteAsync(noteId, cancellationToken).ConfigureAwait(false);

            if (note is null)
            {
                return $"Note with ID {noteId} not found.";
            }

            var title = GetNoteTitle(note) ?? "Note";

            await _noteManagementService.DeleteNoteAsync(noteId, cancellationToken).ConfigureAwait(false);

            return $"Successfully deleted \"{title}\".";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete note");
            return $"Error deleting note: {ex.Message}";
        }
    }

    [KernelFunction("SearchNotes")]
    [Description("Searches for notes matching the query across all notebooks using semantic search. Returns the most relevant notes.")]
    public async Task<string> SearchNotesAsync(
        [Description("The search query to find relevant notes")] string query,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AI calling SearchNotes with query '{Query}'", query);

        if (string.IsNullOrWhiteSpace(query))
        {
            return "Error: Search query cannot be empty.";
        }

        try
        {
            var notes = await _noteManagementService.SearchNotesAsync(query, cancellationToken).ConfigureAwait(false);

            if (notes.Count == 0)
            {
                return "No notes found matching your search.";
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Found {notes.Count} relevant note(s):");

            foreach (var note in notes)
            {
                var preview = GetNotePreview(note, 150);
                var title = GetNoteTitle(note) ?? "(Untitled)";
                sb.AppendLine($"- {title} (ID: {note.Id})");
                if (!string.IsNullOrWhiteSpace(preview))
                {
                    sb.AppendLine($"  {preview}");
                }
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search notes");
            return $"Error searching notes: {ex.Message}";
        }
    }

    private static List<BlockDefinition> ParseBlocks(string blocksJson)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var json = ExtractJsonArray(blocksJson);
        var definitions = JsonSerializer.Deserialize<List<BlockDefinition>>(json, options);
        return definitions ?? [];
    }

    /// <summary>
    /// Extracts a JSON array from a string that may contain additional text before or after the JSON.
    /// This handles cases where the AI model appends explanatory text to its JSON output.
    /// </summary>
    private static string ExtractJsonArray(string input)
    {
        var startIndex = input.IndexOf('[');
        if (startIndex == -1)
        {
            return input; // No array found, let JSON deserializer handle the error
        }

        var bracketCount = 0;
        for (var i = startIndex; i < input.Length; i++)
        {
            switch (input[i])
            {
                case '[':
                    bracketCount++;
                    break;
                case ']':
                    bracketCount--;
                    if (bracketCount == 0)
                    {
                        return input[startIndex..(i + 1)];
                    }
                    break;
            }
        }

        // Unbalanced brackets, return from start of array to end
        return input[startIndex..];
    }

    private static string? GetNoteTitle(Note note)
    {
        // Try to find an H1 block first
        var h1Block = note.Blocks.FirstOrDefault(b => b.Type == BlockType.H1);
        if (h1Block is not null && !string.IsNullOrWhiteSpace(h1Block.Content))
        {
            return h1Block.Content.Length > 50 ? h1Block.Content[..50] + "..." : h1Block.Content;
        }

        // Fall back to first non-empty block
        var firstBlock = note.Blocks
            .OrderBy(b => b.Order)
            .FirstOrDefault(b => !string.IsNullOrWhiteSpace(b.Content));

        if (firstBlock is not null)
        {
            return firstBlock.Content.Length > 50 ? firstBlock.Content[..50] + "..." : firstBlock.Content;
        }

        return null;
    }

    private static string GetNotePreview(Note note, int maxLength)
    {
        var content = string.Join(" ", note.Blocks
            .OrderBy(b => b.Order)
            .Where(b => !string.IsNullOrWhiteSpace(b.Content))
            .Select(b => b.Content));

        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        return content.Length > maxLength ? content[..maxLength] + "..." : content;
    }
}
