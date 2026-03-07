import React, { useEffect, useRef, useState } from 'react';
import type { Block, ImageMetadata } from '../../../types';
import { parseMetadata, stringifyMetadata } from '../../../utils/blocks';
import * as db from '../../../services/db';

interface Props {
  block: Block;
  
  isEditing: boolean;
  onContentChange: (updated: Block) => void;
}

export function ImageBlock({ block, isEditing, onContentChange }: Props) {
  const meta = parseMetadata<ImageMetadata>(block.metadata);
  const [dataUrl, setDataUrl] = useState<string | null>(meta.data_url ?? null);
  const [caption, setCaption] = useState(meta.caption ?? '');
  const [width, setWidth] = useState(meta.width_percent ?? 100);
  const [showPreview, setShowPreview] = useState(false);
  const resizeRef = useRef<HTMLDivElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    // Load from attachment if stored there
    if (!dataUrl && meta.attachment_id) {
      db.getAttachmentData(meta.attachment_id).then((att) => {
        const bytes = new Uint8Array(att.data);
        const blob = new Blob([bytes], { type: att.mime_type });
        const reader = new FileReader();
        reader.onload = () => setDataUrl(reader.result as string);
        reader.readAsDataURL(blob);
      }).catch(() => {});
    }
  }, []);

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = () => {
      const url = reader.result as string;
      setDataUrl(url);
      onContentChange({
        ...block,
        metadata: stringifyMetadata({ ...meta, data_url: url, caption, width_percent: width }),
      });
    };
    reader.readAsDataURL(file);
  };

  const updateMeta = (updates: Partial<ImageMetadata>) => {
    const next = { ...meta, ...updates };
    onContentChange({ ...block, metadata: stringifyMetadata(next as any) });
  };

  if (!dataUrl) {
    return (
      <div
        style={{
          display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center',
          height: 100, border: '2px dashed var(--border-color)', borderRadius: 'var(--radius-md)',
          color: 'var(--text-muted)', cursor: isEditing ? 'pointer' : 'default', gap: 8,
        }}
        onClick={() => isEditing && fileInputRef.current?.click()}
        onDrop={(e) => {
          e.preventDefault();
          if (!isEditing) return;
          const file = e.dataTransfer.files[0];
          if (file?.type.startsWith('image/')) {
            const input = fileInputRef.current;
            if (input) {
              const dt = new DataTransfer();
              dt.items.add(file);
              input.files = dt.files;
              handleFileSelect({ target: input } as any);
            }
          }
        }}
        onDragOver={(e) => e.preventDefault()}
      >
        <input ref={fileInputRef} type="file" accept="image/*" style={{ display: 'none' }} onChange={handleFileSelect} />
        <ImageIcon />
        <span style={{ fontSize: 12 }}>{isEditing ? 'Click or drop to add image' : 'Image'}</span>
      </div>
    );
  }

  return (
    <>
      <div style={{ textAlign: 'center' }}>
        <div ref={containerRef} style={{ display: 'inline-block', width: `${width}%`, position: 'relative' }}>
          <img
            src={dataUrl}
            alt={caption}
            title="Click to preview"
            onClick={() => setShowPreview(true)}
            style={{ width: '100%', borderRadius: 'var(--radius-md)', cursor: 'zoom-in', display: 'block' }}
          />
          {isEditing && (
            <div
              ref={resizeRef}
              onMouseDown={(e) => {
                e.preventDefault();
                const startX = e.clientX;
                const startW = width;
                let currentW = startW;
                const container = containerRef.current?.parentElement;
                const parentW = container?.offsetWidth ?? 1;
                const onMove = (mv: MouseEvent) => {
                  const delta = mv.clientX - startX;
                  currentW = Math.round(Math.min(100, Math.max(20, startW + (delta / parentW * 100))));
                  setWidth(currentW);
                };
                const onUp = () => {
                  window.removeEventListener('mousemove', onMove);
                  window.removeEventListener('mouseup', onUp);
                  updateMeta({ width_percent: currentW, caption, data_url: dataUrl });
                };
                window.addEventListener('mousemove', onMove);
                window.addEventListener('mouseup', onUp);
              }}
              style={{
                position: 'absolute', bottom: 4, right: -6,
                width: 12, height: 24, background: 'var(--accent)', borderRadius: 3,
                cursor: 'ew-resize', opacity: 0.6,
              }}
            />
          )}
        </div>
        <input
          type="text"
          value={caption}
          onChange={(e) => setCaption(e.target.value)}
          onBlur={() => updateMeta({ caption, data_url: dataUrl, width_percent: width })}
          placeholder="Add a caption..."
          readOnly={!isEditing}
          style={{
            display: 'block', margin: '4px auto 0',
            width: `${width}%`, textAlign: 'center',
            background: 'none', border: 'none', outline: 'none',
            fontSize: 11, color: 'var(--text-muted)',
            cursor: isEditing ? 'text' : 'default',
          }}
          className="selectable"
        />
      </div>

      {showPreview && (
        <div
          onClick={() => setShowPreview(false)}
          style={{
            position: 'fixed', inset: 0, zIndex: 500,
            background: 'rgba(0,0,0,0.8)', display: 'flex', alignItems: 'center', justifyContent: 'center',
            cursor: 'zoom-out',
          }}
        >
          <img src={dataUrl} alt={caption} style={{ maxWidth: '90vw', maxHeight: '90vh', borderRadius: 'var(--radius-lg)' }} />
        </div>
      )}
    </>
  );
}

const ImageIcon = () => (
  <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
    <rect x="3" y="3" width="18" height="18" rx="2"/>
    <circle cx="8.5" cy="8.5" r="1.5"/>
    <polyline points="21 15 16 10 5 21"/>
  </svg>
);
