import React, { useCallback, useEffect, useRef, useState } from 'react';
import type { Block, BlockType } from '../../types';
import { parseMetadata, stringifyMetadata } from '../../utils/blocks';
import { BlockContextMenu } from './BlockContextMenu';
import { FormattingToolbar } from './FormattingToolbar';
import { MermaidBlock } from './blocks/MermaidBlock';
import { TableBlock } from './blocks/TableBlock';
import { ImageBlock } from './blocks/ImageBlock';

interface Props {
  block: Block;
  index: number;
  isEditing: boolean;

  onContentChange: (updated: Block) => void;
  onEnterPressed: (afterId: string, newType?: BlockType, metadata?: string) => void;
  onDeletePressed: (id: string) => void;
  onTypeChange: (id: string, type: BlockType) => void;
  onReorder: (dragId: string, overIdx: number) => void;
}

export function BlockRenderer({
  block, index, isEditing,
  onContentChange, onEnterPressed, onDeletePressed, onTypeChange, onReorder,
}: Props) {
  const contentRef = useRef<HTMLElement>(null);
  const [showMenu, setShowMenu] = useState(false);
  const [showContextMenu, setShowContextMenu] = useState(false);
  const [isDragOver, setIsDragOver] = useState(false);
  const [isDragging, setIsDragging] = useState(false);
  const [selectionRect, setSelectionRect] = useState<DOMRect | null>(null);
  const [isBold, setIsBold] = useState(false);
  const [isItalic, setIsItalic] = useState(false);

  // Sync content from block prop into DOM (only when not focused)
  useEffect(() => {
    const el = contentRef.current;
    if (!el) return;
    if (document.activeElement !== el && el.innerHTML !== block.content) {
      el.innerHTML = block.content;
    }
  }, [block.content]);

  const handleInput = useCallback(() => {
    const el = contentRef.current;
    if (!el) return;
    onContentChange({ ...block, content: el.innerHTML });
  }, [block, onContentChange]);

  const handleKeyDown = useCallback((e: React.KeyboardEvent) => {
    const isListLike = block.block_type === 'listitem' || block.block_type === 'agendaitem';

    if (e.key === 'Tab' && isListLike) {
      e.preventDefault();
      const meta = parseMetadata(block.metadata);
      const currentIndent = (meta as any).indent ?? 0;
      const newIndent = e.shiftKey ? Math.max(0, currentIndent - 1) : currentIndent + 1;
      onContentChange({ ...block, metadata: stringifyMetadata({ ...meta, indent: newIndent }) });
      return;
    }

    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      // On code blocks allow normal newline behavior
      if (block.block_type === 'code') {
        document.execCommand('insertHTML', false, '\n');
        return;
      }
      if (isListLike) {
        const meta = parseMetadata(block.metadata);
        const indent = (meta as any).indent ?? 0;
        onEnterPressed(block.id, block.block_type, stringifyMetadata({ indent }));
      } else if (block.block_type === 'todoitem') {
        onEnterPressed(block.id, 'todoitem');
      } else {
        onEnterPressed(block.id);
      }
    }
    if (e.key === 'Backspace') {
      const el = contentRef.current;
      if (el && el.innerHTML === '') {
        e.preventDefault();
        onDeletePressed(block.id);
      }
    }
    // Slash command
    if (e.key === '/' && block.block_type === 'paragraph') {
      const el = contentRef.current;
      if (el && el.innerHTML === '') {
        e.preventDefault();
        setShowContextMenu(true);
      }
    }
  }, [block, onEnterPressed, onDeletePressed, onContentChange]);

  const handleMouseUp = useCallback(() => {
    const sel = window.getSelection();
    if (sel && sel.rangeCount > 0 && !sel.isCollapsed) {
      const range = sel.getRangeAt(0);
      setSelectionRect(range.getBoundingClientRect());
      setIsBold(document.queryCommandState('bold'));
      setIsItalic(document.queryCommandState('italic'));
    } else {
      setSelectionRect(null);
    }
  }, []);

  useEffect(() => {
    const handleSelectionChange = () => {
      const sel = window.getSelection();
      if (!sel || sel.isCollapsed) {
        setSelectionRect(null);
      }
    };
    document.addEventListener('selectionchange', handleSelectionChange);
    return () => document.removeEventListener('selectionchange', handleSelectionChange);
  }, []);

  const handleFormat = (type: 'bold' | 'italic') => {
    document.execCommand(type, false);
    const el = contentRef.current;
    if (el) onContentChange({ ...block, content: el.innerHTML });
    setSelectionRect(null);
  };

  const handleColorSelected = (color: string | null) => {
    if (color) {
      document.execCommand('foreColor', false, color);
    } else {
      document.execCommand('removeFormat', false, 'foreColor');
    }
    const el = contentRef.current;
    if (el) onContentChange({ ...block, content: el.innerHTML });
    setSelectionRect(null);
  };

  // Drag and drop
  const handleDragStart = (e: React.DragEvent) => {
    e.dataTransfer.setData('block-id', block.id);
    e.dataTransfer.setData('block-index', String(index));
    setIsDragging(true);
  };
  const handleDragEnd = () => setIsDragging(false);
  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(true);
  };
  const handleDragLeave = () => setIsDragOver(false);
  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);
    const dragId = e.dataTransfer.getData('block-id');
    if (dragId && dragId !== block.id) {
      onReorder(dragId, index);
    }
  };

  const editableProps = {
    contentEditable: isEditing,
    suppressContentEditableWarning: true,
    onInput: handleInput,
    onKeyDown: handleKeyDown,
    onMouseUp: handleMouseUp,

    spellCheck: block.block_type !== 'code',
  };

  const meta = parseMetadata(block.metadata);

  const renderContent = () => {
    switch (block.block_type) {
      case 'h1':
        return (
          <h1
            ref={contentRef as React.RefObject<HTMLHeadingElement>}
            data-placeholder="Heading 1"
            className="block-h1 selectable"
            style={{ color: 'var(--text-primary)', outline: 'none' }}
            {...editableProps}
          />
        );
      case 'h2':
        return (
          <h2
            ref={contentRef as React.RefObject<HTMLHeadingElement>}
            data-placeholder="Heading 2"
            className="block-h2 selectable"
            style={{ color: 'var(--text-primary)', outline: 'none' }}
            {...editableProps}
          />
        );
      case 'h3':
        return (
          <h3
            ref={contentRef as React.RefObject<HTMLHeadingElement>}
            data-placeholder="Heading 3"
            className="block-h3 selectable"
            style={{ color: 'var(--text-primary)', outline: 'none' }}
            {...editableProps}
          />
        );
      case 'h4':
        return (
          <h4
            ref={contentRef as React.RefObject<HTMLHeadingElement>}
            data-placeholder="Heading 4"
            className="block-h4 selectable"
            style={{ color: 'var(--text-primary)', outline: 'none' }}
            {...editableProps}
          />
        );
      case 'listitem': {
        const indent = (meta as any).indent ?? 0;
        return (
          <div style={{ display: 'flex', alignItems: 'flex-start', gap: 8, paddingLeft: indent * 20 }}>
            <span style={{ color: 'var(--text-secondary)', flexShrink: 0, marginTop: 2, fontSize: 16, lineHeight: 1 }}>•</span>
            <span
              ref={contentRef as React.RefObject<HTMLSpanElement>}
              data-placeholder="List item"
              style={{ outline: 'none', flex: 1, color: 'var(--text-primary)' }}
              {...editableProps}
            />
          </div>
        );
      }
      case 'todoitem': {
        const checked = (meta as any).checked ?? false;
        const toggleCheck = () => {
          if (!isEditing) return;
          onContentChange({ ...block, metadata: stringifyMetadata({ ...meta, checked: !checked }) });
        };
        return (
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <div
              onClick={toggleCheck}
              style={{
                width: 14, height: 14, flexShrink: 0,
                border: `1.5px solid ${checked ? 'var(--accent)' : 'var(--border-color)'}`,
                borderRadius: 3,
                background: checked ? 'var(--accent)' : 'transparent',
                cursor: isEditing ? 'pointer' : 'default',
                display: 'flex', alignItems: 'center', justifyContent: 'center',
                transition: `background var(--transition-fast), border-color var(--transition-fast)`,
                color: 'var(--bg-card)',
              }}
            >
              {checked && (
                <svg width="9" height="7" viewBox="0 0 9 7" fill="none">
                  <path d="M1 3.5L3.5 6L8 1" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
                </svg>
              )}
            </div>
            <span
              ref={contentRef as React.RefObject<HTMLSpanElement>}
              data-placeholder="To-do"
              style={{
                outline: 'none', flex: 1, color: 'var(--text-primary)',
                textDecoration: checked ? 'line-through' : 'none',
                opacity: checked ? 0.6 : 1,
              }}
              {...editableProps}
            />
          </div>
        );
      }
      case 'agendaitem': {
        const indent = (meta as any).indent ?? 0;
        return (
          <div style={{ display: 'flex', alignItems: 'flex-start', gap: 6, paddingLeft: indent * 20 }}>
            <span style={{ color: 'var(--text-muted)', flexShrink: 0, marginTop: -1, fontSize: 18, lineHeight: 1, userSelect: 'none' }}>›</span>
            <span
              ref={contentRef as React.RefObject<HTMLSpanElement>}
              data-placeholder="Agenda item"
              style={{ outline: 'none', flex: 1, color: 'var(--text-primary)' }}
              {...editableProps}
            />
          </div>
        );
      }
      case 'code':
        return (
          <pre className="block-code selectable" style={{ margin: 0 }}>
            <code
              ref={contentRef as React.RefObject<HTMLElement>}
              data-placeholder="Code"
              style={{ outline: 'none', display: 'block' }}
              {...editableProps}
              spellCheck={false}
            />
          </pre>
        );
      case 'quote':
        return (
          <blockquote style={{
            borderLeft: '3px solid var(--accent)', paddingLeft: 12, margin: 0,
            color: 'var(--text-secondary)', fontStyle: 'italic',
          }}>
            <span
              ref={contentRef as React.RefObject<HTMLSpanElement>}
              data-placeholder="Quote"
              style={{ outline: 'none', display: 'block' }}
              {...editableProps}
            />
          </blockquote>
        );
      case 'decision':
        return (
          <div style={{
            background: 'var(--color-info-bg)', borderLeft: '3px solid var(--color-info)',
            borderRadius: '0 var(--radius-sm) var(--radius-sm) 0', padding: '8px 12px',
            display: 'flex', gap: 8, alignItems: 'baseline',
          }}>
            <span style={{ fontSize: 11, fontWeight: 600, color: 'var(--color-info)', flexShrink: 0, textTransform: 'uppercase', letterSpacing: '0.05em' }}>Decision</span>
            <span
              ref={contentRef as React.RefObject<HTMLSpanElement>}
              data-placeholder="Decision"
              style={{ outline: 'none', flex: 1, color: 'var(--text-primary)' }}
              {...editableProps}
            />
          </div>
        );
      case 'warning':
        return (
          <div style={{
            background: 'var(--color-warning-bg)', borderLeft: '3px solid var(--color-warning)',
            borderRadius: '0 var(--radius-sm) var(--radius-sm) 0', padding: '8px 12px',
            display: 'flex', gap: 8, alignItems: 'baseline',
          }}>
            <span style={{ fontSize: 11, fontWeight: 600, color: 'var(--color-warning)', flexShrink: 0, textTransform: 'uppercase', letterSpacing: '0.05em' }}>Warning</span>
            <span
              ref={contentRef as React.RefObject<HTMLSpanElement>}
              data-placeholder="Warning"
              style={{ outline: 'none', flex: 1, color: 'var(--text-primary)' }}
              {...editableProps}
            />
          </div>
        );
      case 'idea': {
        const ideaStatus = (meta as any).status ?? 'new';
        const statusColors: Record<string, string> = {
          new: 'var(--color-info)',
          exploring: 'var(--color-warning)',
          validated: 'var(--color-success)',
          discarded: 'var(--text-muted)',
        };
        const cycleStatus = () => {
          if (!isEditing) return;
          const statuses = ['new', 'exploring', 'validated', 'discarded'];
          const next = statuses[(statuses.indexOf(ideaStatus) + 1) % statuses.length];
          onContentChange({ ...block, metadata: stringifyMetadata({ ...meta, status: next }) });
        };
        return (
          <div style={{
            background: 'var(--color-info-bg)', borderRadius: 'var(--radius-md)',
            padding: '8px 12px', display: 'flex', gap: 8, alignItems: 'flex-start',
          }}>
            <span style={{ flexShrink: 0, marginTop: 1 }}>💡</span>
            <span
              ref={contentRef as React.RefObject<HTMLSpanElement>}
              data-placeholder="Idea"
              style={{ outline: 'none', flex: 1, color: 'var(--text-primary)' }}
              {...editableProps}
            />
            <button
              onClick={cycleStatus}
              style={{
                fontSize: 10, fontWeight: 600, textTransform: 'uppercase',
                letterSpacing: '0.04em', padding: '2px 6px', borderRadius: 'var(--radius-sm)',
                border: `1px solid ${statusColors[ideaStatus]}`, background: 'none',
                color: statusColors[ideaStatus], cursor: isEditing ? 'pointer' : 'default',
                flexShrink: 0,
              }}
            >
              {ideaStatus}
            </button>
          </div>
        );
      }
      case 'divider':
        return (
          <div style={{ padding: '8px 0' }}>
            <hr style={{ border: 'none', borderTop: '1px solid var(--border-color)' }} />
          </div>
        );
      case 'image':
        return (
          <ImageBlock
            block={block}
            isEditing={isEditing}
            onContentChange={onContentChange}
          />
        );
      case 'mermaid':
        return (
          <MermaidBlock
            block={block}
            isEditing={isEditing}
            onContentChange={onContentChange}
          />
        );
      case 'table':
        return (
          <TableBlock
            block={block}
            isEditing={isEditing}
            onContentChange={onContentChange}
          />
        );
      default:
        return (
          <p
            ref={contentRef as React.RefObject<HTMLParagraphElement>}
            data-placeholder="Type something..."
            className="selectable" style={{ outline: 'none', color: 'var(--text-primary)', margin: 0 }}
            {...editableProps}
          />
        );
    }
  };

  return (
    <div
      data-block-id={block.id}
      style={{
        position: 'relative',
        padding: '2px 12px',
        borderTop: isDragOver ? '2px solid var(--accent)' : '2px solid transparent',
        opacity: isDragging ? 0.4 : 1,
        transition: 'opacity var(--transition-fast)',
      }}
      onMouseEnter={() => setShowMenu(true)}
      onMouseLeave={() => { setShowMenu(false); }}
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
    >
      {/* Flex row: always-present handle zone + content */}
      <div style={{ display: 'flex', alignItems: 'center' }}>
        {/* Handle zone — always reserves space so content never shifts */}
        <div style={{ width: 24, flexShrink: 0 }}>
          {isEditing && (
            <button
              draggable
              onDragStart={handleDragStart}
              onDragEnd={handleDragEnd}
              onClick={() => setShowContextMenu((s) => !s)}
              style={{
                background: 'none', border: 'none', cursor: 'grab', color: 'var(--text-muted)',
                padding: 2, display: 'flex', alignItems: 'center', borderRadius: 'var(--radius-sm)',
                opacity: showMenu || showContextMenu ? 1 : 0,
                transition: 'opacity var(--transition-fast)',
              }}
              title="Drag to reorder / click for options"
            >
              <DragIcon />
            </button>
          )}
        </div>

        {/* Block content */}
        <div style={{ flex: 1, minWidth: 0 }}>{renderContent()}</div>
      </div>

      {showContextMenu && (
        <BlockContextMenu
          currentType={block.block_type}
          onTypeSelected={(type) => { onTypeChange(block.id, type); setShowContextMenu(false); }}
          onAddBelow={() => { onEnterPressed(block.id); setShowContextMenu(false); }}
          onDelete={() => { onDeletePressed(block.id); setShowContextMenu(false); }}
          onClose={() => setShowContextMenu(false)}
        />
      )}

      {/* Formatting toolbar */}
      {selectionRect && (
        <FormattingToolbar
          rect={selectionRect}
          isBold={isBold}
          isItalic={isItalic}
          onFormat={handleFormat}
          onColorSelected={handleColorSelected}
        />
      )}
    </div>
  );
}

const DragIcon = () => (
  <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor">
    <circle cx="9" cy="5" r="1.5"/><circle cx="15" cy="5" r="1.5"/>
    <circle cx="9" cy="12" r="1.5"/><circle cx="15" cy="12" r="1.5"/>
    <circle cx="9" cy="19" r="1.5"/><circle cx="15" cy="19" r="1.5"/>
  </svg>
);
