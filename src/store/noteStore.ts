import { create } from 'zustand';
import type { AttachmentMeta, Block, BlockInput, Note } from '../types';
import * as db from '../services/db';
import { v4 as uuidv4 } from '../utils/uuid';

interface NoteState {
  notes: Note[];
  attachments: Record<string, AttachmentMeta[]>; // note_id -> attachments
  editingNoteId: string | null;
  editingBlockId: string | null;
  searchQuery: string;
  isLoading: boolean;

  loadNotes: (notebookId: string) => Promise<void>;
  setSearchQuery: (q: string) => void;
  searchResults: Note[] | null;
  runSearch: (q: string) => Promise<void>;
  clearSearch: () => void;

  createNote: (notebookId: string) => Promise<Note>;
  createMeetingNote: (notebookId: string) => Promise<Note>;
  saveNote: (noteId: string, blocks: Block[]) => Promise<void>;
  deleteNote: (noteId: string) => Promise<void>;
  moveNote: (noteId: string, notebookId: string) => Promise<void>;

  setEditing: (noteId: string | null, blockId?: string | null) => void;

  loadAttachments: (noteId: string) => Promise<void>;
  addAttachment: (noteId: string, fileName: string, mimeType: string, data: number[]) => Promise<AttachmentMeta>;
  deleteAttachment: (noteId: string, attachmentId: string) => Promise<void>;
}

export const useNoteStore = create<NoteState>((set, get) => ({
  notes: [],
  attachments: {},
  editingNoteId: null,
  editingBlockId: null,
  searchQuery: '',
  searchResults: null,
  isLoading: false,

  loadNotes: async (notebookId) => {
    set({ isLoading: true });
    try {
      const notes = await db.getNotes(notebookId);
      set({ notes });
    } finally {
      set({ isLoading: false });
    }
  },

  setSearchQuery: (q) => set({ searchQuery: q }),

  runSearch: async (q) => {
    if (!q.trim()) {
      set({ searchResults: null, searchQuery: '' });
      return;
    }
    set({ searchQuery: q });
    const results = await db.searchNotes(q);
    if (get().searchQuery === q) {
      set({ searchResults: results });
    }
  },

  clearSearch: () => set({ searchResults: null, searchQuery: '' }),

  createNote: async (notebookId) => {
    const blocks: BlockInput[] = [{
      id: uuidv4(),
      block_type: 'paragraph',
      content: '',
      metadata: '{}',
      order: 0,
    }];
    const note = await db.createNote(notebookId, blocks);
    set((s) => ({ notes: [note, ...s.notes] }));
    return note;
  },

  createMeetingNote: async (notebookId) => {
    const now = new Date();
    const dateStr = now.toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' });
    const timeStr = now.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' });

    const blocks: BlockInput[] = [
      { id: uuidv4(), block_type: 'h1', content: `Meeting — ${dateStr}`, metadata: '{}', order: 0 },
      { id: uuidv4(), block_type: 'paragraph', content: timeStr, metadata: '{}', order: 1 },
      { id: uuidv4(), block_type: 'h2', content: 'Agenda', metadata: '{}', order: 2 },
      { id: uuidv4(), block_type: 'agendaitem', content: '', metadata: '{}', order: 3 },
      { id: uuidv4(), block_type: 'h2', content: 'Notes', metadata: '{}', order: 4 },
      { id: uuidv4(), block_type: 'paragraph', content: '', metadata: '{}', order: 5 },
      { id: uuidv4(), block_type: 'h2', content: 'Decisions', metadata: '{}', order: 6 },
      { id: uuidv4(), block_type: 'decision', content: '', metadata: '{}', order: 7 },
      { id: uuidv4(), block_type: 'h2', content: 'Action Items', metadata: '{}', order: 8 },
      { id: uuidv4(), block_type: 'todoitem', content: '', metadata: '{}', order: 9 },
    ];
    const note = await db.createNote(notebookId, blocks);
    set((s) => ({ notes: [note, ...s.notes] }));
    return note;
  },

  saveNote: async (noteId, blocks) => {
    const inputs: BlockInput[] = blocks.map((b) => ({
      id: b.id,
      block_type: b.block_type,
      content: b.content,
      metadata: b.metadata,
      order: b.order,
    }));
    const updated = await db.saveNote(noteId, inputs);
    set((s) => ({
      notes: s.notes.map((n) => (n.id === noteId ? updated : n)),
    }));
  },

  deleteNote: async (noteId) => {
    await db.deleteNote(noteId);
    set((s) => ({
      notes: s.notes.filter((n) => n.id !== noteId),
      editingNoteId: s.editingNoteId === noteId ? null : s.editingNoteId,
    }));
  },

  moveNote: async (noteId, notebookId) => {
    await db.moveNote(noteId, notebookId);
    set((s) => ({
      notes: s.notes.filter((n) => n.id !== noteId),
    }));
  },

  setEditing: (noteId, blockId = null) =>
    set({ editingNoteId: noteId, editingBlockId: blockId }),

  loadAttachments: async (noteId) => {
    const list = await db.getAttachments(noteId);
    set((s) => ({ attachments: { ...s.attachments, [noteId]: list } }));
  },

  addAttachment: async (noteId, fileName, mimeType, data) => {
    const meta = await db.addAttachment(noteId, fileName, mimeType, data);
    set((s) => ({
      attachments: {
        ...s.attachments,
        [noteId]: [...(s.attachments[noteId] ?? []), meta],
      },
    }));
    return meta;
  },

  deleteAttachment: async (noteId, attachmentId) => {
    await db.deleteAttachment(attachmentId);
    set((s) => ({
      attachments: {
        ...s.attachments,
        [noteId]: (s.attachments[noteId] ?? []).filter((a) => a.id !== attachmentId),
      },
    }));
  },
}));
