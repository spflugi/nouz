import { create } from 'zustand';
import type { ChatMessage, Note } from '../types';
import { v4 as uuidv4 } from '../utils/uuid';
import { streamChatResponse } from '../services/aiService';

interface ChatState {
  messages: ChatMessage[];
  isStreaming: boolean;
  streamingMessageId: string | null;
  expandedToolCallIds: Set<string>;
  expandedNoteContextIds: Set<string>;

  sendMessage: (
    content: string,
    apiKey: string,
    model: string,
    getRelevantNotes: () => Promise<Note[]>
  ) => Promise<void>;
  clearChat: () => void;
  toggleToolCalls: (messageId: string) => void;
  toggleNoteContext: (messageId: string) => void;
}

export const useChatStore = create<ChatState>((set, get) => ({
  messages: [],
  isStreaming: false,
  streamingMessageId: null,
  expandedToolCallIds: new Set(),
  expandedNoteContextIds: new Set(),

  sendMessage: async (content, apiKey, model, getRelevantNotes) => {
    // Capture history before adding new messages
    const history = get().messages.filter((m) => !m.is_streaming);

    const userMsg: ChatMessage = {
      id: uuidv4(),
      role: 'user',
      content,
      timestamp: new Date().toISOString(),
    };

    const assistantId = uuidv4();
    const assistantMsg: ChatMessage = {
      id: assistantId,
      role: 'assistant',
      content: '',
      timestamp: new Date().toISOString(),
      tool_calls: [],
      is_streaming: true,
    };

    // Add both messages immediately so UI updates right away
    set((s) => ({
      messages: [...s.messages, userMsg, assistantMsg],
      isStreaming: true,
      streamingMessageId: assistantId,
    }));

    try {
      // Embedding lookup runs after UI update (dots already showing)
      const relevantNotes = await getRelevantNotes();

      await streamChatResponse({
        apiKey,
        model,
        message: content,
        history,
        relevantNotes,
        onChunk: (text) => {
          set((s) => ({
            messages: s.messages.map((m) =>
              m.id === assistantId ? { ...m, content: m.content + text } : m
            ),
          }));
        },
        onToolCall: (label, isCompleted) => {
          set((s) => ({
            messages: s.messages.map((m) => {
              if (m.id !== assistantId) return m;
              const existing = m.tool_calls ?? [];
              const lastRunning = existing.findIndex((t) => !t.is_completed);
              if (lastRunning !== -1 && isCompleted) {
                const updated = existing.map((t, i) =>
                  i === lastRunning ? { ...t, is_completed: true } : t
                );
                return { ...m, tool_calls: updated };
              }
              if (!isCompleted) {
                return {
                  ...m,
                  tool_calls: [...existing, { id: uuidv4(), label, is_completed: false }],
                };
              }
              return m;
            }),
          }));
        },
        onContextNotes: (titles) => {
          set((s) => ({
            messages: s.messages.map((m) =>
              m.id === assistantId ? { ...m, context_note_titles: titles } : m
            ),
          }));
        },
      });
    } catch (err: any) {
      const errorText = err?.message ?? 'An error occurred';
      set((s) => ({
        messages: s.messages.map((m) =>
          m.id === assistantId ? { ...m, content: `Error: ${errorText}` } : m
        ),
      }));
    } finally {
      set((s) => ({
        isStreaming: false,
        streamingMessageId: null,
        messages: s.messages.map((m) =>
          m.id === assistantId ? { ...m, is_streaming: false } : m
        ),
      }));
    }
  },

  clearChat: () =>
    set({ messages: [], expandedToolCallIds: new Set(), expandedNoteContextIds: new Set() }),

  toggleToolCalls: (id) =>
    set((s) => {
      const next = new Set(s.expandedToolCallIds);
      next.has(id) ? next.delete(id) : next.add(id);
      return { expandedToolCallIds: next };
    }),

  toggleNoteContext: (id) =>
    set((s) => {
      const next = new Set(s.expandedNoteContextIds);
      next.has(id) ? next.delete(id) : next.add(id);
      return { expandedNoteContextIds: next };
    }),
}));
