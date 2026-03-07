import React, { useEffect, useState } from 'react';
import type { Block } from '../../../types';

interface Props {
  block: Block;
  isEditing: boolean;
  onContentChange: (updated: Block) => void;
}

let _mermaidInitialized = false;
let _renderQueue: Promise<void> = Promise.resolve();

async function getMermaid() {
  const mermaid = (await import('mermaid')).default;
  if (!_mermaidInitialized) {
    mermaid.initialize({ startOnLoad: false, theme: 'neutral' });
    _mermaidInitialized = true;
  }
  return mermaid;
}

export function MermaidBlock({ block, isEditing, onContentChange }: Props) {
  const [editMode, setEditMode] = useState(false);
  const [draft, setDraft] = useState(block.content || 'graph TD\n  A --> B');
  const [svgHtml, setSvgHtml] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!editMode) renderDiagram();
  }, [editMode, block.content]);

  const renderDiagram = () => {
    // Capture content immediately so the queued task uses the right value
    const content = block.content || 'graph TD\n  A --> B';
    _renderQueue = _renderQueue.then(async () => {
      try {
        const mermaid = await getMermaid();
        const id = `mr${Date.now()}`;
        const { svg } = await mermaid.render(id, content);
        setSvgHtml(svg);
        setError(null);
      } catch (err: any) {
        setError(err?.message ?? 'Diagram error');
        setSvgHtml(null);
      }
    });
  };

  const handleSave = () => {
    onContentChange({ ...block, content: draft });
    setEditMode(false);
  };

  if (editMode) {
    return (
      <div style={{
        border: '1px solid var(--border-color)', borderRadius: 'var(--radius-md)',
        overflow: 'hidden', background: 'var(--bg-secondary)',
      }}>
        <div style={{ padding: '4px 8px', display: 'flex', justifyContent: 'flex-end', gap: 6, borderBottom: '1px solid var(--border-subtle)' }}>
          <MiniButton onClick={() => setEditMode(false)}>Cancel</MiniButton>
          <MiniButton primary onClick={handleSave}>Save</MiniButton>
        </div>
        <textarea
          value={draft}
          onChange={(e) => setDraft(e.target.value)}
          spellCheck={false}
          style={{
            width: '100%', minHeight: 120, padding: '8px 12px', background: 'none',
            border: 'none', outline: 'none', fontFamily: 'monospace', fontSize: 12,
            color: 'var(--text-primary)', resize: 'vertical',
          }}
          className="selectable"
        />
      </div>
    );
  }

  return (
    <div
      style={{ position: 'relative', cursor: isEditing ? 'pointer' : 'default' }}
      onClick={() => { if (isEditing) setEditMode(true); }}
    >
      {error ? (
        <div style={{ padding: '8px 12px', color: 'var(--color-error)', fontSize: 12, background: 'var(--color-error-bg)', borderRadius: 'var(--radius-md)' }}>
          Diagram error: {error}
        </div>
      ) : (
        <div
          dangerouslySetInnerHTML={svgHtml ? { __html: svgHtml } : undefined}
          style={{ display: 'flex', justifyContent: 'center', minHeight: svgHtml ? undefined : 40 }}
          className="mermaid"
        />
      )}
      {isEditing && (
        <div style={{
          position: 'absolute', top: 6, right: 6,
          background: 'var(--bg-card)', border: '1px solid var(--border-color)',
          borderRadius: 'var(--radius-sm)', padding: '2px 8px', fontSize: 11,
          color: 'var(--text-muted)', cursor: 'pointer',
        }}>Edit</div>
      )}
    </div>
  );
}

function MiniButton({ children, onClick, primary }: { children: React.ReactNode; onClick: () => void; primary?: boolean }) {
  return (
    <button
      onClick={onClick}
      style={{
        padding: '3px 10px', fontSize: 12, border: '1px solid var(--border-color)',
        borderRadius: 'var(--radius-sm)', cursor: 'pointer',
        background: primary ? 'var(--accent)' : 'none',
        color: primary ? 'var(--bg-primary)' : 'var(--text-primary)',
      }}
    >
      {children}
    </button>
  );
}
