using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Nouz.Domain.Entities;

namespace Nouz.Components.Notes;

public partial class MermaidBlock : IAsyncDisposable
{
    private ElementReference _codeRef;
    private ElementReference _previewRef;
    private bool _contentInitialized;
    private bool _wasEditMode;
    private bool _hasError;
    private string? _errorMessage;
    private DotNetObjectReference<MermaidBlock>? _dotNetRef;
    private Guid? _previousBlockId;

    [Parameter, EditorRequired]
    public Block Block { get; set; } = null!;

    [Parameter, EditorRequired]
    public Guid NoteId { get; set; }

    [Parameter]
    public EventCallback<Block> OnContentChanged { get; set; }

    [Parameter]
    public EventCallback OnToggleEditMode { get; set; }

    [Parameter]
    public EventCallback OnSaveRequested { get; set; }

    public bool IsEditMode => GetIsEditMode();

    protected override void OnInitialized()
    {
        _dotNetRef = DotNetObjectReference.Create(this);
    }

    protected override void OnParametersSet()
    {
        var currentEditMode = IsEditMode;

        // Detect when the block itself changes (e.g., after switching notebooks)
        if (_previousBlockId.HasValue && _previousBlockId.Value != Block.Id)
        {
            _contentInitialized = false;
            _hasError = false;
            _errorMessage = null;
        }

        // Detect when switching from edit mode to preview mode
        if (!currentEditMode && _wasEditMode)
        {
            _contentInitialized = false; // Reset to re-render preview
        }

        _wasEditMode = currentEditMode;
        _previousBlockId = Block.Id;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsEditMode)
        {
            if (_codeRef.Context is not null && !_contentInitialized)
            {
                try
                {
                    await JsRuntime.InvokeVoidAsync("nouz.initMermaidEditor", _codeRef, _dotNetRef, Block.Content);
                    _contentInitialized = true;
                }
                catch
                {
                    // Ignore JS interop errors
                }
            }
        }
        else
        {
            if (_previewRef.Context is not null && !_contentInitialized)
            {
                try
                {
                    await JsRuntime.InvokeVoidAsync("nouz.renderMermaidDiagram", _previewRef, Block.Content, _dotNetRef);
                    _contentInitialized = true;
                }
                catch
                {
                    // Ignore JS interop errors
                }
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();
        return ValueTask.CompletedTask;
    }

    private bool GetIsEditMode()
    {
        if (Block.Metadata.TryGetValue("isEditMode", out var isEditModeObj))
        {
            return isEditModeObj switch
            {
                bool boolValue => boolValue,
                System.Text.Json.JsonElement jsonElement => jsonElement.GetBoolean(),
                _ => true
            };
        }
        return true;
    }

    private async Task HandleToggleMode()
    {
        // Save current content before toggling
        if (IsEditMode && _codeRef.Context is not null)
        {
            try
            {
                var content = await JsRuntime.InvokeAsync<string>("nouz.getElementText", _codeRef);
                if (content != Block.Content)
                {
                    var updatedBlock = Block with { Content = content };
                    await OnContentChanged.InvokeAsync(updatedBlock);
                }
            }
            catch
            {
                // Ignore JS interop errors
            }
        }

        _contentInitialized = false;
        _hasError = false;
        _errorMessage = null;
        await OnToggleEditMode.InvokeAsync();
    }

    private async Task HandleInput()
    {
        if (_codeRef.Context is not null)
        {
            try
            {
                var content = await JsRuntime.InvokeAsync<string>("nouz.getElementText", _codeRef);
                if (content != Block.Content)
                {
                    var updatedBlock = Block with { Content = content };
                    await OnContentChanged.InvokeAsync(updatedBlock);
                }
            }
            catch
            {
                // Ignore JS interop errors
            }
        }
    }

    [JSInvokable]
    public void OnMermaidRenderSuccess()
    {
        _hasError = false;
        _errorMessage = null;
        StateHasChanged();
    }

    [JSInvokable]
    public void OnMermaidRenderError(string message)
    {
        _hasError = true;
        _errorMessage = message;
        StateHasChanged();
    }

    [JSInvokable]
    public async Task OnSaveKeyPressed()
    {
        await OnSaveRequested.InvokeAsync();
    }

    public async Task<string> GetCurrentContent()
    {
        if (_codeRef.Context is not null)
        {
            try
            {
                return await JsRuntime.InvokeAsync<string>("nouz.getElementText", _codeRef);
            }
            catch
            {
                // Ignore JS interop errors
            }
        }
        return Block.Content;
    }
}
