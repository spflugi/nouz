import React, { useEffect, useState } from 'react';
import { useNotebookStore } from '../../store/notebookStore';
import { useNoteStore } from '../../store/noteStore';
import { useResizable } from '../../hooks/useResizable';
import { NotebookItem } from '../ui/NotebookItem';
import { TextInputModal } from '../modals/TextInputModal';
import { ConfirmationModal } from '../modals/ConfirmationModal';

export function LeftSidebar() {
  const { notebooks, selectedNotebookId, load, create, rename, remove, select } = useNotebookStore();
  const { loadNotes } = useNoteStore();
  const { width, onMouseDown } = useResizable(240, 180, 360, 'right');
  const [collapsed, setCollapsed] = useState(false);
  const [textModal, setTextModal] = useState<{ open: boolean; value?: string; onConfirm: (v: string) => void }>({
    open: false, onConfirm: () => {}
  });
  const [confirmModal, setConfirmModal] = useState<{ open: boolean; message: string; onConfirm: () => void }>({
    open: false, message: '', onConfirm: () => {}
  });

  useEffect(() => { load(); }, []);

  const handleSelectNotebook = (id: string) => {
    select(id);
    loadNotes(id);
  };

  const handleCreateNotebook = () => {
    setTextModal({
      open: true,
      value: '',
      onConfirm: async (name) => {
        if (name.trim()) {
          const nb = await create(name.trim());
          handleSelectNotebook(nb.id);
        }
        setTextModal((m) => ({ ...m, open: false }));
      },
    });
  };

  const handleRenameNotebook = (id: string, currentName: string) => {
    setTextModal({
      open: true,
      value: currentName,
      onConfirm: async (name) => {
        if (name.trim()) await rename(id, name.trim());
        setTextModal((m) => ({ ...m, open: false }));
      },
    });
  };

  const handleDeleteNotebook = (id: string, name: string) => {
    setConfirmModal({
      open: true,
      message: `Delete "${name}"? All notes inside will be permanently deleted.`,
      onConfirm: async () => {
        await remove(id);
        setConfirmModal((m) => ({ ...m, open: false }));
      },
    });
  };

  if (collapsed) {
    return (
      <div style={{
        width: 44, display: 'flex', flexDirection: 'column', alignItems: 'center',
        paddingTop: 10, borderRight: '1px solid var(--border-color)', background: 'var(--bg-secondary)'
      }}>
        <button
          onClick={() => setCollapsed(false)}
          title="Open sidebar"
          style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--text-muted)', padding: 8 }}
        >
          <MenuIcon />
        </button>
      </div>
    );
  }

  return (
    <>
      <div style={{
        width, minWidth: 180, maxWidth: 360, flexShrink: 0, position: 'relative',
        display: 'flex', flexDirection: 'column',
        borderRight: '1px solid var(--border-color)', background: 'var(--bg-secondary)',
        overflow: 'hidden',
      }}>
        {/* Header */}
        <div style={{
          display: 'flex', alignItems: 'center', justifyContent: 'space-between',
          padding: '10px 12px', borderBottom: '1px solid var(--border-subtle)',
          height: 44, flexShrink: 0,
        }}>
          <span style={{ fontWeight: 600, fontSize: 12, color: 'var(--text-secondary)', textTransform: 'uppercase', letterSpacing: '0.05em' }}>
            Notebooks
          </span>
          <div style={{ display: 'flex', gap: 2 }}>
            <IconButton title="New notebook" onClick={handleCreateNotebook}><PlusIcon /></IconButton>
            <IconButton title="Collapse sidebar" onClick={() => setCollapsed(true)}><ChevronLeftIcon /></IconButton>
          </div>
        </div>

        {/* Notebook list */}
        <div style={{ flex: 1, overflowY: 'auto', padding: '4px 0' }}>
          {notebooks.map((nb) => (
            <NotebookItem
              key={nb.id}
              notebook={nb}
              isSelected={nb.id === selectedNotebookId}
              onSelect={handleSelectNotebook}
              onRename={(id) => handleRenameNotebook(id, nb.name)}
              onDelete={(id) => handleDeleteNotebook(id, nb.name)}
            />
          ))}
          {notebooks.length === 0 && (
            <p style={{ padding: '16px 12px', color: 'var(--text-muted)', fontSize: 12 }}>
              No notebooks yet
            </p>
          )}
        </div>

        {/* Resize handle */}
        <div
          onMouseDown={onMouseDown}
          style={{
            position: 'absolute', top: 0, right: 0, width: 4, height: '100%',
            cursor: 'col-resize', zIndex: 10,
          }}
        />
      </div>

      <TextInputModal
        open={textModal.open}
        initialValue={textModal.value}
        placeholder="Notebook name"
        title="Notebook"
        onConfirm={textModal.onConfirm}
        onCancel={() => setTextModal((m) => ({ ...m, open: false }))}
      />
      <ConfirmationModal
        open={confirmModal.open}
        message={confirmModal.message}
        onConfirm={confirmModal.onConfirm}
        onCancel={() => setConfirmModal((m) => ({ ...m, open: false }))}
      />
    </>
  );
}

function IconButton({ children, title, onClick }: { children: React.ReactNode; title: string; onClick: () => void }) {
  return (
    <button
      title={title}
      onClick={onClick}
      style={{
        background: 'none', border: 'none', cursor: 'pointer',
        color: 'var(--text-muted)', padding: '3px 4px', borderRadius: 'var(--radius-sm)',
        display: 'flex', alignItems: 'center', justifyContent: 'center',
        transition: 'background var(--transition-fast)',
      }}
      onMouseEnter={(e) => (e.currentTarget.style.background = 'var(--bg-hover)')}
      onMouseLeave={(e) => (e.currentTarget.style.background = 'none')}
    >
      {children}
    </button>
  );
}

const MenuIcon = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <line x1="3" y1="12" x2="21" y2="12"/><line x1="3" y1="6" x2="21" y2="6"/><line x1="3" y1="18" x2="21" y2="18"/>
  </svg>
);
const PlusIcon = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5">
    <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
  </svg>
);
const ChevronLeftIcon = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <polyline points="15 18 9 12 15 6"/>
  </svg>
);
