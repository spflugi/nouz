using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Nouz.Components.Sidebar;

public partial class Sidebar
{
    private const int MinWidth = 120;
    private const int MaxWidth = 600;

    private ElementReference _sidebarRef;
    private ElementReference _resizeHandleRef;
    private DotNetObjectReference<Sidebar>? _dotNetRef;

    private int _sidebarWidth = 250;
    private bool _isResizingInitialized;

    [JSInvokable]
    public void OnSidebarResized(int newWidth)
    {
        _sidebarWidth = newWidth;
    }

    public ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();
        return ValueTask.CompletedTask;
    }

    protected override void OnInitialized()
    {
        _dotNetRef = DotNetObjectReference.Create(this);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_isResizingInitialized && _resizeHandleRef.Context is not null && _sidebarRef.Context is not null)
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

    private Task CreateNotebook()
    {
        return Task.CompletedTask;
    }
}