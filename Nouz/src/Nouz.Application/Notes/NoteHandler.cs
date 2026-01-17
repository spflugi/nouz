using System.Collections.Immutable;
using Mediator;
using Nouz.Application.Logger;
using Nouz.Application.Notifications;
using Nouz.Application.Store;
using Nouz.Domain.Entities;
using Nouz.Domain.Repositories;

namespace Nouz.Application.Notes;

internal sealed class NoteHandler :
    ICommandHandler<NoteCommands.LoadNotesForNotebook>,
    ICommandHandler<NoteCommands.CreateNote>,
    ICommandHandler<NoteCommands.DeleteNote>,
    ICommandHandler<NoteCommands.UpdateNote>,
    ICommandHandler<NoteCommands.AddBlock>,
    ICommandHandler<NoteCommands.UpdateBlock>,
    ICommandHandler<NoteCommands.DeleteBlock>,
    ICommandHandler<NoteCommands.ChangeBlockType>,
    ICommandHandler<NoteCommands.SetEditingBlock>,
    ICommandHandler<NoteCommands.ClearNotes>,
    ICommandHandler<NoteCommands.SearchNotes>,
    ICommandHandler<NoteCommands.ClearSearch>
{
    private readonly IMediator _mediator;
    private readonly INoteRepository _noteRepository;
    private readonly IStateProvider _stateProvider;
    private readonly IActionDispatcher _actionDispatcher;
    private readonly ILoggerAdapter<NoteHandler> _logger;

    public NoteHandler(
        IMediator mediator,
        INoteRepository noteRepository,
        IStateProvider stateProvider,
        IActionDispatcher actionDispatcher,
        ILoggerAdapter<NoteHandler> logger)
    {
        _mediator = mediator;
        _noteRepository = noteRepository;
        _stateProvider = stateProvider;
        _actionDispatcher = actionDispatcher;
        _logger = logger;
    }

    /// <summary>
    /// Gets a note from state (for operations that don't persist immediately).
    /// </summary>
    private Note? GetNoteFromState(Guid noteId)
    {
        return _stateProvider.State.Notes.Notes.FirstOrDefault(n => n.Id == noteId);
    }

    public async ValueTask<Unit> Handle(NoteCommands.LoadNotesForNotebook command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading notes for notebook '{NotebookId}'", command.NotebookId);

        try
        {
            var notes = await _noteRepository.GetAllByNotebook(command.NotebookId, cancellationToken).ConfigureAwait(false);

            var sortedNotes = notes
                .OrderByDescending(n => n.CreatedAt)
                .ToImmutableList();

            await _actionDispatcher.Dispatch(new NoteActions.NotesLoaded(sortedNotes)).ConfigureAwait(false);

            _logger.LogInformation("{NumNotes} notes loaded for notebook '{NotebookId}'", notes.Count, command.NotebookId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load notes for notebook '{NotebookId}'", command.NotebookId);

            await _mediator.Send(new NotificationCommands.ShowNotification("Error", "Failed to load notes.",
                NotificationSeverity.Error), cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(NoteCommands.CreateNote command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating a new note in notebook '{NotebookId}'", command.NotebookId);

        try
        {
            var now = DateTimeOffset.UtcNow;
            var initialBlock = new Block
            {
                Id = Guid.NewGuid(),
                Type = BlockType.Paragraph,
                Content = string.Empty
            };

            var note = new Note
            {
                Id = Guid.NewGuid(),
                NotebookId = command.NotebookId,
                CreatedAt = now,
                LastModifiedAt = now,
                Blocks = [initialBlock]
            };

            await _noteRepository.Add(note, cancellationToken).ConfigureAwait(false);
            await _actionDispatcher.Dispatch(new NoteActions.NoteCreated(note)).ConfigureAwait(false);

            // Set the initial block as the editing block
            await _actionDispatcher.Dispatch(new NoteActions.EditingBlockChanged(note.Id, initialBlock.Id)).ConfigureAwait(false);

            _logger.LogInformation("New note '{NoteId}' created in notebook '{NotebookId}'", note.Id, command.NotebookId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create note in notebook '{NotebookId}'", command.NotebookId);

            await _mediator.Send(new NotificationCommands.ShowNotification("Error", "Failed to create note.",
                NotificationSeverity.Error), cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(NoteCommands.DeleteNote command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting note '{NoteId}'", command.NoteId);

        try
        {
            await _noteRepository.Delete(command.NoteId, cancellationToken).ConfigureAwait(false);
            await _actionDispatcher.Dispatch(new NoteActions.NoteDeleted(command.NoteId)).ConfigureAwait(false);

            _logger.LogInformation("Note '{NoteId}' deleted", command.NoteId);

            await _mediator.Send(new NotificationCommands.ShowNotification("Deleted", "Note deleted.",
                NotificationSeverity.Success), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete note '{NoteId}'", command.NoteId);

            await _mediator.Send(new NotificationCommands.ShowNotification("Error", "Failed to delete note.",
                NotificationSeverity.Error), cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(NoteCommands.UpdateNote command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Updating note '{NoteId}'", command.Note.Id);

        try
        {
            var updatedNote = command.Note with { LastModifiedAt = DateTimeOffset.UtcNow };

            await _noteRepository.Update(updatedNote, cancellationToken).ConfigureAwait(false);
            await _actionDispatcher.Dispatch(new NoteActions.NoteUpdated(updatedNote)).ConfigureAwait(false);

            _logger.LogDebug("Note '{NoteId}' updated", command.Note.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update note '{NoteId}'", command.Note.Id);

            await _mediator.Send(new NotificationCommands.ShowNotification("Error", "Failed to save changes.",
                NotificationSeverity.Error), cancellationToken).ConfigureAwait(false);
        }

        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(NoteCommands.AddBlock command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Adding block to note '{NoteId}' after block '{AfterBlockId}'", command.NoteId, command.AfterBlockId);

        try
        {
            // Get note from state (not database) since we're not persisting immediately
            var note = GetNoteFromState(command.NoteId);

            if (note is null)
            {
                _logger.LogWarning("Note '{NoteId}' not found in state", command.NoteId);
                return Unit.Value;
            }

            var newBlock = new Block
            {
                Id = Guid.NewGuid(),
                Type = command.Type,
                Content = string.Empty,
                Metadata = command.Metadata ?? new Dictionary<string, object>()
            };

            var blocks = note.Blocks.ToList();
            var insertIndex = command.AfterBlockId is null
                ? 0
                : blocks.FindIndex(b => b.Id == command.AfterBlockId) + 1;

            if (insertIndex < 0)
            {
                insertIndex = blocks.Count;
            }

            blocks.Insert(insertIndex, newBlock);

            var updatedNote = note with
            {
                Blocks = blocks.ToImmutableList(),
                LastModifiedAt = DateTimeOffset.UtcNow
            };

            // Only update state, don't persist - will be saved when user clicks Save
            await _actionDispatcher.Dispatch(new NoteActions.NoteUpdated(updatedNote)).ConfigureAwait(false);
            await _actionDispatcher.Dispatch(new NoteActions.EditingBlockChanged(command.NoteId, newBlock.Id)).ConfigureAwait(false);

            _logger.LogDebug("Block '{BlockId}' added to note '{NoteId}' (state only)", newBlock.Id, command.NoteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add block to note '{NoteId}'", command.NoteId);
        }

        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(NoteCommands.UpdateBlock command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Updating block '{BlockId}' in note '{NoteId}'", command.Block.Id, command.NoteId);

        try
        {
            // Get note from state (not database) since we're not persisting immediately
            var note = GetNoteFromState(command.NoteId);

            if (note is null)
            {
                _logger.LogWarning("Note '{NoteId}' not found in state", command.NoteId);
                return Unit.Value;
            }

            var oldBlock = note.Blocks.FirstOrDefault(b => b.Id == command.Block.Id);

            if (oldBlock is null)
            {
                _logger.LogWarning("Block '{BlockId}' not found in note '{NoteId}'", command.Block.Id, command.NoteId);
                return Unit.Value;
            }

            var updatedBlocks = note.Blocks.Replace(oldBlock, command.Block);
            var updatedNote = note with
            {
                Blocks = updatedBlocks,
                LastModifiedAt = DateTimeOffset.UtcNow
            };

            // Only update state, don't persist - will be saved when user clicks Save
            await _actionDispatcher.Dispatch(new NoteActions.NoteUpdated(updatedNote)).ConfigureAwait(false);

            _logger.LogDebug("Block '{BlockId}' updated in note '{NoteId}' (state only)", command.Block.Id, command.NoteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update block '{BlockId}' in note '{NoteId}'", command.Block.Id, command.NoteId);
        }

        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(NoteCommands.DeleteBlock command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Deleting block '{BlockId}' from note '{NoteId}'", command.BlockId, command.NoteId);

        try
        {
            // Get note from state (not database) since we're not persisting immediately
            var note = GetNoteFromState(command.NoteId);

            if (note is null)
            {
                _logger.LogWarning("Note '{NoteId}' not found in state", command.NoteId);
                return Unit.Value;
            }

            var blockToDelete = note.Blocks.FirstOrDefault(b => b.Id == command.BlockId);

            if (blockToDelete is null)
            {
                _logger.LogWarning("Block '{BlockId}' not found in note '{NoteId}'", command.BlockId, command.NoteId);
                return Unit.Value;
            }

            // Prevent deleting the last block
            if (note.Blocks.Count <= 1)
            {
                _logger.LogDebug("Cannot delete the last block in note '{NoteId}'", command.NoteId);
                return Unit.Value;
            }

            var blockIndex = note.Blocks.IndexOf(blockToDelete);
            var updatedBlocks = note.Blocks.Remove(blockToDelete);
            var updatedNote = note with
            {
                Blocks = updatedBlocks,
                LastModifiedAt = DateTimeOffset.UtcNow
            };

            // Only update state, don't persist - will be saved when user clicks Save
            await _actionDispatcher.Dispatch(new NoteActions.NoteUpdated(updatedNote)).ConfigureAwait(false);

            // Focus the previous block or the first block if we deleted the first one
            var newFocusIndex = Math.Max(0, blockIndex - 1);
            var newFocusBlock = updatedBlocks[newFocusIndex];
            await _actionDispatcher.Dispatch(new NoteActions.EditingBlockChanged(command.NoteId, newFocusBlock.Id)).ConfigureAwait(false);

            _logger.LogDebug("Block '{BlockId}' deleted from note '{NoteId}' (state only)", command.BlockId, command.NoteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete block '{BlockId}' from note '{NoteId}'", command.BlockId, command.NoteId);
        }

        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(NoteCommands.ChangeBlockType command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Changing block '{BlockId}' type to '{NewType}' in note '{NoteId}'",
            command.BlockId, command.NewType, command.NoteId);

        try
        {
            // Get note from state (not database) since we're not persisting immediately
            var note = GetNoteFromState(command.NoteId);

            if (note is null)
            {
                _logger.LogWarning("Note '{NoteId}' not found in state", command.NoteId);
                return Unit.Value;
            }

            var oldBlock = note.Blocks.FirstOrDefault(b => b.Id == command.BlockId);

            if (oldBlock is null)
            {
                _logger.LogWarning("Block '{BlockId}' not found in note '{NoteId}'", command.BlockId, command.NoteId);
                return Unit.Value;
            }

            var updatedBlock = oldBlock with { Type = command.NewType };
            var updatedBlocks = note.Blocks.Replace(oldBlock, updatedBlock);
            var updatedNote = note with
            {
                Blocks = updatedBlocks,
                LastModifiedAt = DateTimeOffset.UtcNow
            };

            // Only update state, don't persist - will be saved when user clicks Save
            await _actionDispatcher.Dispatch(new NoteActions.NoteUpdated(updatedNote)).ConfigureAwait(false);

            _logger.LogDebug("Block '{BlockId}' type changed to '{NewType}' (state only)", command.BlockId, command.NewType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change block '{BlockId}' type in note '{NoteId}'",
                command.BlockId, command.NoteId);
        }

        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(NoteCommands.SetEditingBlock command, CancellationToken cancellationToken)
    {
        await _actionDispatcher.Dispatch(new NoteActions.EditingBlockChanged(command.NoteId, command.BlockId)).ConfigureAwait(false);
        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(NoteCommands.ClearNotes command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Clearing notes from state");
        await _actionDispatcher.Dispatch(new NoteActions.NotesCleared()).ConfigureAwait(false);
        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(NoteCommands.SearchNotes command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Searching notes in notebook '{NotebookId}' for '{Query}'", command.NotebookId, command.Query);

        try
        {
            var results = await _noteRepository.SearchInNotebook(command.NotebookId, command.Query, cancellationToken).ConfigureAwait(false);

            var sortedResults = results
                .OrderByDescending(n => n.CreatedAt)
                .ToImmutableList();

            await _actionDispatcher.Dispatch(new NoteActions.SearchResultsLoaded(command.Query, sortedResults)).ConfigureAwait(false);

            _logger.LogDebug("Search returned {Count} results", results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search notes in notebook '{NotebookId}'", command.NotebookId);
        }

        return Unit.Value;
    }

    public async ValueTask<Unit> Handle(NoteCommands.ClearSearch command, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Clearing search");
        await _actionDispatcher.Dispatch(new NoteActions.SearchCleared()).ConfigureAwait(false);
        return Unit.Value;
    }
}
