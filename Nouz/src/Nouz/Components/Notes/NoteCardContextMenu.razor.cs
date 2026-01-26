using System.Collections.Immutable;
using Microsoft.AspNetCore.Components;
using Nouz.Domain.Entities;

namespace Nouz.Components.Notes;

public partial class NoteCardContextMenu
{
    private bool _showSubmenu;

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
    public EventCallback OnClose { get; set; }

    private void ToggleSubmenu()
    {
        _showSubmenu = !_showSubmenu;
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
}
