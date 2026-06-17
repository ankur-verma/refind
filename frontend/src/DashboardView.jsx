import { useState } from 'react';
import { 
  Pin, Archive, Trash2, Play, Globe, FileText, 
  Camera, Zap, Clock, Search, SlidersHorizontal, RefreshCw, Check 
} from 'lucide-react';
import { request, queryClient } from './api';
import { useQuery, useMutation } from '@tanstack/react-query';

// Enum mappings from C# backend
const PLATFORM_TYPES = {
  0: { label: 'YouTube', icon: Play, color: 'text-red' },
  1: { label: 'Web', icon: Globe, color: 'text-blue' },
  2: { label: 'Instagram', icon: Camera, color: 'text-pink' },
  3: { label: 'PDF', icon: FileText, color: 'text-orange' },
  4: { label: 'TikTok', icon: Camera, color: 'text-dark' }
};

const ENERGY_LEVELS = {
  0: { label: 'Brain Dead (Low Focus)', shortLabel: 'Brain Dead', color: 'badge-blue' },
  1: { label: 'Quick Spark (Med Focus)', shortLabel: 'Quick Spark', color: 'badge-purple' },
  2: { label: 'Deep Dive (High Focus)', shortLabel: 'Deep Dive', color: 'badge-orange' }
};

const STATUS_TYPES = {
  0: { label: 'Processing', class: 'status-processing' },
  1: { label: 'Ready', class: 'status-ready' },
  2: { label: 'Failed', class: 'status-failed' }
};

export default function DashboardView({ onSelectContent }) {
  const [energyFilter, setEnergyFilter] = useState('');
  const [platformFilter, setPlatformFilter] = useState('');
  const [showArchived, setShowArchived] = useState(false);
  const [tagFilter, setTagFilter] = useState('');
  const [page, setPage] = useState(1);

  const fetchFeed = async () => {
    const params = new URLSearchParams();
    if (energyFilter !== '') params.append('EnergyLevel', energyFilter);
    if (platformFilter !== '') params.append('PlatformType', platformFilter);
    params.append('Page', page);
    params.append('PageSize', 12);
    
    const res = await request(`/content/feed?${params.toString()}`);
    return res;
  };

  const { data, isLoading, isFetching, refetch } = useQuery({
    queryKey: ['feed', { energyFilter, platformFilter, page }],
    queryFn: fetchFeed,
  });

  const feed = data?.items || [];
  const totalPages = data?.totalPages || 1;
  const filteredFeedItems = feed.filter(item => item.isArchived === showArchived);

  const pinMutation = useMutation({
    mutationFn: async ({ id, isCurrentlyPinned }) => {
      return request(`/content/${id}/declutter`, {
        method: 'POST',
        body: JSON.stringify({ action: 0 }),
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['feed'] });
    }
  });

  const archiveMutation = useMutation({
    mutationFn: async (id) => {
      return request(`/content/${id}/declutter`, {
        method: 'POST',
        body: JSON.stringify({ action: 1 }),
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['feed'] });
    }
  });

  const deleteMutation = useMutation({
    mutationFn: async (id) => {
      return request(`/content/${id}`, { method: 'DELETE' });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['feed'] });
    }
  });

  const reprocessMutation = useMutation({
    mutationFn: async (id) => {
      return request(`/content/${id}/reprocess`, { method: 'POST' });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['feed'] });
    }
  });

  const handleRefresh = () => {
    refetch();
  };

  const handlePin = (e, id, isCurrentlyPinned) => {
    e.stopPropagation();
    pinMutation.mutate({ id, isCurrentlyPinned });
  };

  const handleArchive = (e, id) => {
    e.stopPropagation();
    archiveMutation.mutate(id);
  };

  const handleDelete = (e, id) => {
    e.stopPropagation();
    if (window.confirm("Are you sure you want to permanently delete this memory?")) {
      deleteMutation.mutate(id);
    }
  };

  const handleReprocess = (e, id) => {
    e.stopPropagation();
    reprocessMutation.mutate(id);
  };

  const allTags = [...new Set(feed.flatMap(item => item.tags || []))];
  const filteredFeed = tagFilter 
    ? filteredFeedItems.filter(item => item.tags?.includes(tagFilter))
    : filteredFeedItems;

  return (
    <div className="dashboard-container">
      <div className="dashboard-header">
        <div className="view-title">
          <h1>Cognitive Memory Feed</h1>
          <p>Organize, declutter, and access your externalized digital mind.</p>
        </div>
        <button 
          className="btn btn-secondary" 
          onClick={handleRefresh} 
          data-tooltip="Refresh Feed"
          aria-label="Refresh Feed"
        >
          <RefreshCw size={18} className={isFetching ? 'spin' : ''} /> Refresh
        </button>
      </div>

      <div className="filter-toolbar glass">
        <div className="filter-section">
          <span className="filter-label"><SlidersHorizontal size={16} /> Filters:</span>
          
          <select 
            value={platformFilter} 
            onChange={(e) => { setPlatformFilter(e.target.value); setPage(1); }}
            className="filter-select"
          >
            <option value="">All Source Platforms</option>
            <option value="0">YouTube</option>
            <option value="1">Web Articles</option>
            <option value="2">Instagram</option>
            <option value="3">PDF Document</option>
            <option value="4">TikTok</option>
          </select>

          <select 
            value={energyFilter} 
            onChange={(e) => { setEnergyFilter(e.target.value); setPage(1); }}
            className="filter-select"
          >
            <option value="">All Energy Levels</option>
            <option value="0">Brain Dead (Low Energy)</option>
            <option value="1">Quick Spark (Medium Energy)</option>
            <option value="2">Deep Dive (High Focus)</option>
          </select>
          
          <button 
            className={`btn-toggle ${showArchived ? 'active' : ''}`}
            onClick={() => { setShowArchived(!showArchived); setPage(1); }}
          >
            Show Archived
          </button>
        </div>

        {allTags.length > 0 && (
          <div className="tags-filter">
            <span className="tags-label">Quick Tag:</span>
            <button 
              className={`tag-chip-btn ${tagFilter === '' ? 'active' : ''}`}
              onClick={() => setTagFilter('')}
            >
              All
            </button>
            {allTags.map(tag => (
              <button 
                key={tag}
                className={`tag-chip-btn ${tagFilter === tag ? 'active' : ''}`}
                onClick={() => setTagFilter(tag)}
              >
                #{tag}
              </button>
            ))}
          </div>
        )}
      </div>

      {isLoading ? (
        <div className="skeleton-grid animate-fade-in" aria-live="polite" aria-label="Loading memories">
          {[...Array(6)].map((_, i) => (
            <div key={i} className="skeleton-card">
              <div className="skeleton-img"></div>
              <div className="skeleton-content">
                <div className="skeleton-line short"></div>
                <div className="skeleton-line long"></div>
                <div className="skeleton-line medium"></div>
              </div>
            </div>
          ))}
        </div>
      ) : filteredFeed.length === 0 ? (
        <div className="empty-state glass">
          <Zap size={64} className="empty-icon" />
          <h3>No memories in this view</h3>
          <p>
            {tagFilter || platformFilter || energyFilter
              ? 'Try clearing your active filters to find your items.'
              : 'Start by pasting links in the Ingestion Hub to build your externalized brain.'}
          </p>
        </div>
      ) : (
        <>
          <div className="memory-grid">
            {filteredFeed.map(item => {
              const platform = PLATFORM_TYPES[item.platformType] || { label: 'Web', icon: Globe, color: 'text-blue' };
              const PlatformIcon = platform.icon;
              const energy = ENERGY_LEVELS[item.energyLevel] || { label: 'Quick Spark', color: 'badge-purple' };
              const status = STATUS_TYPES[item.status] || { label: 'Processing', class: 'status-processing' };

              return (
                <div 
                  key={item.id} 
                  className={`memory-card glass ${item.isPinned ? 'pinned' : ''}`}
                  onClick={() => onSelectContent(item.id)}
                  tabIndex="0"
                  role="button"
                  aria-label={`Open memory: ${item.title || item.originalUrl}`}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      e.preventDefault();
                      onSelectContent(item.id);
                    }
                  }}
                >
                  <div className="card-thumb">
                    {item.heroImageUrl ? (
                      <img src={item.heroImageUrl} alt={item.title} />
                    ) : (
                      <div className="thumb-placeholder">
                        <PlatformIcon size={48} className="icon-placeholder" />
                      </div>
                    )}
                    
                    <div className="card-platform-badge">
                      <PlatformIcon size={16} />
                      <span>{platform.label}</span>
                    </div>

                    {item.isPinned && (
                      <div className="pinned-badge">
                        <Pin size={14} /> Pinned
                      </div>
                    )}
                  </div>

                  <div className="card-body">
                    <div className="card-meta">
                      <span className={`badge ${energy.color}`}>{energy.shortLabel}</span>
                      <span className="consume-time">
                        <Clock size={14} /> {item.consumeTimeMins} min
                      </span>
                    </div>

                    <h4 className="card-title" title={item.title}>
                      {item.title || item.originalUrl}
                    </h4>

                    {item.tags && item.tags.length > 0 && (
                      <div className="card-tags">
                        {item.tags.slice(0, 3).map(t => (
                          <span key={t} className="tag-pill">#{t}</span>
                        ))}
                      </div>
                    )}
                  </div>

                  <div className="card-footer">
                    <span className={`status-dot ${status.class}`} title={`Status: ${status.label}`}>
                      {item.status === 0 && <RefreshCw size={12} className="spin" />}
                      {status.label}
                    </span>

                    <div className="card-actions">
                      <button 
                        className={`action-btn pin ${item.isPinned ? 'active' : ''}`} 
                        onClick={(e) => handlePin(e, item.id, item.isPinned)}
                        data-tooltip={item.isPinned ? "Unpin Content" : "Pin Content"}
                        aria-label={item.isPinned ? "Unpin Content" : "Pin Content"}
                      >
                        <Pin size={16} />
                      </button>
                      <button 
                        className="action-btn archive" 
                        onClick={(e) => handleArchive(e, item.id)}
                        data-tooltip="Archive Content"
                        aria-label="Archive Content"
                      >
                        <Archive size={16} />
                      </button>
                      <button 
                        className="action-btn reprocess" 
                        onClick={(e) => handleReprocess(e, item.id)}
                        data-tooltip="Reprocess Content"
                        aria-label="Reprocess Content"
                        disabled={item.status === 0}
                      >
                        <RefreshCw size={16} className={item.status === 0 ? "spin" : ""} />
                      </button>
                      <button 
                        className="action-btn delete" 
                        onClick={(e) => handleDelete(e, item.id)}
                        data-tooltip="Delete Content"
                        aria-label="Delete Content"
                      >
                        <Trash2 size={16} />
                      </button>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>

          {totalPages > 1 && (
            <div className="pagination">
              <button 
                className="btn btn-secondary-sm" 
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
              >
                Previous
              </button>
              <span className="page-indicator">Page {page} of {totalPages}</span>
              <button 
                className="btn btn-secondary-sm" 
                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                disabled={page === totalPages}
              >
                Next
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
