using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using Nouz.Domain.Entities;

namespace Nouz.Components.Notes;

public partial class NoteCardContextMenu
{
    private bool _showMoveSubmenu;
    private bool _showExportSubmenu;

    [Parameter, EditorRequired]
    public ImmutableList<Notebook> Notebooks { get; set; } = [];

    [Parameter, EditorRequired]
    public Guid CurrentNotebookId { get; set; }

    [Parameter]
    public EventCallback<Guid> OnMoveToNotebook { get; set; }

    [Parameter]
    public EventCallback OnDeleteSelected { get; set; }

    [Parameter]
    public EventCallback OnAddAttachment { get; set; }

    [Parameter]
    public EventCallback OnExportHtml { get; set; }

    [Parameter]
    public EventCallback OnExportPdf { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    private void ToggleMoveSubmenu()
    {
        _showMoveSubmenu = !_showMoveSubmenu;
        _showExportSubmenu = false;
    }

    private void ToggleExportSubmenu()
    {
        _showExportSubmenu = !_showExportSubmenu;
        _showMoveSubmenu = false;
    }

    private async Task SelectNotebook(Notebook notebook)
    {
        if (notebook.Id != CurrentNotebookId)
        {
            await OnMoveToNotebook.InvokeAsync(notebook.Id);
            await OnClose.InvokeAsync();
        }
    }

    private async Task HandleAddAttachment()
    {
        await OnAddAttachment.InvokeAsync();
    }

    private async Task HandleExportHtml()
    {
        await OnExportHtml.InvokeAsync();
        await OnClose.InvokeAsync();
    }

    private async Task HandleExportPdf()
    {
        await OnExportPdf.InvokeAsync();
        await OnClose.InvokeAsync();
    }
}
