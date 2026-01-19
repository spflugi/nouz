# Implementation Plan: Rich Text Formatting & New Block Types

## Executive Summary

This plan extends the block-based editor to support:
1. **Bold and Italic formatting** for paragraphs with selection-based toolbar
2. **Text color** for paragraphs
3. **TodoItem block** (checkbox-style task list)
4. **Code block** (syntax-highlighted code)

### Key Design Decision: Search-Friendly Rich Text

To prevent formatting markup from affecting search results, we'll use a **metadata-based formatting approach**:

- **`Content`**: Remains plain text (fully searchable)
- **`Metadata.formatting`**: Stores formatting ranges as a structured array

```json
{
  "formatting": [
    { "start": 0, "end": 5, "type": "bold" },
    { "start": 10, "end": 15, "type": "italic" },
    { "start": 20, "end": 25, "type": "color", "value": "#ff0000" }
  ]
}
```

**Benefits:**
- Search operates on clean plain text
- No HTML tags or markdown in Content field
- Flexible formatting that can be extended
- Works with existing Metadata persistence (JSON-serialized)

---

## Part 1: Rich Text Formatting (Bold, Italic, Color)

### 1.1 Domain Layer Changes

**File: `Nouz.Domain/Entities/TextFormat.cs`** (NEW)
```csharp
public sealed record TextFormat
{
    public required int Start { get; init; }
    public required int End { get; init; }
    public required TextFormatType Type { get; init; }
    public string? Value { get; init; }  // For color: hex value
}

public enum TextFormatType
{
    Bold,
    Italic,
    Color
}
```

**File: `Nouz.Domain/Extensions/BlockExtensions.cs`** (NEW)
```csharp
public static class BlockExtensions
{
    public static IReadOnlyList<TextFormat> GetFormatting(this Block block);
    public static Block WithFormatting(this Block block, IReadOnlyList<TextFormat> formatting);
    public static Block AddFormat(this Block block, TextFormat format);
    public static Block RemoveFormat(this Block block, int start, int end, TextFormatType type);
}
```

### 1.2 UI Components

**File: `Nouz/Components/Notes/FormattingToolbar.razor`** (NEW)

A floating toolbar that appears when text is selected:
- Bold button (B)
- Italic button (I)
- Color picker dropdown

**Implementation:**
1. JavaScript detects `selectionchange` event
2. If selection is within a paragraph block, show toolbar near selection
3. On button click, calculate selection range and update block metadata
4. Re-render content with formatting applied

**File: `Nouz/wwwroot/nouz.js`** (MODIFY)

Add new functions:
- `getSelectionRange(element)`: Returns `{ start, end }` of selected text
- `setSelectionRange(element, start, end)`: Restores selection after re-render
- `initFormattingToolbar(element, dotNetRef)`: Sets up selection listeners

### 1.3 BlockRenderer Changes

**File: `Nouz/Components/Notes/BlockRenderer.razor`** (MODIFY)

- Replace `innerText` with formatted HTML render
- Apply `<strong>`, `<em>`, `<span style="color:">` based on Metadata formatting
- Use `innerHTML` for render but extract `innerText` for Content

**Content Extraction Strategy:**
- On save: `getElementText()` returns `innerText` (plain text)
- Formatting persists in Metadata (separate from content)

### 1.4 Unit Tests

**File: `Nouz.Domain.Unit.Tests/BlockExtensionsTests.cs`** (NEW)
- Test formatting range calculations
- Test overlapping format handling
- Test format serialization/deserialization

---

## Part 2: TodoItem Block

### 2.1 Current State
- `BlockType.TodoItem` already exists in `BlockType.cs`
- Falls through to default (Paragraph) in `BlockRenderer.razor`
- `BlockContextMenu.razor` does not include TodoItem option

### 2.2 Implementation

**File: `Nouz/Components/Notes/BlockRenderer.razor`** (MODIFY)

Add case for TodoItem:
```razor
case BlockType.TodoItem:
    <div class="block block-todo-item">
        <input type="checkbox"
               checked="@GetCheckedState()"
               @onchange="HandleTodoCheckedChange" />
        <span class="todo-content"
              contenteditable="true"
              @ref="_contentRef"
              data-placeholder="To-do"></span>
    </div>
    break;
```

**Metadata for TodoItem:**
```json
{
  "checked": true
}
```

**File: `Nouz/Components/Notes/BlockContextMenu.razor`** (MODIFY)

Add TodoItem to the menu:
```razor
<button class="menu-item @(CurrentType == BlockType.TodoItem ? "active" : "")"
        @onclick="@(() => OnTypeSelected.InvokeAsync(BlockType.TodoItem))">
    <RadzenIcon Icon="check_box" />
    <span>To-do</span>
</button>
```

**File: `Nouz/Components/Notes/BlockRenderer.razor.css`** (MODIFY)

Add styles:
```css
.block-todo-item {
    display: flex;
    align-items: flex-start;
    gap: var(--space-2);
    font-size: 13px;
    line-height: 1.6;
}

.block-todo-item input[type="checkbox"] {
    margin-top: 4px;
    accent-color: var(--accent-color);
}

.block-todo-item.checked .todo-content {
    text-decoration: line-through;
    color: var(--text-muted);
}

.todo-content {
    flex: 1;
    min-width: 0;
    outline: none;
}
```

### 2.3 Smart Enter Behavior

**File: `Nouz/Components/Notes/BlockRenderer.razor.cs`** (MODIFY)

Update `OnEnterKeyPressed` to handle TodoItem:
```csharp
var newBlockType = Block.Type switch
{
    BlockType.ListItem => BlockType.ListItem,
    BlockType.TodoItem => BlockType.TodoItem,  // ADD THIS
    _ => BlockType.Paragraph
};
```

### 2.4 Unit Tests

**File: `Nouz.Infrastructure.Unit.Tests/Repositories/BlockPersistenceTests.cs`** (MODIFY)
- Add tests for TodoItem metadata (checked state) persistence

---

## Part 3: Code Block

### 3.1 Current State
- `BlockType.Code` already exists in `BlockType.cs`
- Falls through to default (Paragraph) in `BlockRenderer.razor`
- `BlockContextMenu.razor` does not include Code option

### 3.2 Implementation

**File: `Nouz/Components/Notes/BlockRenderer.razor`** (MODIFY)

Add case for Code:
```razor
case BlockType.Code:
    <pre class="block block-code">
        <code contenteditable="true"
              @ref="_contentRef"
              data-placeholder="Code"
              spellcheck="false"></code>
    </pre>
    break;
```

**Metadata for Code (optional, for future syntax highlighting):**
```json
{
  "language": "csharp"
}
```

**File: `Nouz/Components/Notes/BlockContextMenu.razor`** (MODIFY)

Add Code to the menu:
```razor
<button class="menu-item @(CurrentType == BlockType.Code ? "active" : "")"
        @onclick="@(() => OnTypeSelected.InvokeAsync(BlockType.Code))">
    <RadzenIcon Icon="code" />
    <span>Code</span>
</button>
```

**File: `Nouz/Components/Notes/BlockRenderer.razor.css`** (MODIFY)

Add styles:
```css
.block-code {
    background-color: var(--bg-secondary);
    border-radius: var(--radius-sm);
    padding: var(--space-2) var(--space-3);
    font-family: 'Consolas', 'Monaco', monospace;
    font-size: 12px;
    line-height: 1.5;
    overflow-x: auto;
    white-space: pre;
    margin: var(--space-1) 0;
}

.block-code code {
    outline: none;
    display: block;
    min-height: 1.5em;
}

.block-code code:empty::before {
    content: attr(data-placeholder);
    color: var(--text-placeholder);
    pointer-events: none;
}
```

### 3.3 Enter Key Behavior

For code blocks, Enter should create a new line within the block (not a new block).
Shift+Enter can create a new block after the code block.

**File: `Nouz/wwwroot/nouz.js`** (MODIFY)

Update `initBlockEditor` to handle code blocks differently:
```javascript
// For code blocks, allow Enter to create newlines
if (element.closest('.block-code')) {
    // Don't prevent default - allow normal newline behavior
    return;
}
```

---

## Part 4: Text Color Support

### 4.1 Integration with Formatting Toolbar

The FormattingToolbar will include a color picker:
- Predefined palette of ~8-10 colors
- Applies color as a formatting range in Metadata

**Color Palette:**
```
- Default (inherit)
- Red (#ef4444)
- Orange (#f97316)
- Yellow (#eab308)
- Green (#22c55e)
- Blue (#3b82f6)
- Purple (#a855f7)
- Pink (#ec4899)
- Gray (#6b7280)
```

### 4.2 Implementation

**File: `Nouz/Components/Notes/FormattingToolbar.razor`** (NEW - already planned in Part 1)

Add color picker dropdown to the toolbar.

---

## Action Items Summary

### Phase 1: Foundation (TodoItem & Code Blocks)
1. [ ] Update `BlockRenderer.razor` - add TodoItem case
2. [ ] Update `BlockRenderer.razor` - add Code case
3. [ ] Update `BlockRenderer.razor.cs` - add `GetCheckedState()` and `HandleTodoCheckedChange()`
4. [ ] Update `BlockRenderer.razor.cs` - update `OnEnterKeyPressed` for TodoItem
5. [ ] Update `BlockRenderer.razor.css` - add TodoItem and Code styles
6. [ ] Update `BlockContextMenu.razor` - add TodoItem and Code options
7. [ ] Update `nouz.js` - handle Enter key differently for Code blocks
8. [ ] Add unit tests for TodoItem metadata persistence

### Phase 2: Rich Text Formatting
9. [ ] Create `TextFormat.cs` and `TextFormatType.cs` in Domain layer
10. [ ] Create `BlockExtensions.cs` with formatting helper methods
11. [ ] Create `FormattingToolbar.razor` component
12. [ ] Update `nouz.js` - add selection range functions
13. [ ] Update `BlockRenderer.razor` - render formatted content with HTML
14. [ ] Update `BlockRenderer.razor.cs` - handle formatting updates
15. [ ] Add CSS for formatting toolbar
16. [ ] Add unit tests for BlockExtensions

### Phase 3: Text Color
17. [ ] Add color picker to FormattingToolbar
18. [ ] Add CSS for color picker dropdown
19. [ ] Test color formatting persistence and rendering

---

## File Changes Summary

| File | Action | Description |
|------|--------|-------------|
| `Nouz.Domain/Entities/TextFormat.cs` | NEW | Formatting range model |
| `Nouz.Domain/Entities/TextFormatType.cs` | NEW | Formatting type enum |
| `Nouz.Domain/Extensions/BlockExtensions.cs` | NEW | Block formatting helpers |
| `BlockRenderer.razor` | MODIFY | Add TodoItem, Code, formatting render |
| `BlockRenderer.razor.cs` | MODIFY | Add formatting logic, checkbox handling |
| `BlockRenderer.razor.css` | MODIFY | Add styles for new block types |
| `BlockContextMenu.razor` | MODIFY | Add TodoItem and Code options |
| `FormattingToolbar.razor` | NEW | Selection-based formatting toolbar |
| `FormattingToolbar.razor.css` | NEW | Toolbar styles |
| `nouz.js` | MODIFY | Selection handling, code block Enter |
| `BlockExtensionsTests.cs` | NEW | Unit tests for formatting |
| `BlockPersistenceTests.cs` | MODIFY | Add TodoItem/Code tests |

---

## Testing Strategy

### Unit Tests
- `BlockExtensionsTests`: Formatting range operations
- `BlockPersistenceTests`: TodoItem and Code metadata persistence
- Search tests: Verify formatting doesn't affect search results

### Manual Testing Checklist
- [ ] Create TodoItem, toggle checkbox, verify persistence
- [ ] Create Code block, verify multi-line support
- [ ] Select text in paragraph, apply bold, verify display
- [ ] Select text in paragraph, apply italic, verify display
- [ ] Select text in paragraph, apply color, verify display
- [ ] Search for text that has formatting - should find by plain text
- [ ] Search for "strong" or "italic" - should NOT find formatted text
- [ ] Convert paragraph with formatting to heading - formatting should clear
- [ ] Press Enter in TodoItem - creates new TodoItem
- [ ] Press Enter in Code block - creates newline in same block

---

## Architecture Compliance

This plan adheres to the existing architecture:
- **Domain Layer**: New records in `Nouz.Domain/Entities`
- **Redux Pattern**: Formatting changes use existing `UpdateBlock` command
- **Component Pattern**: New components extend `SharedComponentBase` where needed
- **CSS**: Scoped component CSS using existing CSS variables
- **Testing**: NSubstitute for mocks, Shouldly for assertions, SQLite in-memory for integration

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Cursor position lost after re-render | Store and restore selection using JS |
| Performance with many formats | Limit formatting array size, optimize rendering |
| Complex overlapping formats | Use simple merge/split logic in BlockExtensions |
| Mobile selection behavior | Test on MAUI WebView, adjust JS as needed |
