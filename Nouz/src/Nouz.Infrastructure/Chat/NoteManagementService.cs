using System.Collections.Immutable;
using Mediator;
using Nouz.Application.Chat;
using Nouz.Application.Chat.Models;
using Nouz.Application.Embeddings;
using Nouz.Application.Logger;
using Nouz.Application.Notes;
using Nouz.Application.Notebooks;
using Nouz.Application.Notifications;
using Nouz.Application.Store;
using Nouz.Domain.Entities;
using Nouz.Domain.Repositories;

namespace Nouz.Infrastructure.Chat;

internal sealed class NoteManagementService : INoteManagementService
{
    private readonly INotebookRepository _notebookRepository;
    private readonly INoteRepository _noteRepository;
    private readonly INoteContextService _noteContextService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IEmbeddingRepository _embeddingRepository;
    private readonly IActionDispatcher _actionDispatcher;
    private readonly IMediator _mediator;
    private readonly ILoggerAdapter<NoteManagementService> _logger;

    public NoteManagementService(
        INotebookRepository notebookRepository,
        INoteRepository noteRepository,
        INoteContextService noteContextService,
        IEmbeddingService embeddingService,
        IEmbeddingRepository embeddingRepository,
        IActionDispatcher actionDispatcher,
        IMediator mediator,
        ILoggerAdapter<NoteManagementService> logger)
    {
        _notebookRepository = notebookRepository;
        _noteRepository = noteRepository;
        _noteContextService = noteContextService;
        _embeddingService = embeddingService;
        _embeddingRepository = embeddingRepository;
        _actionDispatcher = actionDispatcher;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Notebook>> GetAllNotebooksAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all notebooks for AI");
        return await _notebookRepository.GetAll(cancellationToken).ConfigureAwait(false);
    }

    public async Task<Notebook> CreateNotebookAsync(string name, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AI creating notebook '{Name}'", name);

        var now = DateTimeOffset.UtcNow;
        var notebook = new Notebook
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            CreatedAt = now,
            LastModifiedAt = now,
            SortOrder = 1
        };

        await _notebookRepository.Add(notebook, cancellationToken).ConfigureAwait(false);
        await _actionDispatcher.Dispatch(new NotebookActions.NotebookAdded(notebook)).ConfigureAwait(false);

        await _mediator.Send(new NotificationCommands.ShowNotification(
            "Created",
            $"Notebook \"{notebook.Name}\" created.",
            NotificationSeverity.Success), cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("AI created notebook '{NotebookId}' with name '{Name}'", notebook.Id, notebook.Name);

        return notebook;
    }

    public async Task<IReadOnlyList<Note>> GetNotebookNotesAsync(Guid notebookId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting notes for notebook '{NotebookId}' for AI", notebookId);
        return await _noteRepository.GetAllByNotebook(notebookId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Note> CreateNoteAsync(Guid notebookId, IReadOnlyList<BlockDefinition> blocks, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AI creating note in notebook '{NotebookId}' with {BlockCount} blocks", notebookId, blocks.Count);

        var now = DateTimeOffset.UtcNow;
        var noteBlocks = ConvertToBlocks(blocks);

        // Ensure at least one block
        if (noteBlocks.Count == 0)
        {
            noteBlocks = [new Block
            {
                Id = Guid.NewGuid(),
                Type = BlockType.Paragraph,
                Content = string.Empty,
                Order = 0
            }];
        }

        var note = new Note
        {
            Id = Guid.NewGuid(),
            NotebookId = notebookId,
            CreatedAt = now,
            LastModifiedAt = now,
            Blocks = noteBlocks
        };

        await _noteRepository.Add(note, cancellationToken).ConfigureAwait(false);
        await _actionDispatcher.Dispatch(new NoteActions.NoteCreated(note)).ConfigureAwait(false);

        // Generate embedding for the new note
        await UpdateNoteEmbeddingAsync(note, cancellationToken).ConfigureAwait(false);

        await _mediator.Send(new NotificationCommands.ShowNotification(
            "Created",
            "New note created.",
            NotificationSeverity.Success), cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("AI created note '{NoteId}' in notebook '{NotebookId}'", note.Id, notebookId);

        return note;
    }

    public async Task<Note?> GetNoteAsync(Guid noteId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting note '{NoteId}' for AI", noteId);
        return await _noteRepository.GetById(noteId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Note> EditNoteAsync(Guid noteId, IReadOnlyList<BlockDefinition> blocks, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AI editing note '{NoteId}' with {BlockCount} blocks", noteId, blocks.Count);

        var existingNote = await _noteRepository.GetById(noteId, cancellationToken).ConfigureAwait(false);
        if (existingNote is null)
        {
            throw new InvalidOperationException($"Note with ID {noteId} not found.");
        }

        var noteBlocks = ConvertToBlocks(blocks);

        // Ensure at least one block
        if (noteBlocks.Count == 0)
        {
            noteBlocks = [new Block
            {
                Id = Guid.NewGuid(),
                Type = BlockType.Paragraph,
                Content = string.Empty,
                Order = 0
            }];
        }

        var updatedNote = existingNote with
        {
            Blocks = noteBlocks,
            LastModifiedAt = DateTimeOffset.UtcNow
        };

        await _noteRepository.Update(updatedNote, cancellationToken).ConfigureAwait(false);
        await _actionDispatcher.Dispatch(new NoteActions.NoteUpdated(updatedNote)).ConfigureAwait(false);

        // Update embedding - do not await to speed up - but we cannot use the original cancellation token
        _ = UpdateNoteEmbeddingAsync(updatedNote, CancellationToken.None).ConfigureAwait(false);

        await _mediator.Send(new NotificationCommands.ShowNotification(
            "Updated",
            "Note updated.",
            NotificationSeverity.Success), cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("AI updated note '{NoteId}'", noteId);

        return updatedNote;
    }

    public async Task DeleteNoteAsync(Guid noteId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("AI deleting note '{NoteId}'", noteId);

        await _noteRepository.Delete(noteId, cancellationToken).ConfigureAwait(false);
        await _actionDispatcher.Dispatch(new NoteActions.NoteDeleted(noteId)).ConfigureAwait(false);

        await _mediator.Send(new NotificationCommands.ShowNotification(
            "Deleted",
            "Note deleted.",
            NotificationSeverity.Success), cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("AI deleted note '{NoteId}'", noteId);
    }

    public async Task<IReadOnlyList<Note>> SearchNotesAsync(string query, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("AI searching notes with query '{Query}'", query);

        // Use semantic search via the existing note context service
        const int maxResults = 10;
        var results = await _noteContextService.GetRelevantNotesAsync(query, maxResults, cancellationToken: cancellationToken).ConfigureAwait(false);
        return results.Select(r => r.Note).ToList();
    }

    private static ImmutableList<Block> ConvertToBlocks(IReadOnlyList<BlockDefinition> definitions)
    {
        var blocks = new List<Block>(definitions.Count);

        for (var i = 0; i < definitions.Count; i++)
        {
            var def = definitions[i];
            var blockType = ParseBlockType(def.Type);

            var block = new Block
            {
                Id = Guid.NewGuid(),
                Type = blockType,
                Content = def.Content,
                Metadata = def.Metadata ?? new Dictionary<string, object>(),
                Order = i
            };

            blocks.Add(block);
        }

        return [..blocks];
    }

    private static BlockType ParseBlockType(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "paragraph" or "p" or "text" => BlockType.Paragraph,
            "h1" or "heading1" => BlockType.H1,
            "h2" or "heading2" => BlockType.H2,
            "h3" or "heading3" => BlockType.H3,
            "h4" or "heading4" => BlockType.H4,
            "listitem" or "list" or "bullet" or "li" => BlockType.ListItem,
            "todoitem" or "todo" or "task" or "checkbox" => BlockType.TodoItem,
            "agendaitem" or "agenda" => BlockType.AgendaItem,
            "code" or "codeblock" => BlockType.Code,
            "quote" or "blockquote" => BlockType.Quote,
            "decision" => BlockType.Decision,
            "warning" or "alert" => BlockType.Warning,
            "divider" or "hr" or "separator" => BlockType.Divider,
            _ => BlockType.Paragraph // Default to paragraph for unknown types
        };
    }

    private async Task UpdateNoteEmbeddingAsync(Note note, CancellationToken cancellationToken)
    {
        try
        {
            var text = ExtractTextFromNote(note);
            if (string.IsNullOrWhiteSpace(text))
            {
                await _embeddingRepository.DeleteAsync(note.Id, cancellationToken).ConfigureAwait(false);
                return;
            }

            var embedding = await _embeddingService.GenerateEmbeddingAsync(text, cancellationToken).ConfigureAwait(false);
            if (embedding.Length > 0)
            {
                await _embeddingRepository.UpsertAsync(note.Id, embedding, cancellationToken).ConfigureAwait(false);
                _logger.LogDebug("Embedding updated for note '{NoteId}'", note.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update embedding for note '{NoteId}'", note.Id);
        }
    }

    private static string ExtractTextFromNote(Note note)
    {
        return string.Join("\n", note.Blocks
            .OrderBy(b => b.Order)
            .Where(b => !string.IsNullOrWhiteSpace(b.Content))
            .Select(b => b.Content));
    }
}
