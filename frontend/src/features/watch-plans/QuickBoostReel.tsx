import React, { useEffect, useState } from 'react';
import { createFragmentUrl } from '../../utils/videoUtils';
import { request } from '../../api';
import './QuickBoostCard.css';

export default function QuickBoostReel({ category, userId, topic }) {
  const [clips, setClips] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    const fetchClips = async () => {
      setLoading(true);
      try {
        // Try AI watch-plan endpoint first if topic is provided
        if (userId && topic) {
          const res = await request(`/quickboost/watch-plan?userId=${userId}&topic=${encodeURIComponent(topic)}`);
          if (res.success && res.data) {
            setClips(res.data);
            return;
          }
        }
        
        // Try generic AI-generated endpoint
        if (userId && !topic) {
          const res = await request(`/quickboost/ai-recs?userId=${userId}`);
          if (res.success && res.data) {
            setClips(res.data);
            return;
          }
          console.warn('AI endpoint failed, falling back');
        }
        // Legacy QuickBoost endpoint (category based)
        const resLegacy = await request(`/quickboost?category=${encodeURIComponent(category || '')}`);
        if (resLegacy.success && resLegacy.data) {
          setClips(resLegacy.data);
        } else {
          console.warn('Legacy endpoint unavailable, loading static fallback');
          const fallbackRes = await fetch('/quickboost.json');
          if (!fallbackRes.ok) throw new Error('Failed to fetch quickboost data from both API and fallback');
          const data = await fallbackRes.json();
          setClips(data);
        }
      } catch (err) {
        console.error(err);
        setError(err.message);
      } finally {
        setLoading(false);
      }
    };
    fetchClips();
  }, [category, userId, topic]);

  if (loading) return <div className="quickboost-loading glass">Loading reels…</div>;
  if (error) return <div className="quickboost-error glass">Error: {error}</div>;
  if (!clips || clips.length === 0) return <div className="quickboost-empty glass">No quick reels available.</div>;

  return (
    <div className="quickboost-reel">
      {clips.map((clip, idx) => {
        // Handle both C# record (videoUrl, startSec, endSec) and fallback JSON (videoUrl, startSeconds, endSeconds)
        const videoUrl = clip.videoUrl;
        const startSeconds = clip.startSec !== undefined ? clip.startSec : clip.startSeconds;
        const endSeconds = clip.endSec !== undefined ? clip.endSec : clip.endSeconds;
        const title = clip.title;
        const fragmentUrl = createFragmentUrl(videoUrl, startSeconds, endSeconds);
        const thumbnail = clip.thumbnailUrl || clip.thumbnail;
        const handlePlay = () => {
          import('../../utils/telemetry').then(({ trackInteraction }) => {
            trackInteraction({
              contentItemId: clip.contentItemId || null,
              interactionType: 'View',
              durationSeconds: 15 // approximation for shorts
            });
          });
        };
        const isYouTube = fragmentUrl.includes('youtube.com/embed');
        return (
          <div key={clip.videoId || idx} className="quickboost-card glass-sub">
            {isYouTube ? (
              <iframe
                src={fragmentUrl}
                className="quickboost-video"
                frameBorder="0"
                allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                allowFullScreen
                onLoad={handlePlay}
                style={{ pointerEvents: 'auto' }}
              />
            ) : (
              <video
                src={fragmentUrl}
                poster={thumbnail}
                muted
                loop
                playsInline
                autoPlay
                onPlay={handlePlay}
                className="quickboost-video"
              />
            )}
            <div className="quickboost-info">
              <h4>{title}</h4>
            </div>
          </div>
        );
      })}
    </div>
  );
}
