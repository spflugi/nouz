using System.Collections.Immutable;
using System.Reactive.Linq;
using Nouz.Application.Attachments;
using Nouz.Application.Notes;
using Nouz.Components.Modals;
using Nouz.Domain.Entities;
using Nouz.Extensions;

namespace Nouz.Components.Notes;

public partial class NoteTimeline
{
    private ImmutableList<Note> _notes = [];
    private ImmutableList<Notebook> _notebooks = [];
    private ImmutableDictionary<Guid, ImmutableList<NoteAttachment>> _attachmentsByNoteId =
        ImmutableDictionary<Guid, ImmutableList<NoteAttachment>>.Empty;
    private Guid? _selectedNotebookId;
    private Guid? _editingNoteId;
    private Guid? _editingBlockId;
    private ImagePreviewModal _imagePreviewModal = null!;

    protected override void OnInitialized()
    {
        StateProvider.StateObservable
            .Select(s => s.Notes.DisplayNotes)
            .DistinctUntilChanged()
            .TakeUntilDisappearing(this)
            .Subscribe(notes =>
            {
                _notes = notes;

                // Load attachments for all notes (fire-and-forget, state subscription handles updates)
                _ = LoadAttachmentsForNotes(notes);

                InvokeAsync(StateHasChanged);
            });

        StateProvider.StateObservable
            .Select(s => s.Notes.AttachmentsByNoteId)
            .DistinctUntilChanged()
            .TakeUntilDisappearing(this)
            .Subscribe(attachments =>
            {
                _attachmentsByNoteId = attachments;
                InvokeAsync(StateHasChanged);
            });

        StateProvider.StateObservable
            .Select(s => s.Notebooks.Notebooks)
            .DistinctUntilChanged()
            .TakeUntilDisappearing(this)
            .Subscribe(notebooks =>
            {
                _notebooks = notebooks;
                InvokeAsync(StateHasChanged);
            });

        StateProvider.StateObservable
            .Select(s => s.Notebooks.SelectedNotebook)
            .DistinctUntilChanged()
            .TakeUntilDisappearing(this)
            .Subscribe(notebookId =>
            {
                _selectedNotebookId = notebookId == Guid.Empty ? null : notebookId;

                if (_selectedNotebookId is not null)
                {
                    _ = Mediator.Send(new NoteCommands.LoadNotesForNotebook(_selectedNotebookId.Value));
                }
                else
                {
                    _ = Mediator.Send(new NoteCommands.ClearNotes());
                }

                InvokeAsync(StateHasChanged);
            });

        StateProvider.StateObservable
            .Select(s => (s.Notes.EditingNoteId, s.Notes.EditingBlockId))
            .DistinctUntilChanged()
            .TakeUntilDisappearing(this)
            .Subscribe(editing =>
            {
                _editingNoteId = editing.EditingNoteId;
                _editingBlockId = editing.EditingBlockId;
                InvokeAsync(StateHasChanged);
            });
    }

    private async Task LoadAttachmentsForNotes(ImmutableList<Note> notes)
    {
        foreach (var note in notes)
        {
            await Mediator.Send(new AttachmentCommands.LoadAttachments(note.Id));
        }
    }

    private ImmutableList<NoteAttachment> GetAttachmentsForNote(Guid noteId)
    {
        return _attachmentsByNoteId.TryGetValue(noteId, out var attachments)
            ? attachments
            : [];
    }

    private async Task DeleteNote(Guid noteId)
    {
        var confirmed = await ShowConfirmationModal("Delete note",
            "Are you sure you want to delete this note? This action cannot be undone.", "Delete", "Cancel");

        if (confirmed)
        {
            await Mediator.Send(new NoteCommands.DeleteNote(noteId));
        }
    }

    private async Task HandleBlockClick(Guid noteId, Guid blockId)
    {
        await Mediator.Send(new NoteCommands.SetEditingBlock(noteId, blockId));
    }

    private async Task HandleBlockContentChanged(Guid noteId, Block block)
    {
        await Mediator.Send(new NoteCommands.UpdateBlock(noteId, block));
    }

    private async Task HandleBlockAdd(Guid noteId, Guid afterBlockId, BlockType blockType, Dictionary<string, object>? metadata = null)
    {
        await Mediator.Send(new NoteCommands.AddBlock(noteId, afterBlockId, blockType, metadata));
    }

    private async Task HandleBlockTypeChange(Guid noteId, Guid blockId, BlockType newType)
    {
        await Mediator.Send(new NoteCommands.ChangeBlockType(noteId, blockId, newType));
    }

    private async Task HandleBlockDelete(Guid noteId, Guid blockId)
    {
        await Mediator.Send(new NoteCommands.DeleteBlock(noteId, blockId));
    }

    private async Task HandleSaveNote(Note updatedNote)
    {
        await Mediator.Send(new NoteCommands.UpdateNote(updatedNote));
        await Mediator.Send(new NoteCommands.SetEditingBlock(null, null));
    }

    private async Task MoveNote(Guid noteId, Guid newNotebookId)
    {
        await Mediator.Send(new NoteCommands.MoveNote(noteId, newNotebookId));
    }

    private async Task HandleBlockReorder(Guid noteId, Guid blockId, int newIndex)
    {
        await Mediator.Send(new NoteCommands.ReorderBlocks(noteId, blockId, newIndex));
    }

    private async Task HandleImagePasted(Guid noteId, Guid? afterBlockId, string imageData, string fileName, string mimeType)
    {
        await Mediator.Send(new NoteCommands.AddImageBlock(noteId, afterBlockId, imageData, fileName, mimeType));
    }

    private async Task HandleImageCaptionChanged(Guid noteId, Guid blockId, string caption)
    {
        await Mediator.Send(new NoteCommands.UpdateImageCaption(noteId, blockId, caption));
    }

    private async Task HandleImageWidthChanged(Guid noteId, Guid blockId, int widthPercent)
    {
        await Mediator.Send(new NoteCommands.UpdateImageWidth(noteId, blockId, widthPercent));
    }

    private void ShowImagePreview(string imageDataUrl, string? caption)
    {
        _imagePreviewModal.Show(imageDataUrl, caption);
    }

    private async Task HandleAddAttachment(Guid noteId, Stream fileStream, string fileName)
    {
        await Mediator.Send(new AttachmentCommands.AddAttachment(noteId, fileStream, fileName));
    }

    private async Task HandleDeleteAttachment(Guid noteId, Guid attachmentId)
    {
        var confirmed = await ShowConfirmationModal("Remove attachment",
            "Are you sure you want to remove this attachment?", "Remove", "Cancel");

        if (confirmed)
        {
            await Mediator.Send(new AttachmentCommands.DeleteAttachment(noteId, attachmentId));
        }
    }

    private async Task HandleOpenAttachment(Guid attachmentId)
    {
        await Mediator.Send(new AttachmentCommands.OpenAttachment(attachmentId));
    }

    private async Task HandleMermaidToggleEditMode(Guid noteId, Guid blockId)
    {
        await Mediator.Send(new NoteCommands.ToggleMermaidEditMode(noteId, blockId));
    }

    private async Task HandleTableAddRow(Guid noteId, Guid blockId, int? afterRowIndex)
    {
        await Mediator.Send(new NoteCommands.AddTableRow(noteId, blockId, afterRowIndex));
    }

    private async Task HandleTableRemoveRow(Guid noteId, Guid blockId, int rowIndex)
    {
        await Mediator.Send(new NoteCommands.RemoveTableRow(noteId, blockId, rowIndex));
    }

    private async Task HandleTableAddColumn(Guid noteId, Guid blockId, int? afterColumnIndex)
    {
        await Mediator.Send(new NoteCommands.AddTableColumn(noteId, blockId, afterColumnIndex));
    }

    private async Task HandleTableRemoveColumn(Guid noteId, Guid blockId, int columnIndex)
    {
        await Mediator.Send(new NoteCommands.RemoveTableColumn(noteId, blockId, columnIndex));
    }

    private async Task HandleTableCellChanged(Guid noteId, Guid blockId, int rowIndex, int colIndex, string content)
    {
        await Mediator.Send(new NoteCommands.UpdateTableCell(noteId, blockId, rowIndex, colIndex, content));
    }
}
