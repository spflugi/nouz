# Nouz — Tauri Notes App

A cross-platform note-taking desktop app with AI integration and a block-based editor.

## Tech Stack

- **Tauri v2** — desktop shell (Rust backend)
- **React 19 + TypeScript** — UI
- **Tailwind CSS v4** — styling via `@tailwindcss/vite` plugin
- **Zustand** — state management
- **SQLite** — local storage via `rusqlite` (bundled, no external deps)
- **OpenAI JS SDK** — AI chat with function calling + streaming
- **Mermaid.js** — diagram blocks
- **react-markdown + remark-gfm** — chat markdown rendering

## Project Structure

```
src/                          # React frontend
  components/
    layout/                   # MainLayout, LeftSidebar, RightSidebar, Topbar
    notes/                    # NoteTimeline, NoteCard, BlockRenderer, block types
      blocks/                 # MermaidBlock, TableBlock, ImageBlock
    chat/                     # ChatBot, MarkdownRenderer
    modals/                   # ConfirmationModal, TextInputModal
    settings/                 # SettingsPage
    ui/                       # NotebookItem, NotificationContainer
  store/                      # Zustand stores (notebook, note, chat, settings, notification)
  services/
    db.ts                     # Typed wrappers over Tauri commands (invoke)
    aiService.ts              # OpenAI streaming + tool calling + RAG
  hooks/                      # useResizable, useDebounce
  types/index.ts              # All TypeScript types
  utils/                      # uuid.ts, blocks.ts (block helpers, cosine similarity)
  styles/globals.css          # Tailwind + CSS variable design tokens

src-tauri/                    # Rust backend
  src/
    db/                       # SQLite connection, migrations, models
    commands/                 # Tauri command handlers (notebooks, notes, attachments, embeddings, preferences)
    lib.rs                    # App entry, command registration
```

## Database

SQLite at `~/Library/Application Support/nouz/nouz.db` (macOS).

Tables: `notebooks`, `notes`, `blocks`, `attachments`, `embeddings`, `preferences`

## Running

```bash
npm run tauri dev     # development (starts Vite + Tauri)
npm run build         # Vite production build only
npm run tauri build   # full desktop app bundle
```

## Block Types

17 block types: `paragraph`, `h1`–`h4`, `listitem`, `todoitem`, `agendaitem`, `code`, `quote`, `decision`, `warning`, `idea`, `divider`, `image`, `mermaid`, `table`

Blocks are stored in the `blocks` table with `block_type`, `content`, and `metadata` (JSON string).

## AI Features

- **Chat** in right sidebar with OpenAI streaming responses
- **Tool calling**: list/create notebooks, CRUD notes, search notes
- **RAG**: embeddings stored in SQLite, cosine similarity retrieval
- API key, model, top-N context notes, similarity threshold all configurable in Settings

## Design System

Warm-gray monochromatic palette (light + dark). Design tokens as CSS custom properties in `src/styles/globals.css`. Dark mode via `[data-theme="dark"]` on `<html>`.

Theme is applied at startup from stored preferences (no restart needed for theme toggle).

## Key Conventions

- **No `import React`** — project uses the new JSX transform (`react-jsx`)
- **Inline styles** — components use inline `style` objects (not Tailwind classes) for component-level styles; Tailwind utilities are used for typography classes (`block-h1`, `selectable`, etc.) in `globals.css`
- **Auto-save** — notes save 800ms after last keystroke; explicit save button also available
- **Block metadata** — stored as JSON string in DB; use `parseMetadata<T>()` / `stringifyMetadata()` helpers
- **Tauri commands** — all Rust commands are snake_case; all invocations go through `src/services/db.ts`
