import { create } from 'zustand';
import type { ThemeMode } from '../types';
import * as db from '../services/db';

const KEYS = {
  theme: 'theme',
  openAiApiKey: 'openai_api_key',
  openAiAdminKey: 'openai_admin_key',
  chatModel: 'chat_model',
  embeddingModel: 'embedding_model',
  topNRelevantNotes: 'top_n_relevant_notes',
  minSimilarityThreshold: 'min_similarity_threshold',
};

interface SettingsState {
  theme: ThemeMode;
  openAiApiKey: string;
  openAiAdminKey: string;
  chatModel: string;
  embeddingModel: string;
  topNRelevantNotes: number;
  minSimilarityThreshold: number;

  load: () => Promise<void>;
  setTheme: (theme: ThemeMode) => Promise<void>;
  setOpenAiApiKey: (key: string) => Promise<void>;
  setOpenAiAdminKey: (key: string) => Promise<void>;
  setChatModel: (model: string) => Promise<void>;
  setEmbeddingModel: (model: string) => Promise<void>;
  setTopNRelevantNotes: (n: number) => Promise<void>;
  setMinSimilarityThreshold: (v: number) => Promise<void>;
}

export const useSettingsStore = create<SettingsState>((set) => ({
  theme: 'light',
  openAiApiKey: '',
  openAiAdminKey: '',
  chatModel: 'gpt-4o-mini',
  embeddingModel: 'text-embedding-3-small',
  topNRelevantNotes: 3,
  minSimilarityThreshold: 0.5,

  load: async () => {
    const [theme, apiKey, adminKey, chatModel, embeddingModel, topN, threshold] = await Promise.all([
      db.getPreference(KEYS.theme),
      db.getPreference(KEYS.openAiApiKey),
      db.getPreference(KEYS.openAiAdminKey),
      db.getPreference(KEYS.chatModel),
      db.getPreference(KEYS.embeddingModel),
      db.getPreference(KEYS.topNRelevantNotes),
      db.getPreference(KEYS.minSimilarityThreshold),
    ]);

    const resolvedTheme = (theme as ThemeMode) ?? 'light';
    applyTheme(resolvedTheme);

    set({
      theme: resolvedTheme,
      openAiApiKey: apiKey ?? '',
      openAiAdminKey: adminKey ?? '',
      chatModel: chatModel ?? 'gpt-4o-mini',
      embeddingModel: embeddingModel ?? 'text-embedding-3-small',
      topNRelevantNotes: topN ? parseInt(topN, 10) : 3,
      minSimilarityThreshold: threshold ? parseFloat(threshold) : 0.5,
    });
  },

  setTheme: async (theme) => {
    await db.setPreference(KEYS.theme, theme);
    applyTheme(theme);
    set({ theme });
  },

  setOpenAiApiKey: async (key) => {
    await db.setPreference(KEYS.openAiApiKey, key);
    set({ openAiApiKey: key });
  },

  setOpenAiAdminKey: async (key) => {
    await db.setPreference(KEYS.openAiAdminKey, key);
    set({ openAiAdminKey: key });
  },

  setChatModel: async (model) => {
    await db.setPreference(KEYS.chatModel, model);
    set({ chatModel: model });
  },

  setEmbeddingModel: async (model) => {
    await db.setPreference(KEYS.embeddingModel, model);
    set({ embeddingModel: model });
  },

  setTopNRelevantNotes: async (n) => {
    await db.setPreference(KEYS.topNRelevantNotes, String(n));
    set({ topNRelevantNotes: n });
  },

  setMinSimilarityThreshold: async (v) => {
    await db.setPreference(KEYS.minSimilarityThreshold, String(v));
    set({ minSimilarityThreshold: v });
  },
}));

function applyTheme(theme: ThemeMode) {
  document.documentElement.setAttribute('data-theme', theme);
}
