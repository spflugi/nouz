import { invoke } from '@tauri-apps/api/core';
import type { Attachment, AttachmentMeta, BlockInput, Embedding, Note, Notebook } from '../types';

// Notebooks
export const getNotebooks = () => invoke<Notebook[]>('get_notebooks');
export const createNotebook = (name: string) => invoke<Notebook>('create_notebook', { name });
export const renameNotebook = (id: string, name: string) => invoke<void>('rename_notebook', { id, name });
export const deleteNotebook = (id: string) => invoke<void>('delete_notebook', { id });
export const reorderNotebooks = (ids: string[]) => invoke<void>('reorder_notebooks', { ids });

// Notes
export const getNotes = (notebookId: string) => invoke<Note[]>('get_notes', { notebookId });
export const getAllNotes = () => invoke<Note[]>('get_all_notes');
export const getNote = (noteId: string) => invoke<Note | null>('get_note', { noteId });
export const createNote = (notebookId: string, blocks: BlockInput[]) =>
  invoke<Note>('create_note', { notebookId, blocks });
export const saveNote = (noteId: string, blocks: BlockInput[]) =>
  invoke<Note>('save_note', { noteId, blocks });
export const deleteNote = (noteId: string) => invoke<void>('delete_note', { noteId });
export const moveNote = (noteId: string, notebookId: string) =>
  invoke<void>('move_note', { noteId, notebookId });
export const searchNotes = (query: string) => invoke<Note[]>('search_notes', { query });

// Attachments
export const getAttachments = (noteId: string) => invoke<AttachmentMeta[]>('get_attachments', { noteId });
export const getAllAttachmentsMeta = () => invoke<AttachmentMeta[]>('get_all_attachments_meta');
export const addAttachment = (noteId: string, fileName: string, mimeType: string, data: number[]) =>
  invoke<AttachmentMeta>('add_attachment', { noteId, fileName, mimeType, data });
export const getAttachmentData = (attachmentId: string) =>
  invoke<Attachment>('get_attachment_data', { attachmentId });
export const deleteAttachment = (attachmentId: string) =>
  invoke<void>('delete_attachment', { attachmentId });

// Embeddings
export const saveEmbedding = (noteId: string, embedding: string, model: string) =>
  invoke<void>('save_embedding', { noteId, embedding, model });
export const getAllEmbeddings = () => invoke<Embedding[]>('get_all_embeddings');
export const deleteEmbedding = (noteId: string) => invoke<void>('delete_embedding', { noteId });

// Preferences
export const getPreference = (key: string) => invoke<string | null>('get_preference', { key });
export const setPreference = (key: string, value: string) =>
  invoke<void>('set_preference', { key, value });
export const deletePreference = (key: string) => invoke<void>('delete_preference', { key });

// OpenAI Admin
export interface OpenAiUsage {
  cost: number;
  inputTokens: number;
  outputTokens: number;
  requests: number;
}
export const getOpenAiUsage = (adminKey: string, startTime: number, endTime: number) =>
  invoke<OpenAiUsage>('get_openai_usage', { adminKey, startTime, endTime });
