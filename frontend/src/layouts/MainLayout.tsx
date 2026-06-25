import { Outlet } from 'react-router-dom';
import Sidebar from './Sidebar';
import BottomNav from './BottomNav';

export default function MainLayout() {
  return (
    <div className="flex h-[100dvh] bg-background text-foreground overflow-hidden">
      {/* Desktop Left Navigation */}
      <Sidebar />

      {/* Main Content Pane */}
      <main className="flex-1 flex flex-col relative min-w-0 overflow-y-auto pb-16 md:pb-0">
        <Outlet />
      </main>

      {/* Mobile Bottom Navigation */}
      <BottomNav />
    </div>
  );
}
