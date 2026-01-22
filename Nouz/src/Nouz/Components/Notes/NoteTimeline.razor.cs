using System.Collections.Immutable;
using System.Reactive.Linq;
using Nouz.Application.Notes;
using Nouz.Components.Modals;
using Nouz.Domain.Entities;
using Nouz.Extensions;

namespace Nouz.Components.Notes;

public partial class NoteTimeline
{
    private ImmutableList<Note> _notes = [];
    private ImmutableList<Notebook> _notebooks = [];
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
                StateHasChanged();
            });

        StateProvider.StateObservable
            .Select(s => s.Notebooks.Notebooks)
            .DistinctUntilChanged()
            .TakeUntilDisappearing(this)
            .Subscribe(notebooks =>
            {
                _notebooks = notebooks;
                StateHasChanged();
            });

        StateProvider.StateObservable
            .Select(s => s.Notebooks.SelectedNotebook)
            .DistinctUntilChanged()
            .TakeUntilDisappearing(this)
            .Subscribe(async notebookId =>
            {
                _selectedNotebookId = notebookId == Guid.Empty ? null : notebookId;

                if (_selectedNotebookId is not null)
                {
                    await Mediator.Send(new NoteCommands.LoadNotesForNotebook(_selectedNotebookId.Value));
                }
                else
                {
                    await Mediator.Send(new NoteCommands.ClearNotes());
                }

                StateHasChanged();
            });

        StateProvider.StateObservable
            .Select(s => (s.Notes.EditingNoteId, s.Notes.EditingBlockId))
            .DistinctUntilChanged()
            .TakeUntilDisappearing(this)
            .Subscribe(editing =>
            {
                _editingNoteId = editing.EditingNoteId;
                _editingBlockId = editing.EditingBlockId;
                StateHasChanged();
            });
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
}
