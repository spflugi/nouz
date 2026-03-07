import { useEffect, useRef } from 'react';
import type { BlockType } from '../../types';

interface Props {
  currentType: BlockType;
  onTypeSelected: (type: BlockType) => void;
  onAddBelow: () => void;
  onDelete: () => void;
  onClose: () => void;
}

const BLOCK_OPTIONS: Array<{ type: BlockType; label: string; description: string; icon: string }> = [
  { type: 'paragraph', label: 'Text', description: 'Plain paragraph', icon: '¶' },
  { type: 'h1', label: 'Heading 1', description: 'Large heading', icon: 'H1' },
  { type: 'h2', label: 'Heading 2', description: 'Medium heading', icon: 'H2' },
  { type: 'h3', label: 'Heading 3', description: 'Small heading', icon: 'H3' },
  { type: 'h4', label: 'Heading 4', description: 'Tiny heading', icon: 'H4' },
  { type: 'listitem', label: 'Bullet list', description: 'Unordered list item', icon: '•' },
  { type: 'todoitem', label: 'To-do', description: 'Checkbox item', icon: '☐' },
  { type: 'agendaitem', label: 'Agenda item', description: 'Meeting agenda', icon: '›' },
  { type: 'code', label: 'Code', description: 'Monospace code block', icon: '<>' },
  { type: 'quote', label: 'Quote', description: 'Block quotation', icon: '"' },
  { type: 'decision', label: 'Decision', description: 'Record a decision', icon: '✓' },
  { type: 'warning', label: 'Warning', description: 'Alert or caution', icon: '⚠' },
  { type: 'idea', label: 'Idea', description: 'Idea or insight', icon: '💡' },
  { type: 'divider', label: 'Divider', description: 'Horizontal rule', icon: '─' },
  { type: 'image', label: 'Image', description: 'Upload an image', icon: '🖼' },
  { type: 'mermaid', label: 'Diagram', description: 'Mermaid diagram', icon: '◈' },
  { type: 'table', label: 'Table', description: 'Data table', icon: '▦' },
];

export function BlockContextMenu({ currentType, onTypeSelected, onAddBelow, onDelete, onClose }: Props) {
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) onClose();
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [onClose]);

  return (
    <div
      ref={ref}
      style={{
        position: 'absolute', left: 40, top: 0, zIndex: 200,
        background: 'var(--bg-card)', border: '1px solid var(--border-color)',
        borderRadius: 'var(--radius-md)', boxShadow: 'var(--shadow-lg)',
        width: 240, maxHeight: 360, overflowY: 'auto', padding: '4px 0',
      }}
    >
      <button
        onClick={onAddBelow}
        style={{
          width: '100%', textAlign: 'left', background: 'none', border: 'none',
          padding: '6px 10px', cursor: 'pointer',
          display: 'flex', alignItems: 'center', gap: 10,
        }}
        onMouseEnter={(e) => (e.currentTarget.style.background = 'var(--bg-hover)')}
        onMouseLeave={(e) => (e.currentTarget.style.background = 'none')}
      >
        <span style={{
          width: 28, height: 28, display: 'flex', alignItems: 'center', justifyContent: 'center',
          background: 'var(--bg-secondary)', borderRadius: 'var(--radius-sm)',
          fontSize: 16, color: 'var(--text-secondary)', flexShrink: 0,
        }}>+</span>
        <div>
          <div style={{ fontSize: 13, color: 'var(--text-primary)', fontWeight: 500 }}>Add block below</div>
          <div style={{ fontSize: 11, color: 'var(--text-muted)' }}>Insert a new paragraph</div>
        </div>
      </button>
      <div style={{ height: 1, background: 'var(--border-subtle)', margin: '4px 0' }} />
      <div style={{ padding: '4px 10px 6px', fontSize: 11, color: 'var(--text-muted)', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.05em' }}>
        Block type
      </div>
      {BLOCK_OPTIONS.map((opt) => (
        <button
          key={opt.type}
          onClick={() => onTypeSelected(opt.type)}
          style={{
            width: '100%', textAlign: 'left', background: opt.type === currentType ? 'var(--bg-hover)' : 'none',
            border: 'none', padding: '6px 10px', cursor: 'pointer',
            display: 'flex', alignItems: 'center', gap: 10,
          }}
          onMouseEnter={(e) => (e.currentTarget.style.background = 'var(--bg-hover)')}
          onMouseLeave={(e) => (e.currentTarget.style.background = opt.type === currentType ? 'var(--bg-hover)' : 'none')}
        >
          <span style={{
            width: 28, height: 28, display: 'flex', alignItems: 'center', justifyContent: 'center',
            background: 'var(--bg-secondary)', borderRadius: 'var(--radius-sm)',
            fontSize: 12, color: 'var(--text-secondary)', flexShrink: 0, fontWeight: 600,
          }}>{opt.icon}</span>
          <div>
            <div style={{ fontSize: 13, color: 'var(--text-primary)', fontWeight: 500 }}>{opt.label}</div>
            <div style={{ fontSize: 11, color: 'var(--text-muted)' }}>{opt.description}</div>
          </div>
        </button>
      ))}
      <div style={{ height: 1, background: 'var(--border-subtle)', margin: '4px 0' }} />
      <button
        onClick={onDelete}
        style={{
          width: '100%', textAlign: 'left', background: 'none', border: 'none',
          padding: '6px 10px', cursor: 'pointer', fontSize: 13, color: 'var(--color-error)',
        }}
        onMouseEnter={(e) => (e.currentTarget.style.background = 'var(--color-error-bg)')}
        onMouseLeave={(e) => (e.currentTarget.style.background = 'none')}
      >
        Delete block
      </button>
    </div>
  );
}
