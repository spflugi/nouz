import React, { useState } from 'react';
import { useNotebookStore } from '../../store/notebookStore';
import { useNoteStore } from '../../store/noteStore';
import { useDebounce } from '../../hooks/useDebounce';
import { SettingsPage } from '../settings/SettingsPage';

export function Topbar() {
  const { selectedNotebookId } = useNotebookStore();
  const { createNote, createMeetingNote, runSearch, clearSearch } = useNoteStore();
  const [query, setQuery] = useState('');
  const [showSettings, setShowSettings] = useState(false);

  const debouncedSearch = useDebounce(async (q: string) => {
    await runSearch(q);
  }, 300);

  const handleInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    const q = e.target.value;
    setQuery(q);
    debouncedSearch(q);
  };

  const handleClear = () => {
    setQuery('');
    debouncedSearch(''); // cancel any pending debounced search
    clearSearch();
  };

  const handleCreateNote = async () => {
    if (!selectedNotebookId) return;
    await createNote(selectedNotebookId);
  };

  const handleCreateMeeting = async () => {
    if (!selectedNotebookId) return;
    await createMeetingNote(selectedNotebookId);
  };

  return (
    <>
      <div style={{
        height: 44, display: 'flex', alignItems: 'center',
        padding: '0 12px', gap: 8,
        borderBottom: '1px solid var(--border-color)',
        background: 'var(--bg-secondary)', flexShrink: 0,
      }}>
        {selectedNotebookId && (
          <>
            <SearchIcon style={{ color: 'var(--text-muted)', flexShrink: 0 }} />
            <input
              type="text"
              placeholder="Search notes..."
              value={query}
              onChange={handleInput}
              style={{
                flex: 1, background: 'none', border: 'none', outline: 'none',
                fontSize: 13, color: 'var(--text-primary)',
                '::placeholder': { color: 'var(--text-placeholder)' },
              } as React.CSSProperties}
            />
            {query && (
              <TopbarButton title="Clear search" onClick={handleClear}><XIcon /></TopbarButton>
            )}
          </>
        )}

        <div style={{ flex: selectedNotebookId ? 'none' : 1 }} />

        {selectedNotebookId && (
          <>
            <TopbarButton title="New note" onClick={handleCreateNote}><PlusIcon /></TopbarButton>
            <TopbarButton title="New meeting note" onClick={handleCreateMeeting}><CalendarIcon /></TopbarButton>
            <div style={{ width: 1, height: 20, background: 'var(--border-color)', margin: '0 2px' }} />
          </>
        )}
        <TopbarButton title="Settings" onClick={() => setShowSettings(true)}><GearIcon /></TopbarButton>
      </div>

      {showSettings && <SettingsPage onClose={() => setShowSettings(false)} />}
    </>
  );
}

function TopbarButton({ children, title, onClick }: { children: React.ReactNode; title: string; onClick: () => void }) {
  return (
    <button
      title={title}
      onClick={onClick}
      style={{
        background: 'none', border: 'none', cursor: 'pointer',
        color: 'var(--text-muted)', padding: '4px 6px', borderRadius: 'var(--radius-sm)',
        display: 'flex', alignItems: 'center',
      }}
      onMouseEnter={(e) => (e.currentTarget.style.background = 'var(--bg-hover)')}
      onMouseLeave={(e) => (e.currentTarget.style.background = 'none')}
    >
      {children}
    </button>
  );
}

const SearchIcon = ({ style }: { style?: React.CSSProperties }) => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" style={style}>
    <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
  </svg>
);
const XIcon = () => (
  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
    <line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/>
  </svg>
);
const PlusIcon = () => (
  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
    <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
  </svg>
);
const CalendarIcon = () => (
  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <rect x="3" y="4" width="18" height="18" rx="2" ry="2"/>
    <line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/>
    <line x1="3" y1="10" x2="21" y2="10"/>
    <line x1="12" y1="14" x2="12" y2="18"/><line x1="10" y1="16" x2="14" y2="16"/>
  </svg>
);
const GearIcon = () => (
  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <circle cx="12" cy="12" r="3"/>
    <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/>
  </svg>
);
