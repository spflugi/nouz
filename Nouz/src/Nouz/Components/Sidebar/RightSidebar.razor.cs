using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Nouz.Components.Sidebar;

public partial class RightSidebar
{
    private const int MinWidth = 200;
    private const int MaxWidth = 800;

    private ElementReference _sidebarRef;
    private ElementReference _resizeHandleRef;
    private DotNetObjectReference<RightSidebar>? _dotNetRef;

    private int _sidebarWidth = 400;
    private bool _isResizingInitialized;
    private bool _isCollapsed = true;

    [JSInvokable]
    public void OnRightSidebarResized(int newWidth)
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
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_isCollapsed && !_isResizingInitialized && _resizeHandleRef.Context is not null && _sidebarRef.Context is not null)
        {
            _isResizingInitialized = true;
            try
            {
                await JsRuntime.InvokeVoidAsync("nouz.initRightSidebarResize", _resizeHandleRef, _sidebarRef, _dotNetRef, MinWidth, MaxWidth);
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
}
