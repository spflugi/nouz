import { create } from 'zustand';
import type { Notification, NotificationSeverity } from '../types';
import { v4 as uuidv4 } from '../utils/uuid';

interface NotificationState {
  notifications: Notification[];
  add: (message: string, severity?: NotificationSeverity) => void;
  remove: (id: string) => void;
}

export const useNotificationStore = create<NotificationState>((set) => ({
  notifications: [],

  add: (message, severity = 'info') => {
    const id = uuidv4();
    set((s) => ({
      notifications: [...s.notifications, { id, message, severity }],
    }));
    setTimeout(() => {
      set((s) => ({ notifications: s.notifications.filter((n) => n.id !== id) }));
    }, 4000);
  },

  remove: (id) =>
    set((s) => ({ notifications: s.notifications.filter((n) => n.id !== id) })),
}));
