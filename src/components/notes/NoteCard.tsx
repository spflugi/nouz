import React, { useEffect, useRef, useState } from 'react';
import type { AttachmentMeta, Block, BlockType, Note, Notebook } from '../../types';
import { useNoteStore } from '../../store/noteStore';
import { useNotificationStore } from '../../store/notificationStore';
import { newBlock, reindexBlocks, parseMetadata } from '../../utils/blocks';
import { save } from '@tauri-apps/plugin-dialog';
import { writeTextFile } from '@tauri-apps/plugin-fs';

import { BlockRenderer } from './BlockRenderer';
import { ConfirmationModal } from '../modals/ConfirmationModal';
import { AttachmentPanel } from './AttachmentPanel';

function blockToHtml(block: Block): string {
  const c = block.content;
  const meta = parseMetadata(block.metadata) as Record<string, any>;
  switch (block.block_type) {
    case 'h1': return `<h1>${c}</h1>`;
    case 'h2': return `<h2>${c}</h2>`;
    case 'h3': return `<h3>${c}</h3>`;
    case 'h4': return `<h4>${c}</h4>`;
    case 'listitem': {
      const indent = (meta.indent ?? 0) * 20;
      return `<ul style="padding-left:${1.5 + indent / 16}em;margin:0.15em 0"><li>${c}</li></ul>`;
    }
    case 'agendaitem': {
      const indent = (meta.indent ?? 0) * 20;
      return `<ul style="padding-left:${1.5 + indent / 16}em;margin:0.15em 0;list-style:'› '"><li>${c}</li></ul>`;
    }
    case 'todoitem': {
      const checked = meta.checked ? 'checked' : '';
      const cls = meta.checked ? ' checked' : '';
      return `<div class="todo-item${cls}"><input type="checkbox" disabled ${checked}><span>${c}</span></div>`;
    }
    case 'code': return `<pre><code>${c}</code></pre>`;
    case 'quote': return `<blockquote>${c}</blockquote>`;
    case 'decision': return `<div class="callout decision"><span class="callout-label">Decision</span><span>${c}</span></div>`;
    case 'warning': return `<div class="callout warning"><span class="callout-label">Warning</span><span>${c}</span></div>`;
    case 'idea': {
      const status = meta.status ?? 'new';
      return `<div class="callout idea"><span>💡</span><span>${c}</span><span class="idea-status">${status}</span></div>`;
    }
    case 'divider': return `<hr>`;
    case 'image': {
      const w = meta.width_percent ?? 100;
      const src = meta.data_url ?? '';
      const cap = meta.caption ?? '';
      if (!src) return '';
      return `<figure style="text-align:center;margin:0.5em 0"><img src="${src}" alt="${cap}" style="width:${w}%"><figcaption>${cap}</figcaption></figure>`;
    }
    case 'mermaid': return `<pre><code class="language-mermaid">${c}</code></pre>`;
    case 'table': {
      const headers: string[] = meta.headers ?? [];
      const rows: string[][] = meta.rows ?? [];
      const thead = headers.map((h) => `<th>${h}</th>`).join('');
      const tbody = rows.map((row) => `<tr>${row.map((cell) => `<td>${cell}</td>`).join('')}</tr>`).join('');
      return `<table><thead><tr>${thead}</tr></thead><tbody>${tbody}</tbody></table>`;
    }
    default: return `<p>${c}</p>`;
  }
}

interface Props {
  note: Note;
  notebooks: Notebook[];
  attachments: AttachmentMeta[];
  isEditing: boolean;
}

export function NoteCard({ note, notebooks, attachments, isEditing }: Props) {
  const { saveNote, deleteNote, moveNote, setEditing, loadAttachments, addAttachment, deleteAttachment } = useNoteStore();
  const { add: notify } = useNotificationStore();
  const [blocks, setBlocks] = useState<Block[]>(note.blocks);
  const [showContextMenu, setShowContextMenu] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [savedPulse, setSavedPulse] = useState(false);
  const saveTimeout = useRef<ReturnType<typeof setTimeout> | null>(null);
  const contextMenuRef = useRef<HTMLDivElement>(null);

  useEffect(() => { setBlocks(note.blocks); }, [note.blocks]);
  useEffect(() => { loadAttachments(note.id); }, [note.id]);

  // Close context menu on outside click
  useEffect(() => {
    if (!showContextMenu) return;
    const handler = (e: MouseEvent) => {
      if (contextMenuRef.current && !contextMenuRef.current.contains(e.target as Node)) {
        setShowContextMenu(false);
      }
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [showContextMenu]);

  const triggerAutoSave = (nextBlocks: Block[]) => {
    if (saveTimeout.current) clearTimeout(saveTimeout.current);
    saveTimeout.current = setTimeout(async () => {
      await saveNote(note.id, nextBlocks);
      setSavedPulse(true);
      setTimeout(() => setSavedPulse(false), 1500);
    }, 800);
  };

  const handleSave = async () => {
    if (saveTimeout.current) clearTimeout(saveTimeout.current);
    await saveNote(note.id, blocks);
    setSavedPulse(true);
    setTimeout(() => setSavedPulse(false), 1500);
  };

  const handleBlockChange = (updated: Block) => {
    const next = blocks.map((b) => (b.id === updated.id ? updated : b));
    setBlocks(next);
    triggerAutoSave(next);
  };

  const handleAddBlock = (afterId: string | null, type: BlockType = 'paragraph', metadata?: string) => {
    const afterIdx = afterId ? blocks.findIndex((b) => b.id === afterId) : blocks.length - 1;
    const insertAt = afterIdx + 1;
    const newB = { ...newBlock(type, note.id, insertAt), ...(metadata !== undefined ? { metadata } : {}) };
    const next = reindexBlocks([
      ...blocks.slice(0, insertAt),
      newB,
      ...blocks.slice(insertAt),
    ]);
    setBlocks(next);
    triggerAutoSave(next);
    // Focus new block after render
    setTimeout(() => {
      const el = document.querySelector<HTMLElement>(`[data-block-id="${newB.id}"] [contenteditable]`);
      el?.focus();
    }, 50);
  };

  const handleDeleteBlock = (id: string) => {
    if (blocks.length <= 1) return;
    const idx = blocks.findIndex((b) => b.id === id);
    const next = reindexBlocks(blocks.filter((b) => b.id !== id));
    setBlocks(next);
    triggerAutoSave(next);
    // Focus previous block
    const prevIdx = Math.max(0, idx - 1);
    setTimeout(() => {
      const prevId = next[prevIdx]?.id;
      if (prevId) {
        const el = document.querySelector<HTMLElement>(`[data-block-id="${prevId}"] [contenteditable]`);
        el?.focus();
        // Move caret to end
        const range = document.createRange();
        const sel = window.getSelection();
        if (el && sel) {
          range.selectNodeContents(el);
          range.collapse(false);
          sel.removeAllRanges();
          sel.addRange(range);
        }
      }
    }, 50);
  };

  const handleReorder = (dragId: string, overIdx: number) => {
    const dragIdx = blocks.findIndex((b) => b.id === dragId);
    if (dragIdx === -1) return;
    const next = [...blocks];
    const [dragged] = next.splice(dragIdx, 1);
    next.splice(overIdx, 0, dragged);
    const reindexed = reindexBlocks(next);
    setBlocks(reindexed);
    triggerAutoSave(reindexed);
  };

  const handleTypeChange = (id: string, type: BlockType) => {
    const next = blocks.map((b) => b.id === id ? { ...b, block_type: type } : b);
    setBlocks(next);
    triggerAutoSave(next);
  };

  const handleDeleteNote = async () => {
    await deleteNote(note.id);
    notify('Note deleted', 'success');
  };

  const handleMoveToNotebook = async (notebookId: string) => {
    await moveNote(note.id, notebookId);
    notify('Note moved', 'success');
    setShowContextMenu(false);
  };

  const handleAddAttachment = async (file: File) => {
    const buffer = await file.arrayBuffer();
    const data = Array.from(new Uint8Array(buffer));
    await addAttachment(note.id, file.name, file.type, data);
    notify('Attachment added', 'success');
  };

  const handleExportHtml = async () => {
    const title = blocks.find((b) => b.block_type === 'h1' && b.content.trim())?.content
      ?? blocks.find((b) => b.content.trim())?.content
      ?? 'note';
    const safeName = title.replace(/<[^>]*>/g, '').slice(0, 60).replace(/[^\w\s-]/g, '').trim() || 'note';
    const path = await save({ defaultPath: `${safeName}.html`, filters: [{ name: 'HTML', extensions: ['html'] }] });
    if (!path) return;
    const bodyHtml = blocks.map(blockToHtml).join('\n');
    const fullHtml = `<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<title>${safeName}</title>
<style>
  body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; max-width: 800px; margin: 40px auto; padding: 0 24px; color: #1a1a1a; line-height: 1.6; }
  h1 { font-size: 2em; font-weight: 700; margin: 0.8em 0 0.4em; }
  h2 { font-size: 1.5em; font-weight: 600; margin: 0.8em 0 0.4em; }
  h3 { font-size: 1.2em; font-weight: 600; margin: 0.8em 0 0.4em; }
  h4 { font-size: 1em; font-weight: 600; margin: 0.8em 0 0.4em; }
  p { margin: 0.4em 0; }
  ul { margin: 0.2em 0; padding-left: 1.5em; }
  li { margin: 0.2em 0; }
  pre { background: #f5f5f5; border-radius: 6px; padding: 12px 16px; overflow-x: auto; font-size: 0.875em; }
  code { font-family: 'Fira Code', 'Cascadia Code', Consolas, monospace; }
  blockquote { border-left: 3px solid #888; margin: 0.5em 0; padding: 4px 12px; color: #555; font-style: italic; }
  hr { border: none; border-top: 1px solid #ddd; margin: 1.2em 0; }
  table { width: 100%; border-collapse: collapse; font-size: 0.9em; margin: 0.5em 0; }
  th { background: #f5f5f5; border-bottom: 2px solid #ddd; padding: 6px 10px; text-align: left; font-weight: 600; }
  td { border: 1px solid #e5e5e5; padding: 6px 10px; }
  tr:nth-child(even) td { background: #fafafa; }
  img { max-width: 100%; border-radius: 6px; }
  figcaption { text-align: center; font-size: 0.8em; color: #888; margin-top: 4px; }
  .callout { border-radius: 4px; padding: 8px 12px; margin: 0.4em 0; display: flex; gap: 8px; align-items: baseline; }
  .callout-label { font-size: 0.75em; font-weight: 700; text-transform: uppercase; letter-spacing: 0.05em; flex-shrink: 0; }
  .decision { background: #eff6ff; border-left: 3px solid #3b82f6; }
  .decision .callout-label { color: #3b82f6; }
  .warning { background: #fffbeb; border-left: 3px solid #f59e0b; }
  .warning .callout-label { color: #f59e0b; }
  .idea { background: #eff6ff; border-radius: 6px; }
  .idea-status { font-size: 0.7em; font-weight: 700; text-transform: uppercase; letter-spacing: 0.04em; padding: 2px 6px; border-radius: 3px; border: 1px solid currentColor; margin-left: auto; flex-shrink: 0; }
  .todo-item { display: flex; align-items: center; gap: 8px; margin: 0.25em 0; }
  .todo-item input[type=checkbox] { flex-shrink: 0; }
  .todo-item.checked span { text-decoration: line-through; opacity: 0.6; }
</style>
</head>
<body>
${bodyHtml}
</body>
</html>`;
    await writeTextFile(path, fullHtml);
    notify('Exported as HTML', 'success');
  };


  const createdDate = new Date(note.created_at).toLocaleString('en-GB', {
    day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit',
  });
  const modifiedDate = new Date(note.last_modified_at).toLocaleString('en-GB', {
    day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit',
  });
  const wasModified = note.last_modified_at !== note.created_at;

  return (
    <>
      <div
        style={{
          background: 'var(--bg-card)',
          borderRadius: 'var(--radius-lg)',
          boxShadow: isEditing ? 'var(--shadow-md)' : 'var(--shadow-card)',
          border: '1px solid var(--border-subtle)',
          transition: 'box-shadow var(--transition-base)',
          overflow: 'visible',
        }}
        onClick={() => { if (!isEditing) setEditing(note.id); }}
      >
        {/* Header */}
        <div style={{
          display: 'flex', alignItems: 'center', justifyContent: 'space-between',
          padding: '8px 12px 6px', borderBottom: isEditing ? '1px solid var(--border-subtle)' : 'none',
        }}>
          <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            <span style={{ fontSize: 11, color: 'var(--text-muted)' }}>{createdDate}</span>
            {wasModified && (
              <span style={{ fontSize: 11, color: 'var(--text-muted)' }}>· Modified {modifiedDate}</span>
            )}
            {savedPulse && (
              <span style={{ fontSize: 11, color: 'var(--color-success)' }}>Saved</span>
            )}
          </div>

          <div style={{ display: 'flex', gap: 4, alignItems: 'center' }}>
            {isEditing && (
              <ActionButton title="Save" onClick={handleSave}>
                <CheckIcon style={{ color: 'var(--color-success)' }} />
              </ActionButton>
            )}
            {attachments.length > 0 && (
              <AttachmentPanel
                attachments={attachments}
                onAdd={handleAddAttachment}
                onDelete={(id) => deleteAttachment(note.id, id)}
              />
            )}
            <div style={{ position: 'relative' }} ref={contextMenuRef}>
              <ActionButton title="More options" onClick={() => setShowContextMenu((s) => !s)}>
                <DotsIcon />
              </ActionButton>
              {showContextMenu && (
                <NoteContextMenu
                  notebooks={notebooks}
                  currentNotebookId={note.notebook_id}
                  onMoveToNotebook={handleMoveToNotebook}
                  onAddAttachment={() => {
                    const input = document.createElement('input');
                    input.type = 'file';
                    input.onchange = (e) => {
                      const file = (e.target as HTMLInputElement).files?.[0];
                      if (file) handleAddAttachment(file);
                    };
                    input.click();
                    setShowContextMenu(false);
                  }}
                  onExportHtml={handleExportHtml}
                  onDelete={() => { setShowContextMenu(false); setShowDeleteConfirm(true); }}
                />
              )}
            </div>
          </div>
        </div>

        {/* Blocks */}
        <div
          className="block-editor"
          style={{ padding: '8px 0 12px' }}
          onClick={(e) => {
            if (!isEditing) { setEditing(note.id); e.stopPropagation(); }
          }}
        >
          {blocks.map((block, idx) => (
            <BlockRenderer
              key={block.id}
              block={block}
              index={idx}
              isEditing={isEditing}
              onContentChange={handleBlockChange}
              onEnterPressed={(id, type, meta) => handleAddBlock(id, type, meta)}
              onDeletePressed={handleDeleteBlock}
              onTypeChange={handleTypeChange}
              onReorder={handleReorder}
            />
          ))}
        </div>
      </div>

      <ConfirmationModal
        open={showDeleteConfirm}
        message="Delete this note? This action cannot be undone."
        onConfirm={handleDeleteNote}
        onCancel={() => setShowDeleteConfirm(false)}
      />
    </>
  );
}

function NoteContextMenu({
  notebooks, currentNotebookId, onMoveToNotebook, onAddAttachment, onExportHtml, onDelete
}: {
  notebooks: Notebook[];
  currentNotebookId: string;
  onMoveToNotebook: (id: string) => void;
  onAddAttachment: () => void;
  onExportHtml: () => void;
  onDelete: () => void;
}) {
  const otherNotebooks = notebooks.filter((n) => n.id !== currentNotebookId);
  return (
    <div style={{
      position: 'absolute', top: '100%', right: 0, zIndex: 100,
      background: 'var(--bg-card)', border: '1px solid var(--border-color)',
      borderRadius: 'var(--radius-md)', boxShadow: 'var(--shadow-lg)',
      minWidth: 180, padding: '4px 0', marginTop: 4,
    }}>
      <MenuItem onClick={onAddAttachment}>Add attachment</MenuItem>
      <MenuItem onClick={onExportHtml}>Export as HTML</MenuItem>
      {otherNotebooks.length > 0 && (
        <>
          <div style={{ height: 1, background: 'var(--border-subtle)', margin: '4px 0' }} />
          <div style={{ padding: '4px 12px 2px', fontSize: 11, color: 'var(--text-muted)' }}>Move to</div>
          {otherNotebooks.map((nb) => (
            <MenuItem key={nb.id} onClick={() => onMoveToNotebook(nb.id)}>{nb.name}</MenuItem>
          ))}
        </>
      )}
      <div style={{ height: 1, background: 'var(--border-subtle)', margin: '4px 0' }} />
      <MenuItem onClick={onDelete} style={{ color: 'var(--color-error)' }}>Delete note</MenuItem>
    </div>
  );
}

function MenuItem({ children, onClick, style }: { children: React.ReactNode; onClick: () => void; style?: React.CSSProperties }) {
  return (
    <button
      onClick={onClick}
      style={{
        width: '100%', textAlign: 'left', background: 'none', border: 'none',
        padding: '6px 12px', fontSize: 13, color: 'var(--text-primary)',
        cursor: 'pointer', ...style,
      }}
      onMouseEnter={(e) => (e.currentTarget.style.background = 'var(--bg-hover)')}
      onMouseLeave={(e) => (e.currentTarget.style.background = 'none')}
    >
      {children}
    </button>
  );
}

function ActionButton({ children, title, onClick }: { children: React.ReactNode; title: string; onClick: () => void }) {
  return (
    <button
      title={title}
      onClick={(e) => { e.stopPropagation(); onClick(); }}
      style={{
        background: 'none', border: 'none', cursor: 'pointer', color: 'var(--text-muted)',
        padding: '3px 4px', borderRadius: 'var(--radius-sm)', display: 'flex', alignItems: 'center',
      }}
      onMouseEnter={(e) => (e.currentTarget.style.background = 'var(--bg-hover)')}
      onMouseLeave={(e) => (e.currentTarget.style.background = 'none')}
    >
      {children}
    </button>
  );
}

const CheckIcon = ({ style }: { style?: React.CSSProperties }) => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" style={style}>
    <polyline points="20 6 9 17 4 12"/>
  </svg>
);
const DotsIcon = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <circle cx="12" cy="5" r="1"/><circle cx="12" cy="12" r="1"/><circle cx="12" cy="19" r="1"/>
  </svg>
);
