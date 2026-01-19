using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Nouz.Domain.Entities;
using Nouz.Domain.Extensions;

namespace Nouz.Components.Notes;

public partial class BlockRenderer : IAsyncDisposable
{
    private ElementReference _contentRef;
    private bool _showMenu;
    private bool _showContextMenu;
    private bool _contentInitialized;
    private bool _formattingInitialized;
    private DotNetObjectReference<BlockRenderer>? _dotNetRef;

    // Formatting toolbar state
    private bool _showFormattingToolbar;
    private FormattingToolbar.SelectionRectData? _selectionRect;
    private bool _selectionIsBold;
    private bool _selectionIsItalic;
    private string? _selectionColor;
    private int _selectionStart;
    private int _selectionEnd;

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

    private bool SupportsFormatting => Block.Type == BlockType.Paragraph;

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
                // Track if this is the initial content setup (new block)
                var isNewBlock = !_contentInitialized;

                // Only pass initial content on first init to avoid overwriting user input
                if (!_contentInitialized)
                {
                    if (SupportsFormatting && Block.GetFormatting().Count > 0)
                    {
                        // Set formatted HTML content
                        var html = Block.RenderFormattedContent();
                        await JsRuntime.InvokeVoidAsync("nouz.setElementHtml", _contentRef, html);
                    }
                    else
                    {
                        await JsRuntime.InvokeVoidAsync("nouz.initBlockEditor", _contentRef, _dotNetRef, Block.Content);
                    }
                    _contentInitialized = true;
                }
                else
                {
                    await JsRuntime.InvokeVoidAsync("nouz.initBlockEditor", _contentRef, _dotNetRef, null);
                }

                // Initialize formatting toolbar for paragraph blocks
                if (SupportsFormatting && !_formattingInitialized)
                {
                    await JsRuntime.InvokeVoidAsync("nouz.initFormattingToolbar", _contentRef, _dotNetRef);
                    _formattingInitialized = true;
                }

                // Only focus for newly created blocks that start in editing mode
                // Don't focus when user clicks an existing block (browser handles that)
                if (isNewBlock && IsEditing)
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

    [JSInvokable]
    public void OnSelectionChanged(SelectionData? selection)
    {
        if (selection is null || selection.Text.Length == 0)
        {
            if (_showFormattingToolbar)
            {
                _showFormattingToolbar = false;
                _selectionRect = null;
                StateHasChanged();
            }
            return;
        }

        _selectionStart = selection.Start;
        _selectionEnd = selection.End;
        _selectionRect = new FormattingToolbar.SelectionRectData
        {
            Top = selection.Rect.Top,
            Left = selection.Rect.Left,
            Bottom = selection.Rect.Bottom,
            Right = selection.Rect.Right,
            Width = selection.Rect.Width,
            Height = selection.Rect.Height
        };

        // Check if selection has formatting
        _selectionIsBold = Block.HasFormat(_selectionStart, _selectionEnd, TextFormatType.Bold);
        _selectionIsItalic = Block.HasFormat(_selectionStart, _selectionEnd, TextFormatType.Italic);

        // Get color if any
        var colorFormat = Block.GetFormatting()
            .FirstOrDefault(f => f.Type == TextFormatType.Color && f.Start <= _selectionStart && f.End >= _selectionEnd);
        _selectionColor = colorFormat?.Value;

        _showFormattingToolbar = true;
        StateHasChanged();
    }

    private async Task HandleFormat(TextFormatType formatType)
    {
        if (_selectionStart == _selectionEnd) return;

        // Get current plain text content
        var plainText = await GetCurrentContent();

        // Toggle the format
        var updatedBlock = Block.AdjustFormattingForContentChange(plainText)
            .ToggleFormat(_selectionStart, _selectionEnd, formatType);

        // Update the display
        await UpdateFormattedContent(updatedBlock);

        // Notify parent
        await OnContentChanged.InvokeAsync(updatedBlock);

        // Update toolbar state
        _selectionIsBold = updatedBlock.HasFormat(_selectionStart, _selectionEnd, TextFormatType.Bold);
        _selectionIsItalic = updatedBlock.HasFormat(_selectionStart, _selectionEnd, TextFormatType.Italic);
        StateHasChanged();
    }

    private async Task HandleColorSelected(string? color)
    {
        if (_selectionStart == _selectionEnd) return;

        // Get current plain text content
        var plainText = await GetCurrentContent();
        var updatedBlock = Block.AdjustFormattingForContentChange(plainText);

        if (string.IsNullOrEmpty(color))
        {
            // Remove color formatting
            updatedBlock = updatedBlock.RemoveFormat(_selectionStart, _selectionEnd, TextFormatType.Color);
        }
        else
        {
            // Remove existing color and add new one
            updatedBlock = updatedBlock.RemoveFormat(_selectionStart, _selectionEnd, TextFormatType.Color)
                .AddFormat(new TextFormat
                {
                    Start = _selectionStart,
                    End = _selectionEnd,
                    Type = TextFormatType.Color,
                    Value = color
                });
        }

        // Update the display
        await UpdateFormattedContent(updatedBlock);

        // Notify parent
        await OnContentChanged.InvokeAsync(updatedBlock);

        _selectionColor = color;
        StateHasChanged();
    }

    private async Task UpdateFormattedContent(Block block)
    {
        if (_contentRef.Context is not null)
        {
            try
            {
                var html = block.RenderFormattedContent();
                await JsRuntime.InvokeVoidAsync("nouz.setElementHtml", _contentRef, html);

                // Restore selection
                await JsRuntime.InvokeVoidAsync("nouz.setSelectionRange", _contentRef, _selectionStart, _selectionEnd);
            }
            catch
            {
                // Ignore JS interop errors
            }
        }
    }

    public class SelectionData
    {
        public int Start { get; set; }
        public int End { get; set; }
        public string Text { get; set; } = string.Empty;
        public RectData Rect { get; set; } = new();
    }

    public class RectData
    {
        public double Top { get; set; }
        public double Left { get; set; }
        public double Bottom { get; set; }
        public double Right { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }
}
