using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using Nouz.Domain.Entities;

namespace Nouz.Components.Notes;

public partial class NoteCard
{
    private readonly Dictionary<Guid, BlockRenderer> _blockRenderers = new();
    private bool _showContextMenu;
    private bool _showSavedNotification = false;

    [Parameter, EditorRequired]
    public Note Note { get; set; } = null!;

    [Parameter]
    public ImmutableList<Notebook> Notebooks { get; set; } = [];

    [Parameter]
    public bool IsEditing { get; set; }

    [Parameter]
    public Guid? EditingBlockId { get; set; }

    [Parameter]
    public EventCallback<Guid> OnDelete { get; set; }

    [Parameter]
    public EventCallback<(Guid NoteId, Guid NewNotebookId)> OnMoveToNotebook { get; set; }

    [Parameter]
    public EventCallback<(Guid NoteId, Guid BlockId)> OnBlockClick { get; set; }

    [Parameter]
    public EventCallback<(Guid NoteId, Block Block)> OnBlockContentChanged { get; set; }

    [Parameter]
    public EventCallback<(Guid NoteId, Guid AfterBlockId, BlockType BlockType, Dictionary<string, object>? Metadata)> OnBlockAdd { get; set; }

    [Parameter]
    public EventCallback<(Guid NoteId, Guid BlockId, BlockType NewType)> OnBlockTypeChange { get; set; }

    [Parameter]
    public EventCallback<(Guid NoteId, Guid BlockId)> OnBlockDelete { get; set; }

    [Parameter]
    public EventCallback<(Guid NoteId, Guid BlockId, int NewIndex)> OnBlockReorder { get; set; }

    [Parameter]
    public EventCallback<Note> OnSave { get; set; }

    [Parameter]
    public EventCallback<(Guid NoteId, Guid? AfterBlockId, string ImageData, string FileName, string MimeType)> OnImagePasted { get; set; }

    [Parameter]
    public EventCallback<(Guid NoteId, Guid BlockId, string Caption)> OnImageCaptionChanged { get; set; }

    [Parameter]
    public EventCallback<(Guid NoteId, Guid BlockId, int WidthPercent)> OnImageWidthChanged { get; set; }

    [Parameter]
    public EventCallback<(string ImageDataUrl, string? Caption)> OnImagePreviewRequested { get; set; }

    private async Task HandleBlockClick(Guid blockId)
    {
        await OnBlockClick.InvokeAsync((Note.Id, blockId));
    }

    private async Task HandleBlockContentChanged(Block block)
    {
        await OnBlockContentChanged.InvokeAsync((Note.Id, block));
    }

    private async Task HandleBlockEnterPressed((Guid BlockId, BlockType BlockType, Dictionary<string, object>? Metadata) args)
    {
        await OnBlockAdd.InvokeAsync((Note.Id, args.BlockId, args.BlockType, args.Metadata));
    }

    private async Task HandleBlockTypeChange((Guid BlockId, BlockType NewType) args)
    {
        await OnBlockTypeChange.InvokeAsync((Note.Id, args.BlockId, args.NewType));
    }

    private async Task HandleBlockDelete(Guid blockId)
    {
        await OnBlockDelete.InvokeAsync((Note.Id, blockId));
    }

    private async Task HandleBlockReorder(Guid blockId, int newIndex)
    {
        await OnBlockReorder.InvokeAsync((Note.Id, blockId, newIndex));
    }

    private async Task HandleSave()
    {
        var updatedBlocks = new List<Block>();
        var order = 0;

        foreach (var block in Note.Blocks)
        {
            if (_blockRenderers.TryGetValue(block.Id, out var renderer))
            {
                var content = await renderer.GetCurrentContent();
                updatedBlocks.Add(block with { Content = content, Order = order });
            }
            else
            {
                updatedBlocks.Add(block with { Order = order });
            }
            order++;
        }

        var updatedNote = Note with { Blocks = updatedBlocks.ToImmutableList() };
        await OnSave.InvokeAsync(updatedNote);

        _showSavedNotification = true;
        StateHasChanged();

        await Task.Delay(TimeSpan.FromSeconds(1));

        _showSavedNotification = false;
        StateHasChanged();
    }

    private void ToggleContextMenu()
    {
        _showContextMenu = !_showContextMenu;
    }

    private void CloseContextMenu()
    {
        _showContextMenu = false;
    }

    private async Task HandleMoveToNotebook(Guid newNotebookId)
    {
        _showContextMenu = false;
        await OnMoveToNotebook.InvokeAsync((Note.Id, newNotebookId));
    }

    private async Task HandleDeleteSelected()
    {
        _showContextMenu = false;
        await OnDelete.InvokeAsync(Note.Id);
    }

    private async Task HandleImagePasted((Guid NoteId, Guid? AfterBlockId, string ImageData, string FileName, string MimeType) args)
    {
        await OnImagePasted.InvokeAsync(args);
    }

    private async Task HandleImageCaptionChanged((Guid NoteId, Guid BlockId, string Caption) args)
    {
        await OnImageCaptionChanged.InvokeAsync(args);
    }

    private async Task HandleImageWidthChanged((Guid NoteId, Guid BlockId, int WidthPercent) args)
    {
        await OnImageWidthChanged.InvokeAsync(args);
    }

    private async Task HandleImagePreviewRequested((string ImageDataUrl, string? Caption) args)
    {
        await OnImagePreviewRequested.InvokeAsync(args);
    }
}
