using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Nouz.Domain.Entities;

namespace Nouz.Components.Notes;

public partial class BlockRenderer : IAsyncDisposable
{
    private ElementReference _contentRef;
    private bool _showMenu;
    private bool _showContextMenu;
    private bool _contentInitialized;
    private DotNetObjectReference<BlockRenderer>? _dotNetRef;

    [Parameter, EditorRequired]
    public Block Block { get; set; } = null!;

    [Parameter, EditorRequired]
    public Guid NoteId { get; set; }

    [Parameter]
    public bool IsEditing { get; set; }

    [Parameter]
    public EventCallback<Guid> OnClick { get; set; }

    [Parameter]
    public EventCallback<Block> OnContentChanged { get; set; }

    [Parameter]
    public EventCallback<(Guid BlockId, BlockType BlockType, Dictionary<string, object>? Metadata)> OnEnterPressed { get; set; }

    [Parameter]
    public EventCallback<(Guid BlockId, BlockType NewType)> OnTypeChange { get; set; }

    [Parameter]
    public EventCallback<Guid> OnDelete { get; set; }

    protected override void OnInitialized()
    {
        _dotNetRef = DotNetObjectReference.Create(this);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_contentRef.Context is not null)
        {
            try
            {
                // Always call initBlockEditor - JS will handle deduplication
                // Only pass initial content on first init to avoid overwriting user input
                var initialContent = _contentInitialized ? null : Block.Content;
                await JsRuntime.InvokeVoidAsync("nouz.initBlockEditor", _contentRef, _dotNetRef, initialContent);
                _contentInitialized = true;

                if (IsEditing)
                {
                    await JsRuntime.InvokeVoidAsync("nouz.focusElement", _contentRef);
                }
            }
            catch
            {
                // Ignore JS interop errors
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();
        return ValueTask.CompletedTask;
    }

    [JSInvokable]
    public async Task OnEnterKeyPressed(string currentContent)
    {
        // Determine the type of block to create (smart Enter behavior)
        var newBlockType = Block.Type switch
        {
            BlockType.ListItem => BlockType.ListItem,
            BlockType.TodoItem => BlockType.TodoItem,
            _ => BlockType.Paragraph
        };

        // For list items, preserve the indentation level
        Dictionary<string, object>? metadata = null;
        if (Block.Type == BlockType.ListItem)
        {
            var indentLevel = GetIndentLevel();
            if (indentLevel > 0)
            {
                metadata = new Dictionary<string, object> { ["indent"] = indentLevel };
            }
        }

        // Just create the new block - content will be saved when Save is clicked
        await OnEnterPressed.InvokeAsync((Block.Id, newBlockType, metadata));
    }

    [JSInvokable]
    public async Task OnTabKeyPressed(bool shiftKey)
    {
        if (Block.Type != BlockType.ListItem)
        {
            return;
        }

        var currentIndent = GetIndentLevel();
        var newIndent = shiftKey
            ? Math.Max(0, currentIndent - 1)
            : Math.Min(5, currentIndent + 1);

        if (newIndent != currentIndent)
        {
            var metadata = new Dictionary<string, object>(Block.Metadata)
            {
                ["indent"] = newIndent
            };
            var updatedBlock = Block with { Metadata = metadata };
            await OnContentChanged.InvokeAsync(updatedBlock);
        }
    }

    public async Task<string> GetCurrentContent()
    {
        if (_contentRef.Context is not null)
        {
            try
            {
                return await JsRuntime.InvokeAsync<string>("nouz.getElementText", _contentRef);
            }
            catch
            {
                // Ignore JS interop errors
            }
        }
        return Block.Content;
    }

    private void ShowMenu() => _showMenu = true;

    private void HideMenu()
    {
        _showMenu = false;
        if (!_showContextMenu)
        {
            StateHasChanged();
        }
    }

    private void ToggleContextMenu()
    {
        _showContextMenu = !_showContextMenu;
    }

    private void CloseContextMenu()
    {
        _showContextMenu = false;
    }

    private async Task HandleTypeSelected(BlockType newType)
    {
        _showContextMenu = false;
        if (newType != Block.Type)
        {
            await OnTypeChange.InvokeAsync((Block.Id, newType));
        }
    }

    private async Task HandleDeleteSelected()
    {
        _showContextMenu = false;
        await OnDelete.InvokeAsync(Block.Id);
    }

    private async Task HandleClick()
    {
        await OnClick.InvokeAsync(Block.Id);
    }

    private int GetIndentLevel()
    {
        if (Block.Metadata.TryGetValue("indent", out var indent))
        {
            return indent switch
            {
                System.Text.Json.JsonElement jsonElement => jsonElement.GetInt32(),
                int intValue => intValue,
                long longValue => (int)longValue,
                _ => Convert.ToInt32(indent)
            };
        }
        return 0;
    }

    private bool GetCheckedState()
    {
        if (Block.Metadata.TryGetValue("checked", out var checkedValue))
        {
            return checkedValue switch
            {
                System.Text.Json.JsonElement jsonElement => jsonElement.GetBoolean(),
                bool boolValue => boolValue,
                _ => Convert.ToBoolean(checkedValue)
            };
        }
        return false;
    }

    private async Task HandleTodoCheckedChange(ChangeEventArgs e)
    {
        var isChecked = e.Value is bool boolValue ? boolValue : e.Value?.ToString() == "True";
        var metadata = new Dictionary<string, object>(Block.Metadata)
        {
            ["checked"] = isChecked
        };
        var updatedBlock = Block with { Metadata = metadata };
        await OnContentChanged.InvokeAsync(updatedBlock);
    }
}
