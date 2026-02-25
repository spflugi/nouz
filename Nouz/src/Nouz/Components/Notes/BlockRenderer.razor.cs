using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Nouz.Domain.Entities;
using Nouz.Domain.Extensions;
using Nouz.Domain.Repositories;

namespace Nouz.Components.Notes;

public partial class BlockRenderer : IAsyncDisposable
{
    private ElementReference _contentRef;
    private ElementReference _imageFileInputRef;
    private bool _showMenu;
    private bool _showContextMenu;
    private bool _contentInitialized;
    private bool _formattingInitialized;
    private bool _imagePasteInitialized;
    private bool _wasEditing;
    private bool _shouldFocus;
    private BlockType? _previousBlockType;
    private DotNetObjectReference<BlockRenderer>? _dotNetRef;

    // Drag and drop state
    private bool _isDragging;
    private bool _isDragOver;
    private static Guid? _draggedBlockId;

    // Formatting toolbar state
    private bool _showFormattingToolbar;
    private FormattingToolbar.SelectionRectData? _selectionRect;
    private bool _selectionIsBold;
    private bool _selectionIsItalic;
    private string? _selectionColor;
    private int _selectionStart;
    private int _selectionEnd;

    // Image block state
    private string? _imageDataUrl;
    private Guid? _loadedImageAttachmentId;
    private ElementReference _resizeHandleRef;
    private bool _resizeHandlerInitialized;

    // Mermaid block state
    private MermaidBlock? _mermaidBlockRef;

    [Inject]
    private IAttachmentRepository AttachmentRepository { get; set; } = null!;

    [Parameter, EditorRequired]
    public Block Block { get; set; } = null!;

    [Parameter, EditorRequired]
    public Guid NoteId { get; set; }

    [Parameter]
    public int Index { get; set; }

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

    [Parameter]
    public EventCallback<(Guid BlockId, int NewIndex)> OnReorder { get; set; }

    [Parameter]
    public EventCallback OnSaveRequested { get; set; }

    [Parameter]
    public EventCallback<(Guid NoteId, Guid? AfterBlockId, string ImageData, string FileName, string MimeType)> OnImagePasted { get; set; }

    [Parameter]
    public EventCallback<(Guid NoteId, Guid BlockId, string Caption)> OnImageCaptionChanged { get; set; }

    [Parameter]
    public EventCallback<(Guid NoteId, Guid BlockId, int WidthPercent)> OnImageWidthChanged { get; set; }

    [Parameter]
    public EventCallback<(string ImageDataUrl, string? Caption)> OnImagePreviewRequested { get; set; }

    [Parameter]
    public EventCallback<Guid> OnMermaidToggleEditMode { get; set; }

    [Parameter]
    public EventCallback OnTableAddRow { get; set; }

    [Parameter]
    public EventCallback<int> OnTableRemoveRow { get; set; }

    [Parameter]
    public EventCallback OnTableAddColumn { get; set; }

    [Parameter]
    public EventCallback<int> OnTableRemoveColumn { get; set; }

    [Parameter]
    public EventCallback<(int RowIndex, int ColIndex, string Content)> OnTableCellChanged { get; set; }

    [Parameter]
    public EventCallback OnTableInsertBlockAfter { get; set; }

    [Parameter]
    public EventCallback OnMermaidInsertBlockAfter { get; set; }

    private bool SupportsFormatting => Block.Type == BlockType.Paragraph;

    protected override void OnInitialized()
    {
        _dotNetRef = DotNetObjectReference.Create(this);
    }

    protected override void OnParametersSet()
    {
        // Detect when IsEditing changes from false to true
        if (IsEditing && !_wasEditing)
        {
            _shouldFocus = true;
        }

        // Detect when block type changes while editing (e.g., via context menu)
        // The DOM element changes so we need to re-focus
        if (IsEditing && _previousBlockType.HasValue && _previousBlockType.Value != Block.Type)
        {
            _shouldFocus = true;
            _contentInitialized = false; // Reset so content is re-initialized for new element
        }

        _wasEditing = IsEditing;
        _previousBlockType = Block.Type;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Divider blocks don't need JavaScript initialization - they're not editable
        if (Block.Type == BlockType.Divider)
        {
            _contentInitialized = true;
            return;
        }

        // Image blocks need to load their image data and initialize resize handler
        if (Block.Type == BlockType.Image)
        {
            await LoadImageDataAsync();

            // Initialize resize handler after image is loaded
            if (!_resizeHandlerInitialized && _resizeHandleRef.Context is not null)
            {
                try
                {
                    await JsRuntime.InvokeVoidAsync("nouz.initImageResizeHandler", _resizeHandleRef, _contentRef, _dotNetRef);
                    _resizeHandlerInitialized = true;
                }
                catch
                {
                    // Ignore JS interop errors
                }
            }

            _contentInitialized = true;
            return;
        }

        if (_contentRef.Context is not null)
        {
            try
            {
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

                // Initialize image paste handler for editable blocks
                if (!_imagePasteInitialized && Block.Type != BlockType.Divider)
                {
                    await JsRuntime.InvokeVoidAsync("nouz.initImagePasteHandler", _contentRef, _dotNetRef);
                    _imagePasteInitialized = true;
                }

                // Focus the block if it became the editing block
                // This handles new blocks and blocks changed via context menu
                if (_shouldFocus)
                {
                    _shouldFocus = false;
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
            BlockType.AgendaItem => BlockType.AgendaItem,
            _ => BlockType.Paragraph
        };

        // For list and agenda items, preserve the indentation level
        Dictionary<string, object>? metadata = null;
        if (Block.Type is BlockType.ListItem or BlockType.AgendaItem)
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
        if (Block.Type is not BlockType.ListItem and not BlockType.AgendaItem)
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

    [JSInvokable]
    public async Task OnSaveKeyPressed()
    {
        await OnSaveRequested.InvokeAsync();
    }

    public async Task<string> GetCurrentContent()
    {
        // Handle Mermaid blocks specially - they have their own component
        if (Block.Type == BlockType.Mermaid && _mermaidBlockRef is not null)
        {
            return await _mermaidBlockRef.GetCurrentContent();
        }

        // Table blocks store data in metadata, not content
        if (Block.Type == BlockType.Table)
        {
            return Block.Content;
        }

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

    private async Task HandleInsertImageRequested()
    {
        _showContextMenu = false;
        try
        {
            await JsRuntime.InvokeVoidAsync("nouz.triggerImageFileInput", _imageFileInputRef);
        }
        catch
        {
            // Ignore JS interop errors
        }
    }

    private async Task HandleImageFileSelected(ChangeEventArgs e)
    {
        // File handling is done via JavaScript - the onchange event provides limited info in Blazor
        // We need to read the file via JS and then invoke our method
        try
        {
            await JsRuntime.InvokeVoidAsync("nouz.processImageFileInput", _imageFileInputRef, _dotNetRef);
        }
        catch
        {
            // Ignore JS interop errors
        }
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

    private string GetIdeaStatus()
    {
        if (Block.Metadata.TryGetValue("status", out var statusValue))
        {
            return statusValue switch
            {
                string str => str,
                System.Text.Json.JsonElement jsonElement => jsonElement.GetString() ?? "raw",
                _ => "raw"
            };
        }
        return "raw";
    }

    private string GetIdeaStatusLabel() => GetIdeaStatus() switch
    {
        "exploring" => "Exploring",
        "adopted" => "Adopted",
        "dropped" => "Dropped",
        _ => "Raw"
    };

    private async Task CycleIdeaStatus()
    {
        var nextStatus = GetIdeaStatus() switch
        {
            "raw" => "exploring",
            "exploring" => "adopted",
            "adopted" => "dropped",
            _ => "raw"
        };
        var metadata = new Dictionary<string, object>(Block.Metadata)
        {
            ["status"] = nextStatus
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

    private void HandleDragStart()
    {
        _isDragging = true;
        _draggedBlockId = Block.Id;
    }

    private void HandleDragEnd()
    {
        _isDragging = false;
        _draggedBlockId = null;
    }

    private void HandleDragOver()
    {
        if (_draggedBlockId.HasValue && _draggedBlockId.Value != Block.Id)
        {
            _isDragOver = true;
        }
    }

    private void HandleDragLeave()
    {
        _isDragOver = false;
    }

    private async Task HandleDrop()
    {
        _isDragOver = false;

        if (_draggedBlockId.HasValue && _draggedBlockId.Value != Block.Id)
        {
            await OnReorder.InvokeAsync((_draggedBlockId.Value, Index));
        }
    }

    // Image block methods
    private async Task LoadImageDataAsync()
    {
        if (Block.Type != BlockType.Image)
        {
            return;
        }

        var attachmentId = GetImageAttachmentId();
        if (attachmentId == Guid.Empty || attachmentId == _loadedImageAttachmentId)
        {
            return;
        }

        try
        {
            var imageData = await AttachmentRepository.LoadAsync(attachmentId);
            if (imageData is not null)
            {
                var mimeType = GetImageMimeType();
                _imageDataUrl = $"data:{mimeType};base64,{Convert.ToBase64String(imageData)}";
                _loadedImageAttachmentId = attachmentId;
                StateHasChanged();
            }
        }
        catch
        {
            // Ignore errors loading image
        }
    }

    private Guid GetImageAttachmentId()
    {
        if (Block.Metadata.TryGetValue("attachmentId", out var attachmentIdObj))
        {
            return attachmentIdObj switch
            {
                Guid guid => guid,
                string str => Guid.TryParse(str, out var parsed) ? parsed : Guid.Empty,
                System.Text.Json.JsonElement jsonElement => Guid.TryParse(jsonElement.GetString(), out var parsed) ? parsed : Guid.Empty,
                _ => Guid.Empty
            };
        }
        return Guid.Empty;
    }

    private string GetImageMimeType()
    {
        if (Block.Metadata.TryGetValue("mimeType", out var mimeTypeObj))
        {
            return mimeTypeObj switch
            {
                string str => str,
                System.Text.Json.JsonElement jsonElement => jsonElement.GetString() ?? "image/png",
                _ => "image/png"
            };
        }
        return "image/png";
    }

    private string GetImageCaption()
    {
        if (Block.Metadata.TryGetValue("caption", out var captionObj))
        {
            return captionObj switch
            {
                string str => str,
                System.Text.Json.JsonElement jsonElement => jsonElement.GetString() ?? string.Empty,
                _ => string.Empty
            };
        }
        return string.Empty;
    }

    private async Task HandleCaptionChange(ChangeEventArgs e)
    {
        var caption = e.Value?.ToString() ?? string.Empty;
        await OnImageCaptionChanged.InvokeAsync((NoteId, Block.Id, caption));
    }

    private void HandleImageDragOver()
    {
        // Allow drop
    }

    private async Task HandleImageDrop()
    {
        // This will be handled by JavaScript for file drops
        await Task.CompletedTask;
    }

    private int GetImageWidthPercent()
    {
        if (Block.Metadata.TryGetValue("widthPercent", out var widthObj))
        {
            return widthObj switch
            {
                int intValue => intValue,
                long longValue => (int)longValue,
                System.Text.Json.JsonElement jsonElement => jsonElement.GetInt32(),
                _ => 50
            };
        }
        return 50; // Default to 50%
    }

    private async Task HandleImageClick()
    {
        if (!string.IsNullOrEmpty(_imageDataUrl))
        {
            await OnImagePreviewRequested.InvokeAsync((_imageDataUrl, GetImageCaption()));
        }
    }

    [JSInvokable]
    public async Task OnImageResized(int newWidthPercent)
    {
        await OnImageWidthChanged.InvokeAsync((NoteId, Block.Id, newWidthPercent));
    }

    [JSInvokable]
    public async Task OnImagePastedFromClipboard(string imageData, string fileName, string mimeType)
    {
        await OnImagePasted.InvokeAsync((NoteId, Block.Id, imageData, fileName, mimeType));
    }

    // Mermaid block handlers
    private async Task HandleMermaidContentChanged(Block updatedBlock)
    {
        await OnContentChanged.InvokeAsync(updatedBlock);
    }

    private async Task HandleMermaidToggleEditMode()
    {
        await OnMermaidToggleEditMode.InvokeAsync(Block.Id);
    }

    public async Task<string> GetMermaidContent()
    {
        if (_mermaidBlockRef is not null)
        {
            return await _mermaidBlockRef.GetCurrentContent();
        }
        return Block.Content;
    }

    // Table block handlers
    private async Task HandleTableAddRow()
    {
        await OnTableAddRow.InvokeAsync();
    }

    private async Task HandleTableRemoveRow(int rowIndex)
    {
        await OnTableRemoveRow.InvokeAsync(rowIndex);
    }

    private async Task HandleTableAddColumn()
    {
        await OnTableAddColumn.InvokeAsync();
    }

    private async Task HandleTableRemoveColumn(int colIndex)
    {
        await OnTableRemoveColumn.InvokeAsync(colIndex);
    }

    private async Task HandleTableCellChanged((int RowIndex, int ColIndex, string Content) args)
    {
        await OnTableCellChanged.InvokeAsync(args);
    }

    private async Task HandleTableInsertBlockAfter()
    {
        await OnTableInsertBlockAfter.InvokeAsync();
    }

    private async Task HandleMermaidInsertBlockAfter()
    {
        await OnMermaidInsertBlockAfter.InvokeAsync();
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
