import React, { useState } from 'react';

interface Props {
  rect: DOMRect;
  isBold: boolean;
  isItalic: boolean;
  onFormat: (type: 'bold' | 'italic') => void;
  onColorSelected: (color: string | null) => void;
  
}

const COLORS = ['#ef4444', '#f97316', '#eab308', '#22c55e', '#3b82f6', '#a855f7', '#ec4899', '#6b7280'];

export function FormattingToolbar({ rect, isBold, isItalic, onFormat, onColorSelected }: Props) {
  const [showColors, setShowColors] = useState(false);

  const top = rect.top - 44 + window.scrollY;
  const left = rect.left + rect.width / 2;

  return (
    <div
      style={{
        position: 'fixed', top, left,
        transform: 'translateX(-50%)',
        zIndex: 300,
        background: 'var(--bg-card)', border: '1px solid var(--border-color)',
        borderRadius: 'var(--radius-md)', boxShadow: 'var(--shadow-lg)',
        display: 'flex', alignItems: 'center', gap: 2, padding: '4px 6px',
      }}
      onMouseDown={(e) => e.preventDefault()}
    >
      <ToolBtn active={isBold} onClick={() => onFormat('bold')} title="Bold">
        <strong style={{ fontSize: 13 }}>B</strong>
      </ToolBtn>
      <ToolBtn active={isItalic} onClick={() => onFormat('italic')} title="Italic">
        <em style={{ fontSize: 13 }}>I</em>
      </ToolBtn>
      <div style={{ width: 1, height: 16, background: 'var(--border-color)', margin: '0 2px' }} />
      <div style={{ position: 'relative' }}>
        <ToolBtn active={showColors} onClick={() => setShowColors((s) => !s)} title="Text color">
          <span style={{ fontSize: 12, fontWeight: 600, color: 'var(--text-secondary)' }}>A</span>
        </ToolBtn>
        {showColors && (
          <div style={{
            position: 'absolute', top: '100%', left: '50%', transform: 'translateX(-50%)',
            marginTop: 4, background: 'var(--bg-card)', border: '1px solid var(--border-color)',
            borderRadius: 'var(--radius-md)', boxShadow: 'var(--shadow-md)',
            padding: '6px', display: 'flex', gap: 4, flexWrap: 'wrap', width: 112,
          }}>
            <button
              onClick={() => { onColorSelected(null); setShowColors(false); }}
              style={{ width: 20, height: 20, borderRadius: 4, border: '1px solid var(--border-color)', background: 'var(--bg-secondary)', cursor: 'pointer' }}
              title="Default"
            />
            {COLORS.map((c) => (
              <button
                key={c}
                onClick={() => { onColorSelected(c); setShowColors(false); }}
                style={{ width: 20, height: 20, borderRadius: 4, border: 'none', background: c, cursor: 'pointer' }}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

function ToolBtn({ children, active, onClick, title }: {
  children: React.ReactNode;
  active?: boolean;
  onClick: () => void;
  title: string;
}) {
  return (
    <button
      onClick={onClick}
      title={title}
      style={{
        background: active ? 'var(--bg-active)' : 'none',
        border: 'none', borderRadius: 'var(--radius-sm)',
        padding: '3px 6px', cursor: 'pointer', color: 'var(--text-primary)',
        display: 'flex', alignItems: 'center',
      }}
      onMouseEnter={(e) => { if (!active) e.currentTarget.style.background = 'var(--bg-hover)'; }}
      onMouseLeave={(e) => { if (!active) e.currentTarget.style.background = 'none'; }}
    >
      {children}
    </button>
  );
}
