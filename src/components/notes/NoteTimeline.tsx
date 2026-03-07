
import { useNoteStore } from '../../store/noteStore';
import { useNotebookStore } from '../../store/notebookStore';
import { NoteCard } from './NoteCard';

export function NoteTimeline() {
  const { selectedNotebookId, notebooks } = useNotebookStore();
  const { notes, searchResults, searchQuery, attachments, editingNoteId } = useNoteStore();

  const displayNotes = searchResults ?? notes;

  if (!selectedNotebookId) {
    return (
      <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100%', gap: 8 }}>
        <p style={{ color: 'var(--text-secondary)', fontSize: 14 }}>Select a notebook</p>
        <p style={{ color: 'var(--text-muted)', fontSize: 12 }}>Choose a notebook from the sidebar</p>
      </div>
    );
  }

  return (
    <div style={{ height: '100%', overflowY: 'auto', padding: '16px 0' }}>
      <div style={{ maxWidth: 1100, margin: '0 auto', padding: '0 16px', display: 'flex', flexDirection: 'column', gap: 12 }}>
        {searchQuery && searchResults && (
          <p style={{ color: 'var(--text-muted)', fontSize: 12 }}>
            {searchResults.length} result{searchResults.length !== 1 ? 's' : ''} for "{searchQuery}"
          </p>
        )}
        {displayNotes.length === 0 ? (
          <div style={{ textAlign: 'center', paddingTop: 48 }}>
            <p style={{ color: 'var(--text-secondary)', fontSize: 14 }}>No notes yet</p>
            <p style={{ color: 'var(--text-muted)', fontSize: 12, marginTop: 4 }}>
              Click the + button above to create your first note
            </p>
          </div>
        ) : (
          displayNotes.map((note) => (
            <NoteCard
              key={note.id}
              note={note}
              notebooks={notebooks}
              attachments={attachments[note.id] ?? []}
              isEditing={editingNoteId === note.id}
            />
          ))
        )}
      </div>
    </div>
  );
}
