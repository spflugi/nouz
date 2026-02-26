# Nouz — Application Specifications

## Table of Contents

1. [Overview](#1-overview)
2. [Functional Specifications](#2-functional-specifications)
   - 2.1 [Notebooks](#21-notebooks)
   - 2.2 [Notes](#22-notes)
   - 2.3 [Blocks](#23-blocks)
   - 2.4 [Text Formatting](#24-text-formatting)
   - 2.5 [Search](#25-search)
   - 2.6 [Attachments](#26-attachments)
   - 2.7 [AI Chat](#27-ai-chat)
   - 2.8 [Settings](#28-settings)
   - 2.9 [Notifications](#29-notifications)
   - 2.10 [Export](#210-export)
   - 2.11 [Application Lifecycle](#211-application-lifecycle)
3. [Non-Functional Specifications](#3-non-functional-specifications)
   - 3.1 [Architecture](#31-architecture)
   - 3.2 [State Management](#32-state-management)
   - 3.3 [Persistence](#33-persistence)
   - 3.4 [Performance](#34-performance)
   - 3.5 [Security](#35-security)
   - 3.6 [Reliability & Error Handling](#36-reliability--error-handling)
   - 3.7 [Observability](#37-observability)
   - 3.8 [Testability](#38-testability)
   - 3.9 [Design & Accessibility](#39-design--accessibility)
   - 3.10 [Maintainability](#310-maintainability)

---

## 1. Overview

**Nouz** is a desktop note-taking application for Windows built with .NET 10 MAUI and a Blazor WebView UI. Notes are structured as ordered sequences of typed blocks, organized within notebooks. The app integrates with OpenAI to provide AI-assisted chat with semantic context retrieval from the user's notes.

| Attribute        | Value |
|------------------|-------|
| Platform         | Windows (win10-x64) |
| Framework        | .NET 10 / MAUI + Blazor WebView |
| Database         | SQLite via EF Core 10 |
| AI Integration   | OpenAI (chat + embeddings) via Semantic Kernel |
| Architecture     | Clean Architecture + CQRS + Redux |

---

## 2. Functional Specifications

### 2.1 Notebooks

A **Notebook** is the top-level organizational unit that groups related notes.

| Property       | Type             | Constraints |
|----------------|------------------|-------------|
| `Id`           | `Guid`           | System-generated, unique |
| `Name`         | `string`         | Required, non-empty |
| `CreatedAt`    | `DateTimeOffset` | Set on creation |
| `LastModifiedAt` | `DateTimeOffset` | Updated on every change |
| `SortOrder`    | `int`            | Used for display ordering |

**Operations:**

| Operation | Trigger | Behaviour |
|-----------|---------|-----------|
| Load all notebooks | App start | Notebooks loaded into state; sorted by name |
| Create notebook | User action | Prompts for title; creates record; selects the new notebook |
| Select notebook | User action | Sets active notebook; loads its notes |
| Rename notebook | User action | Prompts for new title; updates record |
| Delete notebook | User action | Requires confirmation; cascades to delete all contained notes and their blocks |

**UI:**
- Left sidebar lists all notebooks.
- Active notebook is highlighted.
- Inline modals for create/rename (text input) and delete (confirmation).
- Sidebar is resizable (min 120 px, max 600 px) and collapsible.

---

### 2.2 Notes

A **Note** belongs to exactly one notebook and contains an ordered list of blocks.

| Property        | Type                     | Constraints |
|-----------------|--------------------------|-------------|
| `Id`            | `Guid`                   | System-generated, unique |
| `NotebookId`    | `Guid`                   | Foreign key to Notebook |
| `CreatedAt`     | `DateTimeOffset`         | Set on creation |
| `LastModifiedAt`| `DateTimeOffset`         | Updated on every block change |
| `Blocks`        | `ImmutableList<Block>`   | Ordered; at least one block after creation |

**Operations:**

| Operation | Behaviour |
|-----------|-----------|
| Create note | Adds a note with one empty Paragraph block; selects it |
| Create meeting note | Adds a note pre-populated with a structured meeting template (heading + agenda items + todo items) |
| Delete note | Requires confirmation; removes note and all its blocks and attachments |
| Move note | Moves note to a different notebook; clears current note list view |
| Export note as HTML | Renders note blocks to an HTML string for clipboard/file use |

**Display:**
- Notes shown as cards in a timeline view within the main content area.
- Searchbar filters notes within the active notebook.

---

### 2.3 Blocks

A **Block** is the atomic content unit within a note. Notes are rendered as an ordered sequence of blocks.

| Property    | Type                            | Constraints |
|-------------|---------------------------------|-------------|
| `Id`        | `Guid`                          | System-generated, unique |
| `Type`      | `BlockType`                     | See table below |
| `Content`   | `string`                        | Text content; may be empty |
| `Metadata`  | `Dictionary<string, object>`    | Type-specific extra data |
| `Order`     | `int`                           | Ascending integer; determines render order |

**Block Types:**

| Type          | Description | Notes |
|---------------|-------------|-------|
| `Paragraph`   | Default text block | Editable contenteditable div |
| `H1` – `H4`  | Heading levels 1–4 | Styled headings; same edit model as Paragraph |
| `ListItem`    | Bullet list item | Prefixed bullet character |
| `AgendaItem`  | Agenda entry | Prefixed chevron character |
| `TodoItem`    | Checkbox task | Checkbox + text span; checkbox toggles content |
| `Code`        | Code snippet | `<pre><code>` render; spellcheck disabled |
| `Quote`       | Block quote | Left-border styled |
| `Decision`    | Decision record | Distinct icon/style |
| `Warning`     | Warning callout | Distinct icon/style |
| `Idea`        | Idea callout | Distinct icon/style |
| `Divider`     | Horizontal rule | No content; non-editable |
| `Image`       | Inline image | Base64 data stored in Metadata; supports caption and width |
| `Mermaid`     | Diagram block | Toggle between diagram preview and raw text edit mode |
| `Table`       | Editable table | Rows/columns managed via dedicated commands |

**Block Operations:**

| Operation | Behaviour |
|-----------|-----------|
| Add block | Inserts new block after a given block (or at end); default type Paragraph |
| Update block | Replaces block content and/or metadata in place |
| Delete block | Removes block; reorders remaining blocks |
| Change block type | Converts block to a new type, preserving content where applicable |
| Reorder block | Moves block to a new index position |
| Set editing block | Tracks which block has active focus for toolbar and context menu |

**Image Block specifics:**

| Sub-operation | Constraints |
|---------------|-------------|
| Add image block | Accepts base64-encoded image data, filename, and MIME type |
| Update caption | Sets optional caption text on the image |
| Update width | Integer percentage 10–100 (%) controlling render width |

**Mermaid Block specifics:**
- Toggles between rendered diagram view and raw text edit mode via `ToggleMermaidEditMode`.

**Table Block specifics:**

| Sub-operation | Behaviour |
|---------------|-----------|
| Add row | Inserts row after given index (or appends) |
| Remove row | Deletes row at index |
| Add column | Inserts column after given index (or appends) |
| Remove column | Deletes column at index |
| Update cell | Sets cell content at (row, column) |

---

### 2.4 Text Formatting

Inline text formatting is applied to ranges within a block's content string.

**TextFormat record:**

| Property | Type              | Description |
|----------|-------------------|-------------|
| `Start`  | `int`             | Inclusive character start index |
| `End`    | `int`             | Exclusive character end index |
| `Type`   | `TextFormatType`  | `Bold`, `Italic`, or `Color` |
| `Value`  | `string?`         | Color value for `Color` type |

**Rules:**
- Formatting ranges are stored in `Block.Metadata["formatting"]` as JSON.
- Overlapping ranges of the same type are merged on save.
- Ranges are clamped to actual content length after content edits.
- Toggle behaviour: applying a format that already covers the entire selection removes it; otherwise adds it.
- Partial-overlap removal splits existing ranges at boundaries.

**Supported formats:**

| Format  | HTML Output |
|---------|-------------|
| Bold    | `<strong>` |
| Italic  | `<em>` |
| Color   | `<span style="color: VALUE">` |

**UI:**
- Floating `FormattingToolbar` appears on text selection.
- Block context menu allows block-level type changes.

---

### 2.5 Search

Full-text search across all blocks within the currently selected notebook.

**Behaviour:**
- Input is debounced before dispatching the search command.
- Search is performed using SQL `LIKE` pattern matching against block content.
- Results replace the note list until the search is cleared.
- Clearing the search bar restores the full note list.
- Search is scoped to the active notebook only.

---

### 2.6 Attachments

Files can be attached to individual notes.

**NoteAttachment record:**

| Property       | Type             | Description |
|----------------|------------------|-------------|
| `Id`           | `Guid`           | Unique attachment identifier |
| `NoteId`       | `Guid`           | Owning note |
| `FileName`     | `string`         | Original filename |
| `Extension`    | `string`         | File extension |
| `FileSizeBytes`| `long`           | File size in bytes |
| `ContentHash`  | `string`         | SHA-256 hash for duplicate detection |
| `AddedAt`      | `DateTimeOffset` | Timestamp |

**Constraints:**
- Maximum file size: **50 MB** per attachment.
- Duplicate detection: uploading a file with the same SHA-256 hash to the same note is rejected.

**Storage:**
- Files stored at `AppDataDirectory/note-attachments/{NoteId}/{AttachmentId}_{FileName}`.
- Metadata stored in the SQLite database.

**Operations:**

| Operation | Behaviour |
|-----------|-----------|
| Load attachments | Loads attachment metadata for a given note |
| Add attachment | Validates size; computes hash; rejects duplicates; saves file + metadata |
| Delete attachment | Removes file from disk and metadata from database |
| Open attachment | Opens file using the OS default application |

---

### 2.7 AI Chat

An integrated chat interface powered by OpenAI that is context-aware of the user's notes.

**Chat Message record:**

| Property            | Type                    | Description |
|---------------------|-------------------------|-------------|
| `Id`                | `Guid`                  | Unique message identifier |
| `Content`           | `string`                | Message text |
| `Role`              | `ChatMessageRole`       | `User` or `Assistant` |
| `Timestamp`         | `DateTimeOffset`        | When sent/received |
| `ContextNoteTitles` | `ImmutableList<string>?`| Titles of notes injected as context |

**Operations:**

| Operation | Behaviour |
|-----------|-----------|
| Send message | Finds semantically relevant notes; includes them as context in the OpenAI request; appends user and assistant messages to history |
| Send message (streaming) | Same as above but streams the assistant response token-by-token; tracks streaming message ID |
| Clear chat | Removes all messages from the chat history |

**Semantic Context Retrieval:**
1. Generate an embedding for the user's message.
2. Query stored note embeddings using cosine similarity.
3. Return the top N notes with similarity ≥ `MinSimilarityThreshold`.
4. Note title is derived from the first H1 block, or the first block's content as fallback.
5. Relevant note titles are attached to the assistant message for UI display.

---

### 2.8 Settings

User-configurable settings persisted across sessions.

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `ThemeMode` | `ThemeMode` | `Light` | Light or Dark UI theme |
| `OpenAiApiKey` | `string?` | — | OpenAI API key for chat and embeddings |
| `OpenAiAdminKey` | `string?` | — | OpenAI admin key for usage data |
| `OpenAiChatModel` | `string` | `gpt-4o-mini` | Model used for AI chat |
| `OpenAiEmbeddingModel` | `string` | `text-embedding-3-small` | Model used for note embeddings |
| `TopNRelevantNotes` | `int` | `3` | Max notes injected as context per chat message |
| `MinSimilarityThreshold` | `float` | `0.3` | Minimum cosine similarity to include a note as context |

**Additional:**
- OpenAI usage data (input tokens, output tokens, request count, estimated cost USD, period) can be loaded from the OpenAI admin API and displayed in the settings page.
- Sensitive keys (API keys) stored using MAUI `SecureStorage`.

---

### 2.9 Notifications

In-app transient notifications for user feedback.

| Property    | Type                   | Description |
|-------------|------------------------|-------------|
| `Id`        | `Guid`                 | Unique notification identifier |
| `Title`     | `string`               | Short title |
| `Message`   | `string`               | Detail message |
| `Severity`  | `NotificationSeverity` | Determines styling (e.g. error, info) |

**Behaviour:**
- Notifications appear in a floating container overlaying the main content.
- Each notification can be dismissed individually.
- Handlers dispatch `ShowNotification` on error or significant events.

---

### 2.10 Export

| Operation | Output |
|-----------|--------|
| Export note as HTML | Renders all blocks to an HTML string; inline styles applied per block type and text formatting |

---

### 2.11 Application Lifecycle

| Event | Behaviour |
|-------|-----------|
| App start (`OnAppStart`) | Apply EF Core migrations; load settings; load notebooks; select first notebook if available |
| App resume (`OnAppResume`) | Restore any state needed after the app returns from background |
| App sleep (`OnAppSleep`) | Persist any pending state before the app enters background |

---

## 3. Non-Functional Specifications

### 3.1 Architecture

| Attribute | Detail |
|-----------|--------|
| Pattern | Clean Architecture with CQRS and Redux-style state management |
| Layers | Domain → Application → Infrastructure; UI (MAUI/Blazor) consumes Application via DI |
| Dependency rule | Domain has no outward dependencies; Application depends on Domain only; Infrastructure implements Domain interfaces; only `ServiceCollectionExtensions` classes are `public` in Infrastructure |
| Code separation | Blazor components split into `.razor` (markup), `.razor.cs` (code-behind), and `.razor.css` (styles) |
| Warnings | `TreatWarningsAsErrors=true` in all projects |
| Nullability | Nullable reference types enabled solution-wide |

**Layer Responsibilities:**

| Layer | Responsibility |
|-------|----------------|
| `Nouz.Domain` | Entities, value objects, repository interfaces — no framework dependencies |
| `Nouz.Application` | Commands, handlers, actions, reducers, state records — depends on Domain only |
| `Nouz.Infrastructure` | EF Core/SQLite repos, file storage, preferences, OpenAI services — implements Domain interfaces |
| `Nouz` (MAUI) | Blazor UI components, DI wiring, app host — consumes Application layer |
| `Nouz.ReduxSimple` | Custom Redux store library — independent, reusable |

---

### 3.2 State Management

| Attribute | Detail |
|-----------|--------|
| Pattern | Redux: immutable state, actions, reducers |
| State type | `RootState` — immutable sealed record composed of sub-states |
| Reactivity | `System.Reactive` (`IObservable<IState>`) for UI subscriptions |
| Command dispatch | `IMediator` (Mediator library 3.0.1 with source generation) |
| Immutability | All state records use `ImmutableList<T>` and `ImmutableDictionary<K,V>`; updated via `with` expressions |
| Undo/Redo | Supported at the `ReduxStore` level (optional time-travel) |

**State tree:**

```
RootState
├── NotebookState    (selected notebook ID, notebooks list)
├── NoteState        (notes, search query, filtered notes, editing block, attachments)
├── SettingsState    (theme, OpenAI config, usage data)
├── ChatState        (messages, typing indicator, streaming message ID)
└── NotificationState (active notifications)
```

---

### 3.3 Persistence

| Attribute | Detail |
|-----------|--------|
| Database | SQLite via EF Core 10 code-first migrations |
| DB path (release) | `AppDataDirectory/nouz.db` |
| DB path (debug) | `AppDataDirectory/dev/nouz.db` |
| Migrations | Applied automatically on `OnAppStart` via `IDbMigrator` |
| File attachments | `AppDataDirectory/note-attachments/{NoteId}/{AttachmentId}_{FileName}` |
| Embeddings | Float arrays binary-serialized via `BinaryPrimitives` in the `NoteEmbedding` table |
| Sensitive data | OpenAI API keys stored in MAUI `SecureStorage` |
| Block metadata | Stored as JSON column in the Blocks table |

---

### 3.4 Performance

| Attribute | Detail |
|-----------|--------|
| Search debounce | Search input is debounced to avoid per-keystroke queries |
| Streaming chat | Assistant responses streamed token-by-token to minimize perceived latency |
| Embedding similarity | Cosine similarity computed in-process over stored float arrays |
| Attachment size limit | 50 MB per file to prevent excessive memory/disk usage |

---

### 3.5 Security

| Attribute | Detail |
|-----------|--------|
| API key storage | OpenAI keys stored in OS-level secure storage (MAUI `SecureStorage`) — not in plain-text files or DB |
| Duplicate detection | SHA-256 hash computed per attachment to prevent re-uploading identical files |
| Local-only data | All notes and embeddings stored locally; only embeddings and chat messages are sent to OpenAI |
| Input validation | File size validated before storage; duplicate hash checked before write |

---

### 3.6 Reliability & Error Handling

| Attribute | Detail |
|-----------|--------|
| Error surfacing | All command handler errors are caught and surfaced via `NotificationCommands.ShowNotification` |
| Graceful degradation | AI features (chat, semantic search) degrade gracefully when OpenAI key is absent or API is unreachable |
| DB migrations | Applied at startup to ensure schema is always up to date before use |
| Logging | Errors and significant events logged via Serilog before user notification |

---

### 3.7 Observability

| Attribute | Detail |
|-----------|--------|
| Logging framework | Serilog 4.x |
| Sinks | Console, Debug, daily rolling file (`logs/nouz-{date}.log`) |
| Enrichers | `FromLogContext`, `WithThreadId`, `WithProcessId` |
| Minimum level | `Information` (configurable via embedded `appsettings.json`) |
| Abstraction | `ILoggerAdapter<T>` wraps `ILogger<T>` for testability |

---

### 3.8 Testability

| Attribute | Detail |
|-----------|--------|
| Test framework | xUnit 2.9.3 / xUnit v3 3.2.2 |
| Mocking | NSubstitute 5.3.0 |
| Assertions | Shouldly 4.3.0 |
| Architecture tests | NetArchTest.Rules 1.3.2 — enforces layer dependency rules |
| Coverage | coverlet.collector 8.0.0 |

**Test projects:**

| Project | Scope |
|---------|-------|
| `Nouz.Application.Unit.Tests` | Command handlers, reducers, business logic; repositories mocked via NSubstitute |
| `Nouz.Infrastructure.Unit.Tests` | EF Core mappings, repository behaviour, file storage |
| `Nouz.Architecture.Tests` | Layer dependency rules, public surface-area rules |

**Workflow rule:** After each implementation step, add relevant unit/integration tests and run them before proceeding.

---

### 3.9 Design & Accessibility

| Attribute | Detail |
|-----------|--------|
| Visual style | Minimalistic, near-monochrome, warm-toned palette |
| Theme | Light and Dark modes; toggled via settings and applied via JS interop |
| Component library | Radzen.Blazor 9.x (Material theme, software variant) |
| Layout | Three-column layout: left sidebar (notebooks), main content (notes/chat), right sidebar |
| Sidebar | Resizable (120–600 px) and collapsible |
| Block editing | In-place `contenteditable` editing for all text block types |
| Image editing | Drag handles for width resize; inline caption input |
| Diagrams | Mermaid diagram rendering with toggle to raw-text edit mode |

---

### 3.10 Maintainability

| Attribute | Detail |
|-----------|--------|
| Package versions | Centralized in `Nouz/Directory.Packages.props` — single source of truth |
| DI registration | Each layer exposes extension methods on `IServiceCollection`; composed in `MauiProgram.cs` |
| Source generation | Mediator handler registration via compile-time source generation (no reflection at runtime) |
| Code style | CSS in `.razor.css`, C# code in `.razor.cs`, markup in `.razor` — enforced by project convention |
| Domain model | All entities are immutable sealed records; mutations produce new instances |
