import { useState, useEffect } from 'react';
import { 
  Database, Inbox, Layers, Search, 
  LogOut, User as UserIcon, Plus
} from 'lucide-react';
import AuthView from './AuthView';
import { setupSignalR, stopSignalR } from './api';
import DashboardView from './DashboardView';
import BulkIngestView from './BulkIngestView';
import DeclutterView from './DeclutterView';
import SemanticSearchView from './SemanticSearchView';
import ContentDetailModal from './ContentDetailModal';

export default function App() {
  const [token, setToken] = useState(localStorage.getItem('token'));
  const [user, setUser] = useState(null);
  const [activeTab, setActiveTab] = useState('dashboard');
  const [selectedContentId, setSelectedContentId] = useState(null);

  useEffect(() => {
    // Parse user details from storage if token exists
    const savedUser = localStorage.getItem('user');
    if (token && savedUser) {
      try {
        setUser(JSON.parse(savedUser));
      } catch (_) {
        setUser({ email: 'user@example.com', tier: 0 });
      }
    } else {
      setUser(null);
      stopSignalR();
    }
  }, [token]);

  useEffect(() => {
    if (token) {
      setupSignalR();
    } else {
      stopSignalR();
    }
  }, [token]);

  // Listen for API helper firing logouts
  useEffect(() => {
    const handleAuthChange = () => {
      setToken(localStorage.getItem('token'));
    };
    window.addEventListener('auth-change', handleAuthChange);
    return () => window.removeEventListener('auth-change', handleAuthChange);
  }, []);

  const handleAuthSuccess = () => {
    setToken(localStorage.getItem('token'));
    setActiveTab('dashboard');
  };

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    setToken(null);
    setUser(null);
  };

  // If not authenticated, force AuthView
  if (!token) {
    return <AuthView onAuthSuccess={handleAuthSuccess} />;
  }

  return (
    <div className="app-layout">
      {/* Sidebar Navigation */}
      <aside className="app-sidebar glass">
        <div className="brand-logo">
          <Database className="logo-icon animate-pulse" />
          <h2>Refind</h2>
          <span className="logo-tagline">Cognitive Brain</span>
        </div>

        <nav className="nav-menu" aria-label="Main Navigation">
          <button 
            className={`nav-item ${activeTab === 'dashboard' ? 'active' : ''}`}
            onClick={() => { setActiveTab('dashboard'); setSelectedContentId(null); }}
            aria-label="View Memory Feed"
            aria-current={activeTab === 'dashboard' ? 'page' : undefined}
          >
            <Layers size={18} />
            <span>Memory Feed</span>
          </button>
          
          <button 
            className={`nav-item ${activeTab === 'ingest' ? 'active' : ''}`}
            onClick={() => { setActiveTab('ingest'); setSelectedContentId(null); }}
            aria-label="Open Ingestion Hub"
            aria-current={activeTab === 'ingest' ? 'page' : undefined}
          >
            <Plus size={18} />
            <span>Ingestion Hub</span>
          </button>
          
          <button 
            className={`nav-item ${activeTab === 'declutter' ? 'active' : ''}`}
            onClick={() => { setActiveTab('declutter'); setSelectedContentId(null); }}
            aria-label="Swipe to Clean Memories"
            aria-current={activeTab === 'declutter' ? 'page' : undefined}
          >
            <Inbox size={18} />
            <span>Swipe to Clean</span>
          </button>
          
          <button 
            className={`nav-item ${activeTab === 'search' ? 'active' : ''}`}
            onClick={() => { setActiveTab('search'); setSelectedContentId(null); }}
            aria-label="Semantic Search"
            aria-current={activeTab === 'search' ? 'page' : undefined}
          >
            <Search size={18} />
            <span>Semantic Search</span>
          </button>
        </nav>

        {/* User profile segment */}
        {user && (
          <div className="sidebar-profile">
            <div className="profile-details">
              <UserIcon size={18} className="text-gray" />
              <div className="profile-text">
                <span className="profile-email" data-tooltip={user.email} aria-label={`Logged in as ${user.email}`}>{user.email}</span>
                <span className="badge badge-purple-sm">
                  {user.tier === 0 ? 'Free Tier' : 'Premium Tier'}
                </span>
              </div>
            </div>
            
            <button 
              className="btn-logout" 
              onClick={handleLogout} 
              data-tooltip="Sign Out"
              aria-label="Sign Out"
            >
              <LogOut size={16} /> Sign Out
            </button>
          </div>
        )}
      </aside>

      {/* Main Panel Content Area */}
      <main className="app-main">
        {activeTab === 'dashboard' && (
          <DashboardView onSelectContent={(id) => setSelectedContentId(id)} />
        )}
        {activeTab === 'ingest' && (
          <BulkIngestView />
        )}
        {activeTab === 'declutter' && (
          <DeclutterView />
        )}
        {activeTab === 'search' && (
          <SemanticSearchView onSelectContent={(id) => setSelectedContentId(id)} />
        )}
      </main>

      {/* Pop-up modal details */}
      {selectedContentId && (
        <ContentDetailModal 
          contentId={selectedContentId} 
          onClose={() => setSelectedContentId(null)}
        />
      )}
    </div>
  );
}
