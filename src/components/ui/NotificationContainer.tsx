
import { useNotificationStore } from '../../store/notificationStore';
import type { NotificationSeverity } from '../../types';

export function NotificationContainer() {
  const { notifications, remove } = useNotificationStore();

  return (
    <div style={{
      position: 'fixed', bottom: 16, right: 16, zIndex: 600,
      display: 'flex', flexDirection: 'column', gap: 8,
      pointerEvents: 'none',
    }}>
      {notifications.map((n) => (
        <div
          key={n.id}
          style={{
            background: 'var(--bg-card)', border: `1px solid ${borderColor(n.severity)}`,
            borderLeft: `3px solid ${accentColor(n.severity)}`,
            borderRadius: 'var(--radius-md)', boxShadow: 'var(--shadow-md)',
            padding: '10px 14px', fontSize: 13, color: 'var(--text-primary)',
            display: 'flex', alignItems: 'center', gap: 10, maxWidth: 300,
            pointerEvents: 'all', cursor: 'pointer',
            animation: 'slideIn 0.15s ease',
          }}
          onClick={() => remove(n.id)}
        >
          <span style={{ fontSize: 14 }}>{icon(n.severity)}</span>
          <span style={{ flex: 1 }}>{n.message}</span>
        </div>
      ))}
    </div>
  );
}

function accentColor(s: NotificationSeverity) {
  const map: Record<NotificationSeverity, string> = {
    info: 'var(--color-info)',
    success: 'var(--color-success)',
    warning: 'var(--color-warning)',
    error: 'var(--color-error)',
  };
  return map[s];
}

function borderColor(s: NotificationSeverity) {
  const map: Record<NotificationSeverity, string> = {
    info: 'var(--color-info-bg)',
    success: 'var(--color-success-bg)',
    warning: 'var(--color-warning-bg)',
    error: 'var(--color-error-bg)',
  };
  return map[s];
}

function icon(s: NotificationSeverity) {
  return { info: 'ℹ', success: '✓', warning: '⚠', error: '✕' }[s];
}
