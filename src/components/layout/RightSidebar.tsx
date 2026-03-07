import { useState } from 'react';
import { useResizable } from '../../hooks/useResizable';
import { ChatBot } from '../chat/ChatBot';

export function RightSidebar() {
  const { width, onMouseDown } = useResizable(300, 240, 480, 'left');
  const [collapsed, setCollapsed] = useState(false);

  if (collapsed) {
    return (
      <div style={{
        width: 44, display: 'flex', flexDirection: 'column', alignItems: 'center',
        paddingTop: 10, borderLeft: '1px solid var(--border-color)', background: 'var(--bg-secondary)'
      }}>
        <button
          onClick={() => setCollapsed(false)}
          title="Open Assistant"
          style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--text-muted)', padding: 8 }}
        >
          <BotIcon />
        </button>
      </div>
    );
  }

  return (
    <div style={{
      width, minWidth: 240, maxWidth: 480, flexShrink: 0, position: 'relative',
      display: 'flex', flexDirection: 'column',
      borderLeft: '1px solid var(--border-color)', background: 'var(--bg-secondary)',
      overflow: 'hidden',
    }}>
      {/* Resize handle on left */}
      <div
        onMouseDown={onMouseDown}
        style={{ position: 'absolute', top: 0, left: 0, width: 4, height: '100%', cursor: 'col-resize', zIndex: 10 }}
      />

      {/* Header */}
      <div style={{
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        padding: '10px 12px', borderBottom: '1px solid var(--border-subtle)',
        height: 44, flexShrink: 0,
      }}>
        <button
          onClick={() => setCollapsed(true)}
          title="Collapse"
          style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--text-muted)', padding: '3px 4px' }}
        >
          <ChevronRightIcon />
        </button>
        <span style={{ fontWeight: 600, fontSize: 12, color: 'var(--text-secondary)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
          Assistant
        </span>
      </div>

      <div style={{ flex: 1, overflow: 'hidden', display: 'flex', flexDirection: 'column' }}>
        <ChatBot />
      </div>
    </div>
  );
}

const BotIcon = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <rect x="3" y="11" width="18" height="10" rx="2"/><circle cx="12" cy="5" r="2"/>
    <line x1="12" y1="7" x2="12" y2="11"/>
    <line x1="8" y1="15" x2="8" y2="17"/><line x1="16" y1="15" x2="16" y2="17"/>
  </svg>
);
const ChevronRightIcon = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <polyline points="9 18 15 12 9 6"/>
  </svg>
);
