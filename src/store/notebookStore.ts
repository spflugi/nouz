import { create } from 'zustand';
import type { Notebook } from '../types';
import * as db from '../services/db';

interface NotebookState {
  notebooks: Notebook[];
  selectedNotebookId: string | null;
  isLoading: boolean;
  load: () => Promise<void>;
  select: (id: string | null) => void;
  create: (name: string) => Promise<Notebook>;
  rename: (id: string, name: string) => Promise<void>;
  remove: (id: string) => Promise<void>;
}

export const useNotebookStore = create<NotebookState>((set) => ({
  notebooks: [],
  selectedNotebookId: null,
  isLoading: false,

  load: async () => {
    set({ isLoading: true });
    try {
      const notebooks = await db.getNotebooks();
      set({ notebooks });
    } finally {
      set({ isLoading: false });
    }
  },

  select: (id) => set({ selectedNotebookId: id }),

  create: async (name) => {
    const notebook = await db.createNotebook(name);
    set((s) => ({ notebooks: [...s.notebooks, notebook] }));
    return notebook;
  },

  rename: async (id, name) => {
    await db.renameNotebook(id, name);
    set((s) => ({
      notebooks: s.notebooks.map((n) =>
        n.id === id ? { ...n, name } : n
      ),
    }));
  },

  remove: async (id) => {
    await db.deleteNotebook(id);
    set((s) => ({
      notebooks: s.notebooks.filter((n) => n.id !== id),
      selectedNotebookId: s.selectedNotebookId === id ? null : s.selectedNotebookId,
    }));
  },
}));
