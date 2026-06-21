import { useState, useEffect } from 'react';
import { Layers, Plus, Inbox, Search, Sparkles, LogOut, User as UserIcon } from 'lucide-react';
import './styles.css';

// We will import V2 components here later
import DashboardV2 from './DashboardV2';
import ProfileV2 from './ProfileV2';
import IngestV2 from './IngestV2';
import DeclutterV2 from './DeclutterV2';
import SearchV2 from './SearchV2';
import ContentDetailModalV2 from './ContentDetailModalV2';

export default function AppLayoutV2({ user, handleLogout }) {
  const [activeTab, setActiveTab] = useState('dashboard');
  const [selectedContentId, setSelectedContentId] = useState(null);

  const switchTab = (tab) => {
    setActiveTab(tab);
    setSelectedContentId(null);
  };

  return (
    <div className="v2-layout">
      {/* Top Glass Navigation Bar */}
      <nav className="v2-topbar">
        <div className="v2-brand">
          <h2>Refind</h2>
        </div>
        
        <div className="v2-nav-links">
          <button className={`v2-nav-btn ${activeTab === 'dashboard' ? 'active' : ''}`} onClick={() => switchTab('dashboard')}>
            <Layers size={18} /> Dashboard
          </button>
          <button className={`v2-nav-btn ${activeTab === 'ingest' ? 'active' : ''}`} onClick={() => switchTab('ingest')}>
            <Plus size={18} /> Ingest
          </button>
          <button className={`v2-nav-btn ${activeTab === 'declutter' ? 'active' : ''}`} onClick={() => switchTab('declutter')}>
            <Inbox size={18} /> Declutter
          </button>
          <button className={`v2-nav-btn ${activeTab === 'search' ? 'active' : ''}`} onClick={() => switchTab('search')}>
            <Search size={18} /> Search
          </button>
          <button className={`v2-nav-btn ${activeTab === 'profile' ? 'active' : ''}`} onClick={() => switchTab('profile')}>
            <Sparkles size={18} /> Profile
          </button>
        </div>

        <div className="v2-user-menu">
          {user && (
            <button className="v2-user-btn" onClick={handleLogout} title="Sign Out">
              <UserIcon size={18} />
            </button>
          )}
        </div>
      </nav>

      {/* Main Content Pane */}
      <main className={`v2-main-pane ${activeTab === 'search' ? 'full-bleed' : ''}`}>
        <div className={`v2-content-container ${activeTab === 'search' ? 'full-bleed' : ''}`}>
          {activeTab === 'dashboard' && <DashboardV2 onSelectContent={setSelectedContentId} />}
          {activeTab === 'profile' && <ProfileV2 user={user} onSelectContent={setSelectedContentId} />}
          {activeTab === 'ingest' && <IngestV2 />}
          {activeTab === 'declutter' && <DeclutterV2 />}
          {activeTab === 'search' && <SearchV2 onSelectContent={setSelectedContentId} />}
        </div>
      </main>

      {/* Pop-up modal details */}
      {selectedContentId && (
        <ContentDetailModalV2 
          contentId={selectedContentId} 
          onClose={() => setSelectedContentId(null)}
        />
      )}
    </div>
  );
}
