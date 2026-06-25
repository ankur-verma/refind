import { useState, useEffect } from 'react';
import { Compass, Target, FolderHeart, Zap, Play, X, Activity, RefreshCw } from 'lucide-react';
import { request } from '../../api';
import QuickBoostReel from '../watch-plans/QuickBoostReel';

export default function ProfileV2({ user }) {
  const [profile, setProfile] = useState(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [error, setError] = useState(null);
  const [activeWatchPlan, setActiveWatchPlan] = useState(null);

  const fetchProfile = async () => {
    setLoading(true);
    setError(null);
    try {
      const pRes = await request('/interaction/profile');
      if (pRes.success) {
        setProfile(pRes.data);
      } else {
        setError(pRes.errors?.[0] || 'Failed to load profile');
      }
    } catch (err) {
      console.error(err);
      setError('An error occurred while loading profile data.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchProfile();
  }, []);

  const handleRefresh = async () => {
    setRefreshing(true);
    try {
      const refreshRes = await request('/interaction/profile/refresh', { method: 'POST' });
      if (refreshRes.success && refreshRes.data) {
        setProfile(refreshRes.data);
      } else {
        alert(refreshRes.errors?.[0] || 'Failed to refresh profile.');
      }
    } catch (err) {
      console.error(err);
      alert('Error communicating with AI service.');
    } finally {
      setRefreshing(false);
    }
  };

  if (loading) {
    return (
      <div className="v2-profile v2-loading-view">
        <Activity size={32} className="v2-pulse-icon" />
        <p>Synthesizing Watch Plans...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="v2-profile v2-loading-view">
        <Compass size={40} className="v2-pulse-icon v2-text-red" />
        <h2>Intelligence unavailable</h2>
        <p>{error}</p>
        <button className="v2-btn-secondary" onClick={fetchProfile} style={{ marginTop: '16px' }}>
          <RefreshCw size={16} /> Try again
        </button>
      </div>
    );
  }

  const intents = profile?.intents || [];
  const collections = profile?.autoCollections || [];

  // Generate Watch Plan cards from Intents and Collections
  const watchPlans = [];
  
  intents.forEach(intent => {
    watchPlans.push({
      id: `intent-${intent.id || intent.goalDescription}`,
      title: intent.goalDescription,
      type: 'Goal',
      icon: <Target size={24} />,
      colorClass: 'v2-bg-orange'
    });
  });

  collections.forEach(col => {
    watchPlans.push({
      id: `col-${col.id || col.name}`,
      title: col.name,
      type: 'Collection',
      icon: <FolderHeart size={24} />,
      colorClass: 'v2-bg-purple'
    });
  });

  if (watchPlans.length === 0) {
    watchPlans.push({
      id: 'default-discovery',
      title: 'General Discovery',
      type: 'Interest',
      icon: <Compass size={24} />,
      colorClass: 'v2-bg-blue'
    });
  }

  return (
    <div className="v2-profile">
      <header className="v2-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
        <div>
          <h1 className="v2-huge-title">Watch Plans</h1>
          <p className="v2-subtitle">Interactive reels curated dynamically from your knowledge base.</p>
        </div>
        <button 
          className="v2-btn-secondary" 
          onClick={handleRefresh} 
          disabled={refreshing}
          style={{ display: 'flex', alignItems: 'center', gap: '8px' }}
        >
          <RefreshCw size={16} className={refreshing ? 'v2-spin' : ''} />
          {refreshing ? 'Analyzing...' : 'Force Sync'}
        </button>
      </header>

      {/* Watch Plans Feed */}
      {!activeWatchPlan ? (
        <div className="v2-watch-plans-grid" style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', gap: '24px', marginTop: '32px' }}>
          {watchPlans.map((plan) => (
            <div 
              key={plan.id} 
              className="v2-watch-plan-card glass"
              onClick={() => setActiveWatchPlan(plan)}
              style={{ padding: '24px', cursor: 'pointer', borderRadius: '16px', display: 'flex', flexDirection: 'column', gap: '16px', transition: 'all 0.2s ease', position: 'relative', overflow: 'hidden' }}
              onMouseOver={(e) => e.currentTarget.style.transform = 'translateY(-4px)'}
              onMouseOut={(e) => e.currentTarget.style.transform = 'translateY(0)'}
            >
              <div className={`v2-row-icon ${plan.colorClass}`} style={{ width: '48px', height: '48px', display: 'flex', alignItems: 'center', justifyContent: 'center', borderRadius: '12px' }}>
                {plan.icon}
              </div>
              <div>
                <span style={{ fontSize: '12px', textTransform: 'uppercase', letterSpacing: '1px', opacity: 0.7, fontWeight: 600 }}>{plan.type} Mix</span>
                <h3 style={{ margin: '8px 0 0 0', fontSize: '20px', lineHeight: 1.3 }}>{plan.title}</h3>
              </div>
              <div style={{ marginTop: 'auto', display: 'flex', alignItems: 'center', gap: '8px', color: '#007aff', fontWeight: 500 }}>
                <Play size={16} fill="currentColor" /> Play Reel
              </div>
            </div>
          ))}
        </div>
      ) : (
        /* Active Watch Plan Player Overlay - Mobile Centric View */
        <div className="v2-active-watch-plan animate-fade-in" style={{ 
          marginTop: '24px', 
          position: 'relative', 
          background: 'var(--surface)', 
          borderRadius: '24px', 
          overflow: 'hidden', 
          border: '1px solid var(--border)', 
          boxShadow: '0 20px 40px rgba(0,0,0,0.2)',
          maxWidth: '450px',
          margin: '24px auto', /* Center the mobile container */
          aspectRatio: '9/16',
          display: 'flex',
          flexDirection: 'column'
        }}>
          <div style={{ padding: '16px 24px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', borderBottom: '1px solid var(--border)', background: 'rgba(0,0,0,0.8)', color: '#fff', zIndex: 10 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
              <div className={`v2-row-icon ${activeWatchPlan.colorClass}`}>
                {activeWatchPlan.icon}
              </div>
              <div>
                <h2 style={{ margin: 0, fontSize: '18px' }}>{activeWatchPlan.title}</h2>
                <span style={{ fontSize: '12px', opacity: 0.7 }}>AI Curated Reel</span>
              </div>
            </div>
            <button 
              onClick={() => setActiveWatchPlan(null)}
              style={{ background: 'rgba(255,255,255,0.2)', border: 'none', borderRadius: '50%', width: '36px', height: '36px', display: 'flex', alignItems: 'center', justifyContent: 'center', cursor: 'pointer', color: '#fff' }}
            >
              <X size={20} />
            </button>
          </div>
          <div style={{ flex: 1, background: '#000', position: 'relative', display: 'flex', flexDirection: 'column', overflow: 'hidden' }}>
            <QuickBoostReel userId={user?.userId} topic={activeWatchPlan.title} />
          </div>
        </div>
      )}
    </div>
  );
}
