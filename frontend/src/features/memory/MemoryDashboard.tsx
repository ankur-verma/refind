import React from 'react';
import { useMemoryTimeline, useMemoryRecommendations, useGenerateTimeline } from './api';
import { Sparkles, Clock, Compass, Brain, ArrowRight } from 'lucide-react';
import { Link } from 'react-router-dom';

export default function MemoryDashboard() {
  const { data: timelineRes, isLoading: isTimelineLoading } = useMemoryTimeline();
  const { data: recommendationsRes, isLoading: isRecsLoading } = useMemoryRecommendations();
  const generateTimeline = useGenerateTimeline();

  const hasTimeline = timelineRes?.hasData;
  const timeline = timelineRes?.data || [];

  const hasRecommendations = recommendationsRes?.hasData;
  const recommendations = recommendationsRes?.data || [];

  const handleGenerate = () => {
    generateTimeline.mutate();
  };

  return (
    <div className="flex flex-col h-full overflow-hidden bg-background">
      <header className="flex-none px-6 py-6 md:px-10 md:py-8 border-b border-border bg-card z-10">
        <div className="flex flex-col md:flex-row md:items-end justify-between gap-4">
          <div>
            <div className="flex items-center gap-3">
              <Brain size={32} className="text-primary" />
              <h1 className="text-3xl md:text-4xl font-extrabold tracking-tight text-foreground">
                Memory
              </h1>
            </div>
            <p className="text-muted-foreground mt-2 max-w-2xl">
              Rediscover your past interests, revisit forgotten ideas, and see how your mind has evolved over time.
            </p>
          </div>
          
          <button 
            onClick={handleGenerate}
            disabled={generateTimeline.isPending}
            className="flex items-center gap-2 bg-secondary text-secondary-foreground hover:bg-secondary/80 px-4 py-2 rounded-full font-semibold transition-colors disabled:opacity-50"
          >
            {generateTimeline.isPending ? (
              <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-secondary-foreground"></div>
            ) : (
              <Sparkles size={16} />
            )}
            Refresh Memories
          </button>
        </div>
      </header>

      <main className="flex-1 overflow-y-auto p-6 md:p-10 space-y-12">
        {/* Memory Recommendations Section */}
        <section>
          <div className="flex items-center gap-2 mb-6">
            <Compass className="text-primary" size={24} />
            <h2 className="text-2xl font-bold text-foreground">Surface from the Past</h2>
          </div>
          
          {isRecsLoading ? (
            <div className="flex space-x-4 overflow-x-auto pb-4">
              {[1, 2, 3].map(i => (
                <div key={i} className="min-w-[280px] h-40 bg-secondary/50 animate-pulse rounded-2xl"></div>
              ))}
            </div>
          ) : recommendations && recommendations.length > 0 ? (
            <div className="flex space-x-4 overflow-x-auto pb-4 snap-x">
              {recommendations.map(rec => (
                <div key={rec.id} className="snap-start shrink-0 w-[280px] md:w-[320px] bg-card border border-border p-5 rounded-2xl flex flex-col hover:shadow-lg transition-shadow">
                  <div className="text-xs font-semibold text-primary uppercase tracking-wider mb-2">{rec.type.replace(/([A-Z])/g, ' $1').trim()}</div>
                  <h3 className="font-bold text-lg mb-2 line-clamp-2">{rec.title}</h3>
                  <p className="text-sm text-muted-foreground flex-1 line-clamp-3 mb-4">{rec.reason}</p>
                  
                  {rec.content && (
                    <div className="mt-auto bg-secondary/50 rounded-lg p-2 flex items-center gap-3">
                      {rec.content.thumbnailUrl ? (
                        <img src={rec.content.thumbnailUrl} alt="" className="w-10 h-10 rounded-md object-cover" />
                      ) : (
                        <div className="w-10 h-10 rounded-md bg-secondary flex items-center justify-center font-bold text-muted-foreground">
                          {rec.content.title.charAt(0)}
                        </div>
                      )}
                      <span className="text-sm font-medium line-clamp-1 flex-1">{rec.content.title}</span>
                      <ArrowRight size={16} className="text-muted-foreground shrink-0" />
                    </div>
                  )}
                  {rec.collection && (
                    <Link to={`/collections/${rec.collection.id}`} className="mt-auto bg-primary/10 text-primary rounded-lg p-3 flex items-center justify-between hover:bg-primary/20 transition-colors">
                      <span className="text-sm font-semibold">{rec.collection.title}</span>
                      <ArrowRight size={16} />
                    </Link>
                  )}
                </div>
              ))}
            </div>
          ) : hasRecommendations === false ? (
            <div className="p-8 border border-dashed border-border rounded-2xl text-center text-muted-foreground">
              <p>Your AI memory will start surfacing recommendations after you interact with at least 10 items.</p>
            </div>
          ) : (
            <div className="p-8 border border-dashed border-border rounded-2xl text-center text-muted-foreground">
              <p>No new memories to surface right now.</p>
            </div>
          )}
        </section>

        {/* Timeline Section */}
        <section>
          <div className="flex items-center gap-2 mb-8">
            <Clock className="text-primary" size={24} />
            <h2 className="text-2xl font-bold text-foreground">Your Interest Timeline</h2>
          </div>

          {isTimelineLoading ? (
            <div className="space-y-8">
              {[1, 2].map(i => (
                <div key={i} className="flex gap-6">
                  <div className="w-32 h-6 bg-secondary animate-pulse rounded"></div>
                  <div className="flex-1 h-24 bg-secondary animate-pulse rounded-xl"></div>
                </div>
              ))}
            </div>
          ) : timeline && timeline.length > 0 ? (
            <div className="relative border-l-2 border-border ml-4 md:ml-20 space-y-12 pb-12">
              {timeline.map((event, idx) => (
                <div key={event.id} className="relative pl-8 md:pl-12">
                  <div className="absolute -left-[9px] top-1 w-4 h-4 rounded-full bg-primary ring-4 ring-background"></div>
                  <div className="absolute -left-20 md:-left-32 top-0 w-16 md:w-24 text-right">
                    <span className="text-sm font-bold text-muted-foreground block">{event.monthYear.split(' ')[0]}</span>
                    <span className="text-xs text-muted-foreground/70">{event.monthYear.split(' ')[1]}</span>
                  </div>
                  
                  <div className="bg-card border border-border p-6 rounded-2xl shadow-sm hover:shadow-md transition-shadow">
                    <h3 className="text-xl font-bold text-foreground mb-2">{event.theme}</h3>
                    <p className="text-muted-foreground leading-relaxed">{event.summary}</p>
                    <div className="mt-4 flex items-center gap-2 text-sm font-medium text-primary bg-primary/5 w-fit px-3 py-1 rounded-full">
                      <Sparkles size={14} />
                      {event.itemCount} items explored
                    </div>
                  </div>
                </div>
              ))}
            </div>
          ) : hasTimeline === false ? (
            <div className="p-12 border border-dashed border-border rounded-2xl text-center text-muted-foreground flex flex-col items-center">
              <Clock size={48} className="opacity-20 mb-4" />
              <p className="mb-4">You need to save more items before we can generate a timeline of your interests.</p>
            </div>
          ) : (
            <div className="p-12 border border-dashed border-border rounded-2xl text-center text-muted-foreground flex flex-col items-center">
              <Clock size={48} className="opacity-20 mb-4" />
              <p className="mb-4">We haven't compiled your timeline yet.</p>
              <button 
                onClick={handleGenerate}
                className="bg-primary text-primary-foreground px-4 py-2 rounded-lg font-medium"
              >
                Generate Timeline
              </button>
            </div>
          )}
        </section>
      </main>
    </div>
  );
}
