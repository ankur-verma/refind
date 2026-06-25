import { NavLink } from 'react-router-dom';
import { 
  Home, 
  Compass, 
  FolderHeart, 
  Search, 
  Bot, 
  UserCircle, 
  Settings,
  Film,
  Plus
} from 'lucide-react';
import { useLayoutStore } from '../stores/layoutStore';

export default function Sidebar() {
  const { toggleAssistant, theme, toggleTheme } = useLayoutStore();

  const navLinks = [
    { to: '/dashboard', icon: Home, label: 'Home' },
    { to: '/discover', icon: Compass, label: 'Discover' },
    { to: '/clips', icon: Film, label: 'Clips' },
    { to: '/collections', icon: FolderHeart, label: 'Collections' },
    { to: '/search', icon: Bot, label: 'AI Assistant' },
  ];

  const bottomLinks = [
    { to: '/profile', icon: UserCircle, label: 'Profile' },
    { to: '/settings', icon: Settings, label: 'Settings' },
  ];

  return (
    <aside className="hidden md:flex flex-col w-64 h-full border-r border-border bg-card/50 ">
      <div className="p-6 pb-2">
        <h2 className="text-2xl font-bold tracking-tight bg-gradient-to-r from-primary to-accent bg-clip-text text-transparent mb-4">
          Refind
        </h2>
        <NavLink 
          to="/ingest" 
          className="flex items-center justify-center gap-2 w-full bg-primary text-primary-foreground py-2.5 rounded-lg text-sm font-semibold hover:bg-primary/90 transition-colors shadow-sm"
        >
          <Plus size={18} /> New Save
        </NavLink>
      </div>

      <nav className="flex-1 px-4 space-y-2 overflow-y-auto">
        {navLinks.map((link) => (
          <NavLink
            key={link.to}
            to={link.to}
            className={({ isActive }) => 
              `flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors ${
                isActive 
                  ? 'bg-primary/10 text-primary' 
                  : 'text-muted-foreground hover:bg-secondary/80 hover:text-foreground'
              }`
            }
          >
            <link.icon size={20} />
            {link.label}
          </NavLink>
        ))}
      </nav>

      <div className="p-4 space-y-2 mt-auto border-t border-border/50">
        {bottomLinks.map((link) => (
          <NavLink
            key={link.to}
            to={link.to}
            className={({ isActive }) => 
              `flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                isActive 
                  ? 'bg-primary/10 text-primary' 
                  : 'text-muted-foreground hover:bg-secondary/80 hover:text-foreground'
              }`
            }
          >
            <link.icon size={18} />
            {link.label}
          </NavLink>
        ))}
      </div>
    </aside>
  );
}
