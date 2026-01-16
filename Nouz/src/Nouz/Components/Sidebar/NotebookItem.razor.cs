using Microsoft.AspNetCore.Components;
using Nouz.Domain.Entities;

namespace Nouz.Components.Sidebar;

public partial class NotebookItem
{
    [Parameter, EditorRequired]
    public Notebook Notebook { get; set; } = null!;

    [Parameter]
    public bool IsSelected { get; set; }

    [Parameter]
    public EventCallback<Guid?> OnSelect { get; set; }

    [Parameter]
    public EventCallback<Guid> OnRename { get; set; }

    [Parameter]
    public EventCallback<Guid> OnDelete { get; set; }

    private async Task HandleSelect()
    {
        await OnSelect.InvokeAsync(Notebook.Id);
    }

    private async Task HandleRename()
    {
        await OnRename.InvokeAsync(Notebook.Id);
    }

    private async Task HandleDelete()
    {
        await OnDelete.InvokeAsync(Notebook.Id);
    }
}