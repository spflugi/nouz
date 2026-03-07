import OpenAI from 'openai';
import type { ChatMessage, Note } from '../types';
import { getNoteTitle } from '../utils/blocks';
import * as db from './db';
import { useNotebookStore } from '../store/notebookStore';
import { useNoteStore } from '../store/noteStore';
import { v4 as uuidv4 } from '../utils/uuid';

const BASE_SYSTEM_PROMPT = `You are a helpful assistant integrated into a note-taking application called Nouz.
Be concise and helpful. You can help users with their notes, answer questions, and provide information.

You have access to note management functions that allow you to:
- List and create notebooks
- Create, read, edit, and delete notes
- Search for notes across all notebooks

When creating or editing notes, use structured blocks. Available block types:
- h1, h2, h3, h4: Headings (use h1 for main title)
- paragraph: Regular text
- todoitem: Checkbox/task item. To mark as done, set metadata {"checked":true}
- agendaitem: Agenda item for meetings
- listitem: Bullet point
- code: Code block
- quote: Block quote
- decision: Decision block (for recording decisions)
- warning: Warning/alert block
- idea: Idea or insight block

HOW TO ACT:
- Be autonomous: complete operations in a single response using multiple tool calls without asking for mid-flow confirmation.
- When you need a notebook ID, call list_notebooks first, match by name (case-insensitive), then proceed immediately — do not ask the user to repeat which notebook.
- When the user asks to create a note, just create it. Do not ask "should I proceed?" or "shall I create it?" — act directly.
- When the user asks to edit a note, use get_note_content to read it, then call edit_note with the updated content — do not ask for confirmation before editing.
- Only for DELETE operations: use get_note_content to show what will be deleted and ask "Are you sure you want to delete this note?" before calling delete_note.
- If you genuinely cannot determine which notebook to use (user didn't mention one and there are multiple), ask once with a list of options.
- Always confirm the result to the user after completing an operation.`;

function buildSystemPrompt(relevantNotes?: Note[]): string {
  if (!relevantNotes?.length) return BASE_SYSTEM_PROMPT;

  let prompt = BASE_SYSTEM_PROMPT + '\n\nHere are relevant notes from the user\'s notebook:\n\n';
  relevantNotes.forEach((note, i) => {
    prompt += `--- Note ${i + 1} ---\n`;
    note.blocks.forEach((block) => {
      if (block.content.trim()) {
        prompt += block.content + '\n';
      }
    });
    prompt += '\n';
  });
  prompt += 'Use the above notes as context when relevant.';
  return prompt;
}

// Tool definitions
const NOTE_TOOLS: OpenAI.Chat.ChatCompletionTool[] = [
  {
    type: 'function',
    function: {
      name: 'list_notebooks',
      description: 'Lists all available notebooks with their IDs and names.',
      parameters: { type: 'object', properties: {}, required: [] },
    },
  },
  {
    type: 'function',
    function: {
      name: 'create_notebook',
      description: 'Creates a new notebook with the specified name.',
      parameters: {
        type: 'object',
        properties: { name: { type: 'string', description: 'The name for the new notebook' } },
        required: ['name'],
      },
    },
  },
  {
    type: 'function',
    function: {
      name: 'get_notebook_notes',
      description: 'Gets all notes in a specific notebook.',
      parameters: {
        type: 'object',
        properties: { notebook_id: { type: 'string' } },
        required: ['notebook_id'],
      },
    },
  },
  {
    type: 'function',
    function: {
      name: 'create_note',
      description: 'Creates a new note in the specified notebook. Use list_notebooks first to resolve a notebook name to its ID if needed. Blocks is a JSON array of {type, content} objects.',
      parameters: {
        type: 'object',
        properties: {
          notebook_id: { type: 'string' },
          blocks: {
            type: 'string',
            description: 'JSON array of blocks, e.g. [{"type":"h1","content":"Title"},{"type":"paragraph","content":"Body"}]',
          },
        },
        required: ['notebook_id', 'blocks'],
      },
    },
  },
  {
    type: 'function',
    function: {
      name: 'get_note_content',
      description: 'Gets the full content of a specific note including all blocks.',
      parameters: {
        type: 'object',
        properties: { note_id: { type: 'string' } },
        required: ['note_id'],
      },
    },
  },
  {
    type: 'function',
    function: {
      name: 'edit_note',
      description: 'Updates an existing note with new content. Use get_note_content first to read the current content, then call this with the full updated block structure.',
      parameters: {
        type: 'object',
        properties: {
          note_id: { type: 'string' },
          new_blocks: {
            type: 'string',
            description: 'JSON array of the complete new block structure.',
          },
        },
        required: ['note_id', 'new_blocks'],
      },
    },
  },
  {
    type: 'function',
    function: {
      name: 'delete_note',
      description: 'Permanently deletes a note. Only call after the user has explicitly confirmed the deletion.',
      parameters: {
        type: 'object',
        properties: { note_id: { type: 'string' } },
        required: ['note_id'],
      },
    },
  },
  {
    type: 'function',
    function: {
      name: 'search_notes',
      description: 'Searches for notes matching the query.',
      parameters: {
        type: 'object',
        properties: { query: { type: 'string' } },
        required: ['query'],
      },
    },
  },
];

async function executeToolCall(name: string, args: Record<string, string>): Promise<string> {
  try {
    switch (name) {
      case 'list_notebooks': {
        const notebooks = await db.getNotebooks();
        if (!notebooks.length) return 'No notebooks found.';
        return `Found ${notebooks.length} notebook(s):\n` +
          notebooks.map((n) => `- ${n.name} (ID: ${n.id})`).join('\n');
      }

      case 'create_notebook': {
        const nb = await db.createNotebook(args.name);
        // Update store
        useNotebookStore.getState().load();
        return `Successfully created notebook "${nb.name}" (ID: ${nb.id}).`;
      }

      case 'get_notebook_notes': {
        const notes = await db.getNotes(args.notebook_id);
        if (!notes.length) return 'This notebook has no notes yet.';
        return `Found ${notes.length} note(s):\n` +
          notes.slice(0, 20).map((n) => {
            const title = getNoteTitle(n.blocks) ?? '(Untitled)';
            return `- ${title} (ID: ${n.id})`;
          }).join('\n');
      }

      case 'create_note': {
        const rawBlocks = extractJsonArray(args.blocks);
        const parsed = JSON.parse(rawBlocks) as Array<{ type: string; content: string; metadata?: Record<string, unknown> }>;
        const blockInputs = parsed.map((b, i) => ({
          id: uuidv4(),
          block_type: b.type as any,
          content: b.content ?? '',
          metadata: JSON.stringify(b.metadata ?? {}),
          order: i,
        }));
        const note = await db.createNote(args.notebook_id, blockInputs);
        useNoteStore.getState().loadNotes(args.notebook_id);
        const title = getNoteTitle(note.blocks) ?? 'New note';
        return `Successfully created note "${title}" (ID: ${note.id}).`;
      }

      case 'get_note_content': {
        const note = await db.getNote(args.note_id);
        if (!note) return `Note ${args.note_id} not found.`;
        const lines = note.blocks.map((b) => {
          switch (b.block_type) {
            case 'h1': return `# ${b.content}`;
            case 'h2': return `## ${b.content}`;
            case 'h3': return `### ${b.content}`;
            case 'h4': return `#### ${b.content}`;
            case 'listitem': return `- ${b.content}`;
            case 'todoitem': {
              const m = JSON.parse(b.metadata || '{}');
              return `- [${m.checked ? 'x' : ' '}] ${b.content}`;
            }
            case 'quote': return `> ${b.content}`;
            case 'code': return `\`\`\`\n${b.content}\n\`\`\``;
            case 'decision': return `[Decision] ${b.content}`;
            case 'warning': return `[Warning] ${b.content}`;
            case 'idea': return `[Idea] ${b.content}`;
            case 'agendaitem': return `> ${b.content}`;
            default: return b.content;
          }
        });
        return `Note ID: ${note.id}\n\n${lines.join('\n')}`;
      }

      case 'edit_note': {
        const rawBlocks = extractJsonArray(args.new_blocks);
        const parsed = JSON.parse(rawBlocks) as Array<{ type: string; content: string }>;
        const note = await db.getNote(args.note_id);
        if (!note) return `Note ${args.note_id} not found.`;
        const blockInputs = parsed.map((b, i) => ({
          id: uuidv4(),
          block_type: b.type as any,
          content: b.content ?? '',
          metadata: '{}',
          order: i,
        }));
        const updated = await db.saveNote(args.note_id, blockInputs);
        useNoteStore.getState().loadNotes(updated.notebook_id);
        return `Successfully updated note.`;
      }

      case 'delete_note': {
        const note = await db.getNote(args.note_id);
        if (!note) return `Note ${args.note_id} not found.`;
        await db.deleteNote(args.note_id);
        useNoteStore.getState().loadNotes(note.notebook_id);
        return `Successfully deleted note.`;
      }

      case 'search_notes': {
        const notes = await db.searchNotes(args.query);
        if (!notes.length) return 'No notes found matching your search.';
        return `Found ${notes.length} note(s):\n` +
          notes.map((n) => {
            const title = getNoteTitle(n.blocks) ?? '(Untitled)';
            return `- ${title} (ID: ${n.id})`;
          }).join('\n');
      }

      default:
        return `Unknown tool: ${name}`;
    }
  } catch (err: any) {
    return `Error: ${err?.message ?? String(err)}`;
  }
}

function extractJsonArray(input: string): string {
  const start = input.indexOf('[');
  if (start === -1) return input;
  let depth = 0;
  for (let i = start; i < input.length; i++) {
    if (input[i] === '[') depth++;
    else if (input[i] === ']') {
      depth--;
      if (depth === 0) return input.slice(start, i + 1);
    }
  }
  return input.slice(start);
}

interface StreamOptions {
  apiKey: string;
  model: string;
  message: string;
  history: ChatMessage[];
  relevantNotes?: Note[];
  onChunk: (text: string) => void;
  onToolCall: (label: string, isCompleted: boolean) => void;
  onContextNotes: (titles: string[]) => void;
}

export async function streamChatResponse(opts: StreamOptions): Promise<void> {
  const { apiKey, model, message, history, relevantNotes, onChunk, onToolCall, onContextNotes } = opts;

  if (!apiKey) {
    onChunk('Please configure your OpenAI API key in Settings to use the assistant.');
    return;
  }

  if (relevantNotes?.length) {
    const titles = relevantNotes
      .map((n) => getNoteTitle(n.blocks))
      .filter(Boolean) as string[];
    onContextNotes(titles);
  }

  const client = new OpenAI({ apiKey, dangerouslyAllowBrowser: true });

  const messages: OpenAI.Chat.ChatCompletionMessageParam[] = [
    { role: 'system', content: buildSystemPrompt(relevantNotes) },
    ...history.map((m) => ({
      role: m.role as 'user' | 'assistant',
      content: m.content,
    })),
    { role: 'user', content: message },
  ];

  // Agentic loop — keep calling until no more tool calls
  let continueLoop = true;
  while (continueLoop) {
    const stream = await client.chat.completions.create({
      model,
      messages,
      tools: NOTE_TOOLS,
      tool_choice: 'auto',
      stream: true,
    });

    let assistantContent = '';
    const toolCallMap: Record<string, { name: string; arguments: string }> = {};

    for await (const chunk of stream) {
      const delta = chunk.choices[0]?.delta;
      if (!delta) continue;

      if (delta.content) {
        assistantContent += delta.content;
        onChunk(delta.content);
      }

      if (delta.tool_calls) {
        for (const tc of delta.tool_calls) {
          const idx = String(tc.index ?? 0);
          if (!toolCallMap[idx]) {
            toolCallMap[idx] = { name: '', arguments: '' };
          }
          if (tc.function?.name) toolCallMap[idx].name += tc.function.name;
          if (tc.function?.arguments) toolCallMap[idx].arguments += tc.function.arguments;
        }
      }
    }

    const toolCalls = Object.values(toolCallMap);
    if (toolCalls.length === 0) {
      continueLoop = false;
      break;
    }

    // Add assistant message with tool calls to history
    messages.push({
      role: 'assistant',
      content: assistantContent || null,
      tool_calls: toolCalls.map((tc, i) => ({
        id: `call_${i}`,
        type: 'function' as const,
        function: { name: tc.name, arguments: tc.arguments },
      })),
    });

    // Execute each tool call
    for (let i = 0; i < toolCalls.length; i++) {
      const tc = toolCalls[i];
      const label = formatToolLabel(tc.name);
      onToolCall(label, false);

      let args: Record<string, string> = {};
      try {
        args = JSON.parse(tc.arguments);
      } catch {}

      const result = await executeToolCall(tc.name, args);
      onToolCall(label, true);

      messages.push({
        role: 'tool',
        tool_call_id: `call_${i}`,
        content: result,
      });
    }
  }
}

function formatToolLabel(name: string): string {
  const labels: Record<string, string> = {
    list_notebooks: 'Listing notebooks',
    create_notebook: 'Creating notebook',
    get_notebook_notes: 'Reading notes',
    create_note: 'Creating note',
    get_note_content: 'Reading note',
    edit_note: 'Editing note',
    delete_note: 'Deleting note',
    search_notes: 'Searching notes',
  };
  return labels[name] ?? name;
}

export async function generateEmbedding(text: string, apiKey: string, model: string): Promise<number[]> {
  const client = new OpenAI({ apiKey, dangerouslyAllowBrowser: true });
  const response = await client.embeddings.create({ model, input: text });
  return response.data[0].embedding;
}
