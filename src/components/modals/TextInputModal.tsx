import React, { useEffect, useRef, useState } from 'react';
import { Overlay, DialogButton } from './ConfirmationModal';

interface Props {
  open: boolean;
  title: string;
  placeholder?: string;
  initialValue?: string;
  onConfirm: (value: string) => void;
  onCancel: () => void;
}

export function TextInputModal({ open, title, placeholder, initialValue = '', onConfirm, onCancel }: Props) {
  const [value, setValue] = useState(initialValue);
  const inputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (open) {
      setValue(initialValue);
      setTimeout(() => inputRef.current?.select(), 50);
    }
  }, [open, initialValue]);

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') onConfirm(value);
    if (e.key === 'Escape') onCancel();
  };

  if (!open) return null;
  return (
    <Overlay onClose={onCancel}>
      <div style={{ padding: '20px 24px', minWidth: 300 }}>
        <p style={{ fontSize: 13, fontWeight: 600, color: 'var(--text-secondary)', marginBottom: 10 }}>{title}</p>
        <input
          ref={inputRef}
          type="text"
          value={value}
          onChange={(e) => setValue(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder={placeholder}
          className="selectable"
          style={{
            width: '100%', padding: '7px 10px', fontSize: 13,
            border: '1px solid var(--border-color)', borderRadius: 'var(--radius-md)',
            background: 'var(--bg-secondary)', color: 'var(--text-primary)', outline: 'none',
          }}
        />
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8, marginTop: 12 }}>
          <DialogButton onClick={onCancel}>Cancel</DialogButton>
          <DialogButton primary onClick={() => onConfirm(value)}>Save</DialogButton>
        </div>
      </div>
    </Overlay>
  );
}
