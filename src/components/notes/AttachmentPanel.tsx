import { useState } from 'react';
import type { AttachmentMeta } from '../../types';
import * as db from '../../services/db';

interface Props {

  attachments: AttachmentMeta[];
  onAdd: (file: File) => void;
  onDelete: (id: string) => void;
}

export function AttachmentPanel({ attachments, onAdd, onDelete }: Props) {
  const [open, setOpen] = useState(false);

  const handleOpen = async (att: AttachmentMeta) => {
    const data = await db.getAttachmentData(att.id);
    const bytes = new Uint8Array(data.data);
    const blob = new Blob([bytes], { type: att.mime_type });
    const url = URL.createObjectURL(blob);
    window.open(url, '_blank');
    setTimeout(() => URL.revokeObjectURL(url), 5000);
  };

  return (
    <div style={{ position: 'relative' }}>
      <button
        onClick={(e) => { e.stopPropagation(); setOpen((s) => !s); }}
        title="Attachments"
        style={{
          background: 'none', border: 'none', cursor: 'pointer', color: 'var(--text-muted)',
          padding: '3px 4px', borderRadius: 'var(--radius-sm)', display: 'flex', alignItems: 'center', gap: 3,
          fontSize: 11,
        }}
      >
        <PaperclipIcon />
        <span>{attachments.length}</span>
      </button>
      {open && (
        <div
          style={{
            position: 'absolute', top: '100%', right: 0, zIndex: 100,
            background: 'var(--bg-card)', border: '1px solid var(--border-color)',
            borderRadius: 'var(--radius-md)', boxShadow: 'var(--shadow-lg)',
            minWidth: 200, padding: '4px 0', marginTop: 4,
          }}
          onClick={(e) => e.stopPropagation()}
        >
          {attachments.map((att) => (
            <div
              key={att.id}
              style={{ display: 'flex', alignItems: 'center', padding: '6px 10px', gap: 8 }}
            >
              <span
                style={{ flex: 1, fontSize: 12, color: 'var(--text-primary)', cursor: 'pointer', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}
                onClick={() => handleOpen(att)}
                title={att.file_name}
              >
                {att.file_name}
              </span>
              <button
                onClick={() => onDelete(att.id)}
                style={{ background: 'none', border: 'none', cursor: 'pointer', color: 'var(--text-muted)', padding: 2 }}
              >×</button>
            </div>
          ))}
          <div style={{ height: 1, background: 'var(--border-subtle)', margin: '4px 0' }} />
          <button
            onClick={() => {
              const input = document.createElement('input');
              input.type = 'file';
              input.onchange = (e) => {
                const file = (e.target as HTMLInputElement).files?.[0];
                if (file) onAdd(file);
                setOpen(false);
              };
              input.click();
            }}
            style={{ width: '100%', textAlign: 'left', background: 'none', border: 'none', padding: '6px 10px', cursor: 'pointer', fontSize: 12, color: 'var(--text-secondary)' }}
          >
            + Add attachment
          </button>
        </div>
      )}
    </div>
  );
}

const PaperclipIcon = () => (
  <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
    <path d="M21.44 11.05l-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48"/>
  </svg>
);
