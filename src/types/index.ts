export type BlockType =
  | 'paragraph'
  | 'h1'
  | 'h2'
  | 'h3'
  | 'h4'
  | 'listitem'
  | 'todoitem'
  | 'agendaitem'
  | 'code'
  | 'quote'
  | 'decision'
  | 'warning'
  | 'idea'
  | 'divider'
  | 'image'
  | 'mermaid'
  | 'table';

export interface Block {
  id: string;
  note_id: string;
  block_type: BlockType;
  content: string;
  metadata: string; // JSON string
  order: number;
}

export interface BlockInput {
  id?: string;
  block_type: BlockType;
  content: string;
  metadata?: string;
  order: number;
}

export interface Note {
  id: string;
  notebook_id: string;
  created_at: string;
  last_modified_at: string;
  blocks: Block[];
}

export interface Notebook {
  id: string;
  name: string;
  sort_order: number;
  created_at: string;
  last_modified_at: string;
}

export interface AttachmentMeta {
  id: string;
  note_id: string;
  file_name: string;
  mime_type: string;
  created_at: string;
}

export interface Attachment extends AttachmentMeta {
  data: number[];
}

export interface Embedding {
  note_id: string;
  embedding: string;
  model: string;
  updated_at: string;
}

export type ThemeMode = 'light' | 'dark';

export type NotificationSeverity = 'info' | 'success' | 'warning' | 'error';

export interface Notification {
  id: string;
  message: string;
  severity: NotificationSeverity;
}

// Block metadata shapes (parsed from JSON string)
export interface TodoMetadata {
  checked?: boolean;
}

export interface IdeaMetadata {
  status?: 'new' | 'exploring' | 'validated' | 'discarded';
}

export interface ImageMetadata {
  attachment_id?: string;
  caption?: string;
  width_percent?: number;
  data_url?: string; // base64 for inline images
}

export interface ListMetadata {
  indent?: number;
}

export interface TableMetadata {
  rows: string[][];
  headers?: string[];
}

export interface CodeMetadata {
  language?: string;
}

export type ChatRole = 'user' | 'assistant';

export interface ToolCallActivity {
  id: string;
  label: string;
  is_completed: boolean;
}

export interface ChatMessage {
  id: string;
  role: ChatRole;
  content: string;
  timestamp: string;
  tool_calls?: ToolCallActivity[];
  context_note_titles?: string[];
  is_streaming?: boolean;
}
