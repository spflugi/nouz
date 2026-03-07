
import { LeftSidebar } from './LeftSidebar';
import { RightSidebar } from './RightSidebar';
import { Topbar } from './Topbar';
import { NoteTimeline } from '../notes/NoteTimeline';
import { NotificationContainer } from '../ui/NotificationContainer';

export function MainLayout() {
  return (
    <div style={{ display: 'flex', height: '100vh', overflow: 'hidden', background: 'var(--bg-primary)' }}>
      <LeftSidebar />
      <div style={{ flex: 1, display: 'flex', flexDirection: 'column', minWidth: 0, overflow: 'hidden' }}>
        <Topbar />
        <main style={{ flex: 1, overflow: 'hidden' }}>
          <NoteTimeline />
        </main>
      </div>
      <RightSidebar />
      <NotificationContainer />
    </div>
  );
}
