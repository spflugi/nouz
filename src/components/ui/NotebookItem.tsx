import React, { useRef, useState } from 'react';
import type { Notebook } from '../../types';

interface Props {
  notebook: Notebook;
  isSelected: boolean;
  onSelect: (id: string) => void;
  onRename: (id: string) => void;
  onDelete: (id: string) => void;
}

export function NotebookItem({ notebook, isSelected, onSelect, onRename, onDelete }: Props) {
  const [showMenu, setShowMenu] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);

  return (
    <div
      style={{
        display: 'flex', alignItems: 'center',
        padding: '6px 12px', cursor: 'pointer',
        background: isSelected ? 'var(--bg-active)' : 'none',
        borderRadius: 'var(--radius-sm)', margin: '1px 4px',
        gap: 8,
      }}
      onClick={() => onSelect(notebook.id)}
      onMouseEnter={(e) => { if (!isSelected) e.currentTarget.style.background = 'var(--bg-hover)'; }}
      onMouseLeave={(e) => { if (!isSelected) e.currentTarget.style.background = 'none'; }}
    >
      <NotebookIcon style={{ color: 'var(--text-muted)', flexShrink: 0 }} />
      <span style={{
        flex: 1, fontSize: 13, color: isSelected ? 'var(--text-primary)' : 'var(--text-secondary)',
        overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap',
        fontWeight: isSelected ? 500 : 400,
      }}>
        {notebook.name}
      </span>
      <div style={{ position: 'relative' }} ref={menuRef}>
        <button
          onClick={(e) => { e.stopPropagation(); setShowMenu((s) => !s); }}
          style={{
            background: 'none', border: 'none', cursor: 'pointer',
            color: 'var(--text-muted)', padding: '2px 3px', borderRadius: 'var(--radius-sm)',
            display: 'flex', alignItems: 'center',
            opacity: isSelected || showMenu ? 1 : 0,
          }}
          onMouseEnter={(e) => (e.currentTarget.style.opacity = '1')}
        >
          <DotsIcon />
        </button>
        {showMenu && (
          <div
            style={{
              position: 'absolute', top: '100%', right: 0, zIndex: 100,
              background: 'var(--bg-card)', border: '1px solid var(--border-color)',
              borderRadius: 'var(--radius-md)', boxShadow: 'var(--shadow-lg)',
              minWidth: 140, padding: '4px 0', marginTop: 2,
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <MenuItem onClick={() => { onRename(notebook.id); setShowMenu(false); }}>Rename</MenuItem>
            <MenuItem onClick={() => { onDelete(notebook.id); setShowMenu(false); }} danger>Delete</MenuItem>
          </div>
        )}
      </div>
    </div>
  );
}

function MenuItem({ children, onClick, danger }: { children: React.ReactNode; onClick: () => void; danger?: boolean }) {
  return (
    <button
      onClick={onClick}
      style={{
        width: '100%', textAlign: 'left', background: 'none', border: 'none',
        padding: '6px 12px', fontSize: 13,
        color: danger ? 'var(--color-error)' : 'var(--text-primary)', cursor: 'pointer',
      }}
      onMouseEnter={(e) => (e.currentTarget.style.background = danger ? 'var(--color-error-bg)' : 'var(--bg-hover)')}
      onMouseLeave={(e) => (e.currentTarget.style.background = 'none')}
    >
      {children}
    </button>
  );
}

const NotebookIcon = ({ style }: { style?: React.CSSProperties }) => (
  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" style={style}>
    <path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z"/>
    <path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z"/>
  </svg>
);
const DotsIcon = () => (
  <svg width="13" height="13" viewBox="0 0 24 24" fill="currentColor">
    <circle cx="5" cy="12" r="1.5"/><circle cx="12" cy="12" r="1.5"/><circle cx="19" cy="12" r="1.5"/>
  </svg>
);
