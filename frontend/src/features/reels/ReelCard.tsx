import React, { useEffect, useRef, useState } from 'react';
import { Play, Pause, Bookmark, Sparkles, Share2, Volume2, VolumeX, FolderPlus } from 'lucide-react';
import { useLayoutStore } from '../../stores/layoutStore';

interface ReelProps {
  item: any;
  isActive: boolean;
  index?: number;
}

export default function ReelCard({ item, isActive, index }: ReelProps) {
  const videoRef = useRef<HTMLVideoElement>(null);
  const [isPlaying, setIsPlaying] = useState(false);
  const [isMuted, setIsMuted] = useState(true);
  const { setAssistantOpen } = useLayoutStore();

  const isYouTube = item.videoUrl?.includes('youtube.com') || item.videoUrl?.includes('youtu.be');
  let youtubeId = null;
  if (isYouTube) {
    if (item.videoUrl.includes('youtube.com/watch')) {
      try {
        const urlObj = new URL(item.videoUrl);
        youtubeId = urlObj.searchParams.get('v');
      } catch (e) {}
    } else if (item.videoUrl.includes('youtube.com/shorts/')) {
      youtubeId = item.videoUrl.split('shorts/')[1]?.split('?')[0];
    } else if (item.videoUrl.includes('youtu.be/')) {
      youtubeId = item.videoUrl.split('youtu.be/')[1]?.split('?')[0];
    } else if (item.videoUrl.includes('youtube.com/embed/')) {
      youtubeId = item.videoUrl.split('embed/')[1]?.split('?')[0];
    }
  }

  useEffect(() => {
    if (isYouTube) {
      setIsPlaying(isActive);
      return;
    }

    if (isActive && videoRef.current) {
      videoRef.current.play().then(() => setIsPlaying(true)).catch(console.error);
    } else if (videoRef.current) {
      videoRef.current.pause();
      setIsPlaying(false);
      videoRef.current.currentTime = 0; // reset
    }
  }, [isActive, isYouTube]);

  const togglePlay = () => {
    if (isYouTube) return; // YouTube iframe handles its own clicks
    if (!videoRef.current) return;
    if (isPlaying) {
      videoRef.current.pause();
      setIsPlaying(false);
    } else {
      videoRef.current.play();
      setIsPlaying(true);
    }
  };

  const toggleMute = (e: React.MouseEvent) => {
    e.stopPropagation();
    setIsMuted(!isMuted);
  };

  const handleAskAI = (e: React.MouseEvent) => {
    e.stopPropagation();
    setAssistantOpen(true);
    // In a real app, we'd also seed the assistant with this video context
  };

  const handleAction = (e: React.MouseEvent, action: string) => {
    e.stopPropagation();
    console.log(`Action [${action}] on clip ${item.id}`);
  };

  const ActionButtons = ({ isDesktop }: { isDesktop?: boolean }) => (
    <div className={`flex flex-col items-center gap-6 ${isDesktop ? 'hidden md:flex' : 'md:hidden absolute right-4 bottom-24 z-20'}`}>
      <button className="flex flex-col items-center gap-1.5 group/btn" onClick={(e) => handleAction(e, 'like')}>
        <div className="bg-white/10 hover:bg-white/20 p-3 rounded-full text-white transition-all backdrop-blur-sm">
          <Bookmark size={24} />
        </div>
        <span className="text-xs font-medium text-white shadow-sm">Save</span>
      </button>

      <button className="flex flex-col items-center gap-1.5 group/btn" onClick={handleAskAI}>
        <div className="bg-purple-500/80 p-3 rounded-full text-white group-hover/btn:bg-purple-600 transition-all shadow-[0_0_15px_rgba(168,85,247,0.5)]">
          <Sparkles size={24} />
        </div>
        <span className="text-xs font-medium text-white">Ask AI</span>
      </button>

      <button className="flex flex-col items-center gap-1.5 group/btn" onClick={(e) => handleAction(e, 'collection')}>
        <div className="bg-white/10 hover:bg-white/20 p-3 rounded-full text-white transition-all backdrop-blur-sm">
          <FolderPlus size={24} />
        </div>
        <span className="text-xs font-medium text-white">Add</span>
      </button>

      <button className="flex flex-col items-center gap-1.5 group/btn" onClick={(e) => handleAction(e, 'share')}>
        <div className="bg-white/10 hover:bg-white/20 p-3 rounded-full text-white transition-all backdrop-blur-sm">
          <Share2 size={24} />
        </div>
        <span className="text-xs font-medium text-white">Share</span>
      </button>
    </div>
  );

  return (
    <div 
      data-index={index}
      className="reel-card-container w-full h-[100dvh] md:h-screen snap-start flex-shrink-0 flex items-center justify-center bg-black md:bg-transparent"
    >
      {/* Container for Video + Inside Overlays */}
      <div className="relative w-full h-full md:w-[400px] md:h-[90vh] md:max-h-[850px] md:rounded-2xl overflow-hidden bg-black shadow-2xl">
        
        {/* Video Element */}
        <div className="absolute inset-0 cursor-pointer" onClick={togglePlay}>
          {isYouTube && youtubeId ? (
            isActive ? (
              <iframe
                src={`https://www.youtube.com/embed/${youtubeId}?autoplay=1&mute=${isMuted ? 1 : 0}&controls=1&modestbranding=1&rel=0&loop=1&playlist=${youtubeId}&playsinline=1${item.segmentStart !== null && item.segmentStart !== undefined ? `&start=${Math.floor(item.segmentStart)}` : ''}${item.segmentEnd !== null && item.segmentEnd !== undefined ? `&end=${Math.ceil(item.segmentEnd)}` : ''}`}
                className="w-full h-full object-contain pointer-events-auto"
                allow="autoplay; encrypted-media; picture-in-picture"
                frameBorder="0"
              />
            ) : (
              <img src={item.thumbnailUrl || item.heroImageUrl} className="w-full h-full object-contain" alt="thumbnail" />
            )
          ) : (
            <video
              ref={videoRef}
              src={item.videoUrl}
              loop
              muted={isMuted}
              playsInline
              controls={false}
              className="w-full h-full object-contain"
              poster={item.thumbnailUrl || item.heroImageUrl}
            />
          )}
        </div>

        {/* Play/Pause Overlay Indicator */}
        {!isPlaying && !isYouTube && (
          <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
            <div className="bg-black/50 p-5 rounded-full text-white backdrop-blur-sm">
              <Play size={48} className="ml-2" fill="currentColor" />
            </div>
          </div>
        )}

        {/* Top Header Overlay */}
        <div className="absolute top-0 inset-x-0 p-4 z-20 flex justify-between items-start pointer-events-none bg-gradient-to-b from-black/60 to-transparent pb-10">
          <div className="flex flex-col gap-2">
            {item.categoryLabel && item.categoryLabel.toLowerCase() !== 'general' && (
              <span className="bg-white/10 backdrop-blur-md text-white text-[10px] font-bold px-3 py-1 rounded-full uppercase tracking-wider w-max border border-white/10">
                {item.categoryLabel}
              </span>
            )}
          </div>
          <button 
            onClick={toggleMute} 
            className="pointer-events-auto bg-black/40 p-2.5 rounded-full hover:bg-black/60 transition-colors text-white backdrop-blur-md"
          >
            {isMuted ? <VolumeX size={18} /> : <Volume2 size={18} />}
          </button>
        </div>

        {/* Mobile Action Buttons (Inside) */}
        <ActionButtons />

        {/* Info Context (Bottom Overlay) */}
        <div className="absolute bottom-0 inset-x-0 bg-gradient-to-t from-black/90 via-black/60 to-transparent pt-24 pb-6 px-4 md:px-6 z-20 pointer-events-none">
          <div className="md:pr-0 pr-16"> {/* Leave space for mobile buttons on right */}
            <h2 className="text-lg md:text-xl font-bold text-white leading-tight mb-1.5 drop-shadow-md">
              {item.title}
            </h2>
            <p className="text-sm text-white/80 line-clamp-2 mb-3 drop-shadow-sm font-medium">
              {item.summary}
            </p>

            {/* AI Jump Timestamps */}
            {item.timestamps && item.timestamps.length > 0 && (
              <div className="flex gap-2 overflow-x-auto no-scrollbar pointer-events-auto">
                {item.timestamps.map((ts: any, idx: number) => (
                  <button 
                    key={idx}
                    onClick={(e) => {
                      e.stopPropagation();
                      if (videoRef.current) videoRef.current.currentTime = ts.time;
                    }}
                    className="flex-none bg-white/15 hover:bg-white/25 px-2.5 py-1 rounded-md text-xs font-semibold flex items-center gap-1.5 transition-colors border border-white/10 text-white backdrop-blur-sm"
                  >
                    <span>{ts.label}</span>
                    <span className="opacity-70">{Math.floor(ts.time / 60)}:{(ts.time % 60).toString().padStart(2, '0')}</span>
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Desktop Action Buttons (Outside Right) */}
      <div className="hidden md:flex flex-col justify-end h-[90vh] max-h-[850px] pb-4 pl-4 w-[80px]">
        <ActionButtons isDesktop />
      </div>
    </div>
  );
}
