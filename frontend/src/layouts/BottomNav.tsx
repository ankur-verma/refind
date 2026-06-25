import { NavLink, useNavigate } from 'react-router-dom';
import { Home, Compass, Plus, Film, UserCircle, Brain } from 'lucide-react';

export default function BottomNav() {
  const navigate = useNavigate();

  const navLinksLeft = [
    { to: '/dashboard', icon: Home, label: 'Home' },
    { to: '/discover', icon: Compass, label: 'Discover' },
  ];

  const navLinksRight = [
    { to: '/search', icon: Brain, label: 'Assistant' },
    { to: '/profile', icon: UserCircle, label: 'Profile' },
  ];

  return (
    <nav className="md:hidden fixed bottom-0 left-0 right-0 h-16 bg-card/90 border-t border-border z-50 px-4 pb-safe flex items-center justify-between">
      
      {/* Left Links */}
      <div className="flex flex-1 justify-around h-full">
        {navLinksLeft.map((link) => (
          <NavLink
            key={link.to}
            to={link.to}
            className={({ isActive }) => 
              `flex flex-col items-center justify-center w-full h-full space-y-1 transition-colors ${
                isActive ? 'text-primary' : 'text-muted-foreground hover:text-foreground'
              }`
            }
          >
            <link.icon size={22} />
            <span className="text-[10px] font-medium">{link.label}</span>
          </NavLink>
        ))}
      </div>

      {/* Elevated Center Add Button for Fast Save */}
      <div className="flex-shrink-0 relative -top-6 mx-2">
        <button 
          onClick={() => navigate('/ingest')}
          className="w-14 h-14 bg-primary text-primary-foreground rounded-full shadow-xl shadow-primary/30 flex flex-col items-center justify-center active:scale-95 transition-transform border-4 border-background"
        >
          <Plus size={28} strokeWidth={3} />
        </button>
      </div>

      {/* Right Links */}
      <div className="flex flex-1 justify-around h-full">
        {navLinksRight.map((link) => (
          <NavLink
            key={link.to}
            to={link.to}
            className={({ isActive }) => 
              `flex flex-col items-center justify-center w-full h-full space-y-1 transition-colors ${
                isActive ? 'text-primary' : 'text-muted-foreground hover:text-foreground'
              }`
            }
          >
            <link.icon size={22} />
            <span className="text-[10px] font-medium">{link.label}</span>
          </NavLink>
        ))}
      </div>

    </nav>
  );
}
