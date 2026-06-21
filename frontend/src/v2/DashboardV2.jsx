import { useState } from 'react';
import { Play, Globe, Camera, FileText, Zap, Archive, Check, Clock, RefreshCw } from 'lucide-react';
import { request, queryClient } from '../api';
import { useQuery, useMutation } from '@tanstack/react-query';

const PLATFORM_TYPES = {
  0: { label: 'YouTube', icon: Play, color: 'text-red' },
  1: { label: 'Web', icon: Globe, color: 'text-blue' },
  2: { label: 'Instagram', icon: Camera, color: 'text-pink' },
  3: { label: 'PDF', icon: FileText, color: 'text-orange' },
  4: { label: 'TikTok', icon: Camera, color: 'text-dark' }
};

const ENERGY_LEVELS = {
  0: { label: 'Brain Dead', class: 'v2-energy-low' },
  1: { label: 'Quick Spark', class: 'v2-energy-med' },
  2: { label: 'Deep Dive', class: 'v2-energy-high' }
};

export default function DashboardV2({ onSelectContent }) {
  const [energyFilter, setEnergyFilter] = useState('');
  const [platformFilter, setPlatformFilter] = useState('');
  const [showArchived, setShowArchived] = useState(false);
  const [page, setPage] = useState(1);

  const fetchFeed = async () => {
    const params = new URLSearchParams();
    if (energyFilter !== '') params.append('EnergyLevel', energyFilter);
    if (platformFilter !== '') params.append('PlatformType', platformFilter);
    params.append('Page', page);
    params.append('PageSize', 12);
    return await request(`/content/feed?${params.toString()}`);
  };

  const { data, isLoading } = useQuery({
    queryKey: ['feedV2', { energyFilter, platformFilter, page }],
    queryFn: fetchFeed,
  });

  const feed = data?.items || [];
  const totalPages = data?.totalPages || 1;
  const filteredFeed = feed.filter(item => item.isArchived === showArchived);

  return (
    <div className="v2-dashboard">
      <header className="v2-header">
        <h1 className="v2-huge-title">Memory Feed</h1>
        <p className="v2-subtitle">Your externalized brain, elegantly organized.</p>
      </header>

      {/* Apple-style minimalist filter pills */}
      <div className="v2-filters">
        <div className="v2-filter-group">
          <span className="v2-filter-label">Source</span>
          <button className={`v2-pill ${platformFilter === '' ? 'active' : ''}`} onClick={() => setPlatformFilter('')}>All</button>
          <button className={`v2-pill ${platformFilter === '1' ? 'active' : ''}`} onClick={() => setPlatformFilter('1')}>Web</button>
          <button className={`v2-pill ${platformFilter === '0' ? 'active' : ''}`} onClick={() => setPlatformFilter('0')}>YouTube</button>
        </div>
        
        <div className="v2-filter-group">
          <span className="v2-filter-label">Energy</span>
          <button className={`v2-pill ${energyFilter === '' ? 'active' : ''}`} onClick={() => setEnergyFilter('')}>All</button>
          <button className={`v2-pill ${energyFilter === '0' ? 'active' : ''}`} onClick={() => setEnergyFilter('0')}>Low</button>
          <button className={`v2-pill ${energyFilter === '2' ? 'active' : ''}`} onClick={() => setEnergyFilter('2')}>High</button>
        </div>
      </div>

      {isLoading ? (
        <div className="v2-loading">Loading memories...</div>
      ) : filteredFeed.length === 0 ? (
        <div className="v2-empty">
          <Zap size={48} />
          <p>No memories found</p>
        </div>
      ) : (
        <div className="v2-grid">
          {filteredFeed.map(item => {
            const platform = PLATFORM_TYPES[item.platformType] || PLATFORM_TYPES[1];
            const PlatformIcon = platform.icon;
            const energy = ENERGY_LEVELS[item.energyLevel] || ENERGY_LEVELS[1];

            return (
              <div 
                key={item.id} 
                className="v2-card"
                onClick={() => onSelectContent(item.id)}
              >
                <div className="v2-card-image-wrap">
                  {item.heroImageUrl ? (
                    <img src={item.heroImageUrl} alt={item.title} className="v2-card-img" />
                  ) : (
                    <div className="v2-card-img-placeholder">
                      <PlatformIcon size={32} />
                    </div>
                  )}
                  <div className="v2-card-badge">
                    <PlatformIcon size={12} /> {platform.label}
                  </div>
                </div>
                <div className="v2-card-content">
                  <div className="v2-card-meta">
                    <span className={`v2-energy-dot ${energy.class}`}></span>
                    <span>{item.consumeTimeMins} min read</span>
                  </div>
                  <h3 className="v2-card-title">{item.title || item.originalUrl}</h3>
                  <p className="v2-card-desc">{item.summary || 'Tap to explore this memory in detail.'}</p>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
