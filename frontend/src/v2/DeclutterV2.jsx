import { useState, useEffect } from 'react';
import { Pin, Archive, Trash2, CheckCircle, RotateCcw } from 'lucide-react';
import { request } from '../api';

export default function DeclutterV2() {
  const [queue, setQueue] = useState([]);
  const [currentIndex, setCurrentIndex] = useState(0);
  const [loading, setLoading] = useState(true);

  const fetchQueue = async () => {
    setLoading(true);
    const res = await request('/content/feed?Page=1&PageSize=30');
    if (res && res.items) {
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
        method: 'POST', body: JSON.stringify({ action: 0 })
      });
      success = res.success;
    } else if (actionType === 'archive') {
      const res = await request(`/content/${currentItem.id}/declutter`, {
        method: 'POST', body: JSON.stringify({ action: 1 })
      });
      success = res.success;
    } else if (actionType === 'delete') {
      const res = await request(`/content/${currentItem.id}`, { method: 'DELETE' });
      success = res.success;
    }

    if (success) {
      setCurrentIndex(idx => idx + 1);
    }
  };

  if (loading) {
    return (
      <div className="v2-declutter v2-loading-view">
        <RotateCcw size={32} className="v2-pulse-icon v2-spin" />
        <p>Loading queue...</p>
      </div>
    );
  }

  const hasItems = queue.length > 0 && currentIndex < queue.length;
  const currentItem = hasItems ? queue[currentIndex] : null;

  return (
    <div className="v2-declutter">
      <header className="v2-header v2-text-center">
        <h1 className="v2-huge-title">Declutter</h1>
        <p className="v2-subtitle">Maintain cognitive focus by clearing the backlog.</p>
      </header>

      {!hasItems ? (
        <div className="v2-declutter-empty">
          <CheckCircle size={64} className="v2-text-green" />
          <h2>Inbox Zero Achieved</h2>
          <p>You have perfectly organized your digital footprint.</p>
          <button className="v2-btn-secondary" onClick={fetchQueue}>Refresh Queue</button>
        </div>
      ) : (
        <div className="v2-declutter-workspace">
          <div className="v2-declutter-card">
            {currentItem.heroImageUrl && (
              <div className="v2-dcard-img">
                <img src={currentItem.heroImageUrl} alt="" />
              </div>
            )}
            <div className="v2-dcard-body">
              <span className="v2-dcard-meta">{currentItem.consumeTimeMins} min read</span>
              <h2 className="v2-dcard-title">{currentItem.title || currentItem.originalUrl}</h2>
              <a href={currentItem.originalUrl} target="_blank" rel="noreferrer" className="v2-dcard-link">
                {currentItem.originalUrl}
              </a>
            </div>
          </div>

          <div className="v2-declutter-actions">
            <button className="v2-dbtn v2-dbtn-delete" onClick={() => performAction('delete')}>
              <div className="v2-dbtn-icon"><Trash2 size={24} /></div>
              <span>Delete</span>
            </button>
            <button className="v2-dbtn v2-dbtn-archive" onClick={() => performAction('archive')}>
              <div className="v2-dbtn-icon"><Archive size={24} /></div>
              <span>Archive</span>
            </button>
            <button className="v2-dbtn v2-dbtn-pin" onClick={() => performAction('pin')}>
              <div className="v2-dbtn-icon"><Pin size={24} /></div>
              <span>Pin</span>
            </button>
          </div>
          
          <div className="v2-declutter-progress">
            Remaining: {queue.length - currentIndex}
          </div>
        </div>
      )}
    </div>
  );
}
