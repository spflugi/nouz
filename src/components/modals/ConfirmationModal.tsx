import React from 'react';

interface Props {
  open: boolean;
  message: string;
  onConfirm: () => void;
  onCancel: () => void;
}

export function ConfirmationModal({ open, message, onConfirm, onCancel }: Props) {
  if (!open) return null;
  return (
    <Overlay onClose={onCancel}>
      <div style={{ padding: '20px 24px', maxWidth: 360 }}>
        <p style={{ fontSize: 14, color: 'var(--text-primary)', lineHeight: 1.5 }}>{message}</p>
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8, marginTop: 16 }}>
          <DialogButton onClick={onCancel}>Cancel</DialogButton>
          <DialogButton primary onClick={onConfirm}>Confirm</DialogButton>
        </div>
      </div>
    </Overlay>
  );
}

export function Overlay({ children, onClose }: { children: React.ReactNode; onClose: () => void }) {
  return (
    <div
      onClick={onClose}
      style={{
        position: 'fixed', inset: 0, zIndex: 400,
        background: 'rgba(0,0,0,0.3)', display: 'flex', alignItems: 'center', justifyContent: 'center',
      }}
    >
      <div
        onClick={(e) => e.stopPropagation()}
        style={{
          background: 'var(--bg-card)', borderRadius: 'var(--radius-lg)',
          boxShadow: 'var(--shadow-lg)', border: '1px solid var(--border-color)',
          minWidth: 280,
        }}
      >
        {children}
      </div>
    </div>
  );
}

export function DialogButton({ children, onClick, primary }: { children: React.ReactNode; onClick: () => void; primary?: boolean }) {
  return (
    <button
      onClick={onClick}
      style={{
        padding: '6px 16px', fontSize: 13, borderRadius: 'var(--radius-md)',
        border: '1px solid var(--border-color)', cursor: 'pointer',
        background: primary ? 'var(--accent)' : 'none',
        color: primary ? 'var(--bg-primary)' : 'var(--text-primary)',
      }}
    >
      {children}
    </button>
  );
}
