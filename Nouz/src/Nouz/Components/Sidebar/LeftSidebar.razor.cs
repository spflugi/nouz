using System.Collections.Immutable;
using System.Reactive.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Nouz.Application.Notebooks;
using Nouz.Domain.Entities;
using Nouz.Extensions;

namespace Nouz.Components.Sidebar;

public partial class LeftSidebar
{
    private const int MinWidth = 120;
    private const int MaxWidth = 600;

    private ElementReference _sidebarRef;
    private ElementReference _resizeHandleRef;
    private DotNetObjectReference<LeftSidebar>? _dotNetRef;

    private int _sidebarWidth = 270;
    private bool _isResizingInitialized;
    private bool _isCollapsed;

    private ImmutableList<Notebook> _notebooks = [];
    private Guid? _selectedNotebookId;

    [JSInvokable]
    public void OnSidebarResized(int newWidth)
    {
        _sidebarWidth = newWidth;
    }

    public ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();

        // Call base.Dispose() to fire DisposingEvent for TakeUntilDisappearing subscriptions
        Dispose();

        return ValueTask.CompletedTask;
    }

    protected override void OnInitialized()
    {
        _dotNetRef = DotNetObjectReference.Create(this);

        StateProvider.StateObservable
            .Select(s => s.Notebooks.Notebooks)
            .DistinctUntilChanged()
            .TakeUntilDisappearing(this)
            .Subscribe(notebooks =>
            {
                _notebooks = notebooks.OrderBy(n => n.Name).ToImmutableList();
                StateHasChanged();
            });

        StateProvider.StateObservable
            .Select(s => s.Notebooks.SelectedNotebook)
            .DistinctUntilChanged()
            .TakeUntilDisappearing(this)
            .Subscribe(id =>
            {
                _selectedNotebookId = id;
                StateHasChanged();
            });
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_isCollapsed && !_isResizingInitialized && _resizeHandleRef.Context is not null && _sidebarRef.Context is not null)
        {
            _isResizingInitialized = true;
            try
            {
                await JsRuntime.InvokeVoidAsync("nouz.initSidebarResize", _resizeHandleRef, _sidebarRef, _dotNetRef, MinWidth, MaxWidth);
            }
            catch
            {
                // Ignore JS errors during initialization
            }
        }
    }

    private void ToggleCollapse()
    {
        _isCollapsed = !_isCollapsed;
        _isResizingInitialized = false; // Reset so resize gets re-initialized when expanded
    }

    private async Task CreateNotebook()
    {
        var notebookTitle = await ShowTextInputModal("Create new notebook", "Notebook title");

        if (!string.IsNullOrWhiteSpace(notebookTitle))
        {
            await Mediator.Send(new NotebookCommands.CreateNotebook(notebookTitle));
        }
    }

    private async Task SelectNotebook(Guid? notebookId)
    {
        if (notebookId is not null)
        {
            await Mediator.Send(new NotebookCommands.SelectNotebook(notebookId.Value));
        }
    }

    private async Task RenameNotebook(Guid notebookId)
    {
        var newNotebookTitle = await ShowTextInputModal("Rename notebook", "Notebook title");

        if (!string.IsNullOrWhiteSpace(newNotebookTitle))
        {
            await Mediator.Send(new NotebookCommands.RenameNotebook(notebookId, newNotebookTitle));
        }
    }

    private async Task DeleteNotebook(Guid notebookId)
    {
        var confirmed = await ShowConfirmationModal("Delete notebook",
            "Are you sure you want to delete this notebook? This action cannot be undone.", "Delete", "Cancel");

        if (confirmed)
        {
            await Mediator.Send(new NotebookCommands.DeleteNotebook(notebookId));
        }
    }
}