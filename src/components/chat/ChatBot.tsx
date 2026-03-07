import React, { useEffect, useRef, useState } from 'react';
import { useChatStore } from '../../store/chatStore';
import { useSettingsStore } from '../../store/settingsStore';
import { MarkdownRenderer } from './MarkdownRenderer';
import { cosineDistance } from '../../utils/blocks';
import * as db from '../../services/db';
import { generateEmbedding } from '../../services/aiService';
import type { Note } from '../../types';

export function ChatBot() {
  const { messages, isStreaming, expandedToolCallIds, expandedNoteContextIds, sendMessage, clearChat, toggleToolCalls, toggleNoteContext } = useChatStore();
  const { openAiApiKey, chatModel, embeddingModel, topNRelevantNotes, minSimilarityThreshold } = useSettingsStore();
  
  
  const [input, setInput] = useState('');
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const getRelevantNotes = async (text: string): Promise<Note[]> => {
    if (!topNRelevantNotes || !openAiApiKey || !text) return [];
    try {
      const queryEmbedding = await generateEmbedding(text, openAiApiKey, embeddingModel);
      const allEmbeddings = await db.getAllEmbeddings();
      const scored = allEmbeddings
        .map((e) => {
          try {
            const vec = JSON.parse(e.embedding) as number[];
            const score = cosineDistance(queryEmbedding, vec);
            return { note_id: e.note_id, score };
          } catch { return null; }
        })
        .filter((x): x is { note_id: string; score: number } => x !== null && x.score >= minSimilarityThreshold)
        .sort((a, b) => b.score - a.score)
        .slice(0, topNRelevantNotes);

      const relevantNotes: Note[] = [];
      for (const { note_id } of scored) {
        const note = await db.getNote(note_id);
        if (note) relevantNotes.push(note);
      }
      return relevantNotes;
    } catch {
      return [];
    }
  };

  const handleSend = async () => {
    const text = input.trim();
    if (!text || isStreaming) return;
    setInput('');
    await sendMessage(text, openAiApiKey, chatModel, () => getRelevantNotes(text));
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  // Auto-resize textarea
  useEffect(() => {
    const el = textareaRef.current;
    if (!el) return;
    el.style.height = 'auto';
    el.style.height = Math.min(el.scrollHeight, 120) + 'px';
  }, [input]);

  return (
    <div style={{ display: 'flex', flexDirection: 'column', height: '100%', overflow: 'hidden' }}>
      {/* Messages */}
      <div style={{ flex: 1, overflowY: 'auto', padding: '8px 0' }}>
        {messages.length === 0 ? (
          <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100%', gap: 8 }}>
            <BotIcon style={{ color: 'var(--text-muted)', width: 32, height: 32 }} />
            <p style={{ color: 'var(--text-muted)', fontSize: 12, textAlign: 'center', padding: '0 20px' }}>
              Ask me anything about your notes or let me help you create and edit them.
            </p>
          </div>
        ) : (
          messages.map((msg) => {
            const isUser = msg.role === 'user';
            return (
              <div key={msg.id} style={{ padding: '3px 12px', display: 'flex', justifyContent: isUser ? 'flex-end' : 'flex-start' }}>
                {isUser ? (
                  /* User bubble — right side */
                  <div style={{ maxWidth: '82%', display: 'flex', flexDirection: 'column', alignItems: 'flex-end' }}>
                    <div style={{
                      background: 'var(--bg-active)', borderRadius: '14px 14px 3px 14px',
                      padding: '7px 12px', fontSize: 13, color: 'var(--text-primary)', lineHeight: 1.55,
                    }} className="selectable">
                      <p style={{ margin: 0 }}>{msg.content}</p>
                    </div>
                    <span style={{ fontSize: 10, color: 'var(--text-muted)', marginTop: 2, paddingRight: 2 }}>
                      {new Date(msg.timestamp).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' })}
                    </span>
                  </div>
                ) : (
                  /* Assistant — left side with avatar */
                  <div style={{ display: 'flex', gap: 8, alignItems: 'flex-start', maxWidth: '92%' }}>
                    <div style={{
                      width: 24, height: 24, borderRadius: '50%', flexShrink: 0, marginTop: 4,
                      background: 'var(--accent-light)', display: 'flex', alignItems: 'center', justifyContent: 'center',
                    }}>
                      <BotIcon />
                    </div>
                    <div style={{ flex: 1, minWidth: 0 }}>
                      {/* Tool calls */}
                      {msg.tool_calls && msg.tool_calls.length > 0 && (() => {
                        const hasRunning = msg.tool_calls.some((t) => !t.is_completed);
                        const hasText = !!msg.content;
                        return (
                          <div style={{ marginBottom: hasText ? 6 : 0 }}>
                            {hasRunning ? (
                              <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                                {msg.tool_calls.map((tc) => (
                                  <ToolCallItem key={tc.id} label={tc.label} completed={tc.is_completed} />
                                ))}
                              </div>
                            ) : hasText ? (
                              <>
                                <button
                                  onClick={() => toggleToolCalls(msg.id)}
                                  style={{
                                    display: 'flex', alignItems: 'center', gap: 6, background: 'var(--bg-secondary)',
                                    border: '1px solid var(--border-color)', borderRadius: 'var(--radius-sm)',
                                    padding: '3px 8px', cursor: 'pointer', fontSize: 11, color: 'var(--text-muted)',
                                  }}
                                >
                                  <WrenchIcon />
                                  <span>{msg.tool_calls.length} action{msg.tool_calls.length !== 1 ? 's' : ''}</span>
                                  <ChevronIcon up={expandedToolCallIds.has(msg.id)} />
                                </button>
                                {expandedToolCallIds.has(msg.id) && (
                                  <div style={{ marginTop: 4, display: 'flex', flexDirection: 'column', gap: 2 }}>
                                    {msg.tool_calls.map((tc) => <ToolCallItem key={tc.id} label={tc.label} completed />)}
                                  </div>
                                )}
                              </>
                            ) : (
                              <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                                {msg.tool_calls.map((tc) => <ToolCallItem key={tc.id} label={tc.label} completed={tc.is_completed} />)}
                                {msg.is_streaming && (
                                  <div style={{ display: 'flex', gap: 4, padding: '4px 0' }}>
                                    <span className="typing-dot" /><span className="typing-dot" /><span className="typing-dot" />
                                  </div>
                                )}
                              </div>
                            )}
                          </div>
                        );
                      })()}

                      {/* Thinking dots */}
                      {msg.is_streaming && !msg.content && (!msg.tool_calls || msg.tool_calls.length === 0) && (
                        <div style={{ display: 'flex', gap: 4, padding: '6px 0' }}>
                          <span className="typing-dot" /><span className="typing-dot" /><span className="typing-dot" />
                        </div>
                      )}

                      {/* Message text */}
                      {msg.content && (
                        <div style={{ fontSize: 13, color: 'var(--text-primary)', lineHeight: 1.6 }} className="selectable">
                          <MarkdownRenderer content={msg.content} />
                        </div>
                      )}

                      {/* Context notes chip */}
                      {msg.context_note_titles && msg.context_note_titles.length > 0 && (
                        <div style={{ marginTop: 6 }}>
                          <button
                            onClick={() => toggleNoteContext(msg.id)}
                            style={{
                              display: 'flex', alignItems: 'center', gap: 6, background: 'var(--bg-secondary)',
                              border: '1px solid var(--border-color)', borderRadius: 'var(--radius-sm)',
                              padding: '3px 8px', cursor: 'pointer', fontSize: 11, color: 'var(--text-muted)',
                            }}
                          >
                            <DocIcon />
                            <span>{msg.context_note_titles.length} note{msg.context_note_titles.length !== 1 ? 's' : ''} referenced</span>
                            <ChevronIcon up={expandedNoteContextIds.has(msg.id)} />
                          </button>
                          {expandedNoteContextIds.has(msg.id) && (
                            <div style={{ marginTop: 4, display: 'flex', flexDirection: 'column', gap: 2 }}>
                              {msg.context_note_titles.map((t, i) => (
                                <div key={i} style={{ fontSize: 11, color: 'var(--text-muted)', paddingLeft: 8, display: 'flex', gap: 4, alignItems: 'center' }}>
                                  <DocIcon /><span>{t}</span>
                                </div>
                              ))}
                            </div>
                          )}
                        </div>
                      )}

                      <span style={{ fontSize: 10, color: 'var(--text-muted)', marginTop: 2, display: 'block' }}>
                        {new Date(msg.timestamp).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' })}
                      </span>
                    </div>
                  </div>
                )}
              </div>
            );
          })
        )}
        <div ref={messagesEndRef} />
      </div>

      {/* Input area */}
      <div style={{ padding: '8px 10px', borderTop: '1px solid var(--border-subtle)', flexShrink: 0 }}>
        <div style={{
          display: 'flex', gap: 6, background: 'var(--bg-card)',
          border: '1px solid var(--border-color)', borderRadius: 'var(--radius-md)',
          padding: '6px 8px', alignItems: 'flex-end',
        }}>
          <textarea
            ref={textareaRef}
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Type a message..."
            disabled={isStreaming}
            rows={1}
            className="selectable"
            style={{
              flex: 1, background: 'none', border: 'none', outline: 'none',
              fontSize: 13, color: 'var(--text-primary)', resize: 'none',
              fontFamily: 'inherit', lineHeight: 1.5, overflow: 'hidden',
            }}
          />
          <div style={{ display: 'flex', gap: 4, flexShrink: 0 }}>
            {messages.length > 0 && (
              <button
                onClick={clearChat}
                disabled={isStreaming}
                title="Clear chat"
                style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--text-muted)', padding: '3px 4px' }}
              >
                <TrashIcon />
              </button>
            )}
            <button
              onClick={handleSend}
              disabled={!input.trim() || isStreaming}
              title="Send"
              style={{
                background: input.trim() && !isStreaming ? 'var(--accent)' : 'var(--bg-hover)',
                border: 'none', cursor: input.trim() && !isStreaming ? 'pointer' : 'default',
                color: input.trim() && !isStreaming ? 'var(--bg-primary)' : 'var(--text-muted)',
                padding: '4px 6px', borderRadius: 'var(--radius-sm)', display: 'flex', alignItems: 'center',
                transition: 'background var(--transition-fast)',
              }}
            >
              <SendIcon />
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

function ToolCallItem({ label, completed }: { label: string; completed: boolean }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 11, color: 'var(--text-muted)' }}>
      {completed ? (
        <CheckCircleIcon style={{ color: 'var(--color-success)' }} />
      ) : (
        <div style={{ width: 12, height: 12, borderRadius: '50%', border: '2px solid var(--text-muted)', borderTopColor: 'transparent', animation: 'spin 0.8s linear infinite' }} />
      )}
      <span style={{ textDecoration: completed ? 'none' : 'none' }}>{label}</span>
    </div>
  );
}

const BotIcon = ({ style }: { style?: React.CSSProperties }) => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" style={style}>
    <rect x="3" y="11" width="18" height="10" rx="2"/><circle cx="12" cy="5" r="2"/>
    <line x1="12" y1="7" x2="12" y2="11"/>
    <line x1="8" y1="15" x2="8" y2="17"/><line x1="16" y1="15" x2="16" y2="17"/>
  </svg>
);
const SendIcon = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <line x1="22" y1="2" x2="11" y2="13"/><polygon points="22 2 15 22 11 13 2 9 22 2"/>
  </svg>
);
const TrashIcon = () => (
  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14H6L5 6"/>
    <path d="M10 11v6"/><path d="M14 11v6"/><path d="M9 6V4h6v2"/>
  </svg>
);
const WrenchIcon = () => (
  <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z"/>
  </svg>
);
const DocIcon = () => (
  <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
    <polyline points="14 2 14 8 20 8"/>
  </svg>
);
const ChevronIcon = ({ up }: { up: boolean }) => (
  <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" style={{ transform: up ? 'rotate(180deg)' : 'none' }}>
    <polyline points="6 9 12 15 18 9"/>
  </svg>
);
const CheckCircleIcon = ({ style }: { style?: React.CSSProperties }) => (
  <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" style={style}>
    <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/>
  </svg>
);
