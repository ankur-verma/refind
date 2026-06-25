import React, { useState, useEffect, useRef } from 'react';
import ReelCard from '../features/reels/ReelCard';
import { request } from '../api';
import { useQuery } from '@tanstack/react-query';
import { Sparkles } from 'lucide-react';

export default function ClipsPage() {
  const [activeIndex, setActiveIndex] = useState(0);
  const [isSynthesizing, setIsSynthesizing] = useState(false);
  const [synthesizedData, setSynthesizedData] = useState<any>(null);
  const [synthesisProgress, setSynthesisProgress] = useState(0);
  const [synthesisStatusMsg, setSynthesisStatusMsg] = useState("");
  const containerRef = useRef<HTMLDivElement>(null);
  const pollIntervalRef = useRef<any>(null);



  useEffect(() => {
    return () => {
      if (pollIntervalRef.current) clearInterval(pollIntervalRef.current);
    };
  }, []);

  const fetchClips = async () => {
    // Fetch ML-generated dynamic reel
    const res = await request('/quickboost/dynamic-reel?limit=10');
    
    // Map backend DTO to the format ReelCard expects
    const reels = (res || []).map((seg: any, index: number) => {
      const platformStr = seg.platformType || seg.PlatformType;
      const typeNum = platformStr === 'YouTube' ? 0 : platformStr === 'TikTok' ? 4 : platformStr === 'Instagram' ? 2 : 0;
      
      return {
        ...seg,
        id: seg.id || seg.Id || `clip-${index}`,
        videoUrl: seg.originalUrl || seg.OriginalUrl,
        platformType: typeNum,
        title: seg.title || seg.Title,
        summary: seg.summary || seg.Summary,
        categoryLabel: "Dynamic Selection",
        timestamps: [{ label: seg.title || seg.Title, time: seg.startSeconds || seg.StartSeconds }],
        segmentStart: seg.startSeconds || seg.StartSeconds,
        segmentEnd: (seg.endSeconds || seg.EndSeconds) > 0 ? (seg.endSeconds || seg.EndSeconds) : null
      };
    });

    return reels;
  };

  const { data: clips = [], isLoading } = useQuery({
    queryKey: ['clipsFeed'],
    queryFn: fetchClips,
  });

  useEffect(() => {
    if (clips.length > 0 && clips[activeIndex]) {
      import('../shared/analytics/AnalyticsService').then(({ analytics }) => {
        analytics.track('VideoStarted', clips[activeIndex].id, { index: activeIndex });
      });
    }
  }, [activeIndex, clips]);

  const handleSynthesize = async () => {
    if (!clips || clips.length === 0) return;
    setIsSynthesizing(true);
    setSynthesisProgress(0);
    setSynthesisStatusMsg("Starting synthesis process...");
    
    try {
      const transcripts = clips.map((c: any) => c.summary).filter(Boolean);
      const res = await request('/quickboost/synthesize', {
        method: 'POST',
        body: JSON.stringify({ transcripts })
      });
      
      const taskId = res.taskId || res.task_id;
      if (!taskId) {
         throw new Error("Failed to start task");
      }

      // Start polling
      pollIntervalRef.current = setInterval(async () => {
        const statusRes = await request(`/quickboost/synthesize/${taskId}/status`);
        if (statusRes && statusRes.status) {
          setSynthesisProgress(statusRes.progress || 0);
          setSynthesisStatusMsg(statusRes.status);

          if (statusRes.progress >= 100 || statusRes.status === "Done" || statusRes.status === "Error") {
            clearInterval(pollIntervalRef.current);
            setIsSynthesizing(false);
            if (statusRes.result && statusRes.result.success) {
              setSynthesizedData(statusRes.result);
            } else {
              console.error("Synthesis failed:", statusRes.result);
              alert("Video synthesis failed: " + (statusRes.result?.error || "Unknown error"));
            }
          }
        }
      }, 1000);

    } catch (e) {
      console.error("Failed to start synthesizing video", e);
      setIsSynthesizing(false);
    }
  };

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            const index = Number(entry.target.getAttribute('data-index'));
            if (!isNaN(index) && index >= 0 && index < clips.length) {
              setActiveIndex(index);
            }
          }
        });
      },
      {
        root: container,
        threshold: 0.6, // Fire when 60% of the reel is visible
      }
    );

    const children = container.querySelectorAll('.reel-card-container');
    children.forEach((child) => observer.observe(child));

    return () => observer.disconnect();
  }, [clips.length]);

  return (
    <div className="fixed inset-0 md:relative md:inset-auto bg-black md:bg-background h-[100dvh] md:h-full w-full flex items-center justify-center z-40 md:z-auto">
      
      {/* Container for vertical snapping */}
      <div 
        ref={containerRef}
        className="w-full h-full md:w-full md:max-w-[600px] md:h-full overflow-y-scroll snap-y snap-mandatory no-scrollbar flex flex-col md:py-4 items-center"
        style={{ scrollBehavior: 'smooth' }}
      >
        {isLoading ? (
          <div className="flex h-full w-full items-center justify-center text-white/50">
            Loading your feed...
          </div>
        ) : clips.length === 0 ? (
          <div className="flex h-full w-full items-center justify-center text-white/50 px-8 text-center">
            No video content found. Try saving some YouTube or TikTok links first!
          </div>
        ) : (
          clips.map((item, index) => {
            return (
              <ReelCard 
                key={item.id} 
                item={item} 
                isActive={index === activeIndex} 
                index={index}
              />
            );
          })
        )}
      </div>

      {/* Floating Action Button for Synthesize Video */}
      {!isLoading && clips.length > 0 && !synthesizedData && !isSynthesizing && (
        <div className="absolute top-4 right-4 z-50">
          <button 
            onClick={handleSynthesize}
            className="flex items-center space-x-2 bg-gradient-to-r from-blue-600 to-purple-600 text-white px-4 py-2 rounded-full shadow-lg hover:shadow-xl transition-all transform hover:scale-105"
          >
            <Sparkles className="h-5 w-5" />
            <span className="font-medium text-sm">
              Synthesize AI Video
            </span>
          </button>
        </div>
      )}

      {/* Synthesis Progress Overlay */}
      {isSynthesizing && (
        <div className="absolute inset-0 z-50 bg-black/80 backdrop-blur-sm flex flex-col items-center justify-center p-6">
          <div className="w-full max-w-md bg-white/10 rounded-2xl p-8 border border-white/20 shadow-2xl flex flex-col items-center text-center">
            <Sparkles className="h-12 w-12 text-purple-400 mb-4 animate-pulse" />
            <h2 className="text-2xl font-bold text-white mb-2">Crafting Your Video</h2>
            <p className="text-white/70 mb-8">{synthesisStatusMsg || "Initializing..."}</p>
            
            <div className="w-full bg-black/50 rounded-full h-4 overflow-hidden border border-white/10 relative">
              <div 
                className="bg-gradient-to-r from-blue-500 via-purple-500 to-pink-500 h-full transition-all duration-1000 ease-out relative"
                style={{ width: `${synthesisProgress}%` }}
              >
                <div className="absolute inset-0 bg-white/20 animate-pulse"></div>
              </div>
            </div>
            <div className="mt-4 text-white/90 font-mono font-bold text-xl">{synthesisProgress}%</div>
          </div>
        </div>
      )}

      {/* Synthesized Video Modal Overlay */}
      {synthesizedData && (
        <div className="absolute inset-0 z-50 bg-black/90 flex flex-col items-center justify-center p-4">
          <div className="w-full max-w-md bg-white/10 backdrop-blur-md rounded-2xl p-6 border border-white/20">
            <h2 className="text-xl font-bold text-white mb-2 flex items-center">
              <Sparkles className="h-6 w-6 text-purple-400 mr-2" />
              Your Copyright-Free AI Video
            </h2>
            <p className="text-white/70 text-sm mb-4">
              Theme: <span className="text-purple-400 font-semibold">{synthesizedData.theme}</span>
            </p>
            
            {/* The actual video player */}
            <div className="aspect-[9/16] bg-black rounded-xl overflow-hidden mb-4 relative">
              <video 
                src={synthesizedData.finalVideoUrl} 
                controls 
                autoPlay 
                className="w-full h-full object-contain"
              />
            </div>

            <div className="max-h-32 overflow-y-auto mb-4 custom-scrollbar">
              <p className="text-white/80 text-xs italic bg-black/30 p-3 rounded-lg border border-white/10">
                "{synthesizedData.script}"
              </p>
            </div>

            <button 
              onClick={() => setSynthesizedData(null)}
              className="w-full py-3 bg-white/10 hover:bg-white/20 text-white rounded-xl transition-colors font-medium border border-white/10"
            >
              Close
            </button>
          </div>
        </div>
      )}
      
    </div>
  );
}
