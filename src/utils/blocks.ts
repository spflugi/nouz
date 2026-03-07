import type { Block, BlockType, TableMetadata } from '../types';
import { v4 as uuidv4 } from './uuid';

export function parseMetadata<T = Record<string, unknown>>(metadata: string): T {
  try {
    return JSON.parse(metadata) as T;
  } catch {
    return {} as T;
  }
}

export function stringifyMetadata(meta: Record<string, unknown>): string {
  return JSON.stringify(meta);
}

export function newBlock(type: BlockType, noteId: string, order: number, content = ''): Block {
  return {
    id: uuidv4(),
    note_id: noteId,
    block_type: type,
    content,
    metadata: defaultMetadata(type),
    order,
  };
}

export function defaultMetadata(type: BlockType): string {
  switch (type) {
    case 'todoitem':
      return JSON.stringify({ checked: false });
    case 'idea':
      return JSON.stringify({ status: 'new' });
    case 'image':
      return JSON.stringify({ width_percent: 100, caption: '' });
    case 'table': {
      const meta: TableMetadata = { rows: [['', ''], ['', '']], headers: ['Column 1', 'Column 2'] };
      return JSON.stringify(meta);
    }
    default:
      return '{}';
  }
}

export function getBlockTitle(block: Block): string {
  const titles: Record<BlockType, string> = {
    paragraph: 'Text',
    h1: 'Heading 1',
    h2: 'Heading 2',
    h3: 'Heading 3',
    h4: 'Heading 4',
    listitem: 'List Item',
    todoitem: 'To-do',
    agendaitem: 'Agenda Item',
    code: 'Code',
    quote: 'Quote',
    decision: 'Decision',
    warning: 'Warning',
    idea: 'Idea',
    divider: 'Divider',
    image: 'Image',
    mermaid: 'Diagram',
    table: 'Table',
  };
  return titles[block.block_type] ?? 'Block';
}

export function getNotePreview(blocks: Block[]): string {
  return blocks
    .filter((b) => b.content && b.block_type !== 'divider')
    .map((b) => b.content)
    .join(' ')
    .slice(0, 120);
}

export function getNoteTitle(blocks: Block[]): string | null {
  const h1 = blocks.find((b) => b.block_type === 'h1' && b.content.trim());
  if (h1) return h1.content.slice(0, 60);
  const first = blocks.find((b) => b.content.trim() && b.block_type !== 'divider');
  return first ? first.content.slice(0, 60) : null;
}

export function reindexBlocks(blocks: Block[]): Block[] {
  return blocks.map((b, i) => ({ ...b, order: i }));
}

export function cosineDistance(a: number[], b: number[]): number {
  let dot = 0;
  let magA = 0;
  let magB = 0;
  for (let i = 0; i < a.length; i++) {
    dot += a[i] * b[i];
    magA += a[i] * a[i];
    magB += b[i] * b[i];
  }
  if (magA === 0 || magB === 0) return 0;
  return dot / (Math.sqrt(magA) * Math.sqrt(magB));
}
