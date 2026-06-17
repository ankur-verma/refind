import { useState, useEffect } from 'react';
import { Pin, Archive, Trash2, Sparkles, RefreshCw, Check, ArrowRight } from 'lucide-react';
import { request } from './api';

export default function DeclutterView() {
  const [queue, setQueue] = useState([]);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [loading, setLoading] = useState(true);
  const [lastAction, setLastAction] = useState('');

  const fetchQueue = async () => {
    setLoading(true);
    // Fetch first 30 active, unarchived, unpinned items to declutter
    const res = await request('/content/feed?Page=1&PageSize=30');
    if (res && res.items) {
      // Find items that are active and not archived or pinned yet
      const unorganized = res.items.filter(item => !item.isArchived && !item.isPinned);
      setQueue(unorganized);
      setCurrentIndex(0);
    }
    setLoading(false);
  };

  useEffect(() => {
    fetchQueue();
  }, []);

  const performAction = async (actionType) => {
    if (currentIndex >= queue.length) return;
    const currentItem = queue[currentIndex];
    
    let success = false;
    if (actionType === 'pin') {
      const res = await request(`/content/${currentItem.id}/declutter`, {
        method: 'POST',
        body: JSON.stringify({ action: 0 }), // Pin
      });
      success = res.success;
      setLastAction(`Pinned "${currentItem.title}"`);
    } else if (actionType === 'archive') {
      const res = await request(`/content/${currentItem.id}/declutter`, {
        method: 'POST',
        body: JSON.stringify({ action: 1 }), // Archive
      });
      success = res.success;
      setLastAction(`Archived "${currentItem.title}"`);
    } else if (actionType === 'delete') {
      const res = await request(`/content/${currentItem.id}`, {
        method: 'DELETE',
      });
      success = res.success;
      setLastAction(`Deleted "${currentItem.title}"`);
    }

    if (success) {
      // Advance to next index
      setCurrentIndex(idx => idx + 1);
    } else {
      alert('Declutter action failed. Please try again.');
    }
  };

  if (loading) {
    return (
      <div className="declutter-loading">
        <RefreshCw size={40} className="spin text-purple" />
        <p>Loading your ingestion backlog queue...</p>
      </div>
    );
  }

  const hasItems = queue.length > 0 && currentIndex < queue.length;
  const currentItem = hasItems ? queue[currentIndex] : null;
  const remaining = queue.length - currentIndex;

  return (
    <div className="declutter-container">
      <div className="view-header">
        <h1>Swipe to Clean Console</h1>
        <p>Go through your raw bookmarks and bulk notes. Quickly categorize or delete items to maintain a tidy cognitive map.</p>
      </div>

      {lastAction && (
        <div className="declutter-toast animate-fade-in">
          <span>{lastAction}</span>
        </div>
      )}

      {!hasItems ? (
        <div className="empty-state glass declutter-empty animate-scale-up">
          <div className="success-circle">
            <Check size={48} className="text-green" />
          </div>
          <h3>Inbox Zero Achieved!</h3>
          <p>Fantastic work. All your incoming links and bulk pasted records have been decluttered and organized.</p>
          <button className="btn btn-primary" onClick={fetchQueue}>
            <RefreshCw size={16} /> Scan For New Links
          </button>
        </div>
      ) : (
        <div className="declutter-workspace">
          <div className="queue-status">
            <span>Remaining in backlog: <strong>{remaining}</strong> items</span>
            <div className="progress-mini-bar">
              <div 
                className="progress-mini-fill" 
                style={{ width: `${((queue.length - remaining) / queue.length) * 100}%` }}
              ></div>
            </div>
          </div>

          {/* Swipe Card Deck */}
          <div className="swipe-card-wrapper animate-slide-down">
            <div className="swipe-card glass">
              <div className="swipe-card-meta">
                <span className="badge badge-purple">
                  {currentItem.platformType === 0 ? 'YouTube' :
                   currentItem.platformType === 1 ? 'Web Article' :
                   currentItem.platformType === 2 ? 'Instagram' :
                   currentItem.platformType === 3 ? 'PDF' : 'TikTok'}
                </span>
                <span className="consume-time">{currentItem.consumeTimeMins} mins load</span>
              </div>

              <h2 className="swipe-card-title">{currentItem.title || currentItem.originalUrl}</h2>
              
              <div className="swipe-card-link-preview">
                <span>Reference URL:</span>
                <a href={currentItem.originalUrl} target="_blank" rel="noreferrer" title={currentItem.originalUrl}>
                  {currentItem.originalUrl}
                </a>
              </div>

              {currentItem.heroImageUrl && (
                <div className="swipe-card-img">
                  <img src={currentItem.heroImageUrl} alt="" />
                </div>
              )}

              {currentItem.tags && currentItem.tags.length > 0 && (
                <div className="swipe-card-tags">
                  {currentItem.tags.map(t => <span key={t} className="tag-pill">#{t}</span>)}
                </div>
              )}
            </div>
          </div>

          {/* Swipe Action Buttons */}
          <div className="swipe-buttons">
            <button 
              className="swipe-btn delete" 
              onClick={() => performAction('delete')}
              title="Delete Permanently (Left / Delete)"
            >
              <Trash2 size={24} />
              <span>Delete</span>
            </button>

            <button 
              className="swipe-btn archive" 
              onClick={() => performAction('archive')}
              title="Archive Memory (Up / Archive)"
            >
              <Archive size={24} />
              <span>Archive</span>
            </button>

            <button 
              className="swipe-btn pin" 
              onClick={() => performAction('pin')}
              title="Keep & Pin (Right / Pin)"
            >
              <Pin size={24} />
              <span>Pin / Keep</span>
            </button>
          </div>
          
          <div className="keyboard-helper">
            <span>Tip: You can quickly clean up by clicking the console action buttons.</span>
          </div>
        </div>
      )}
    </div>
  );
}
