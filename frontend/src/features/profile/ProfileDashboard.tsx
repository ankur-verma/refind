import React, { useEffect } from 'react';
import { User, TrendingUp, TrendingDown, Clock, CalendarDays, BrainCircuit, Activity, BarChart3, Settings, Sun, Moon } from 'lucide-react';
import { useLayoutStore } from '../../stores/layoutStore';
import FeedCard from '../content-feed/components/FeedCard';
import { useProfileOverview, useProfileInterests, useProfileCategories, useProfileUpcomingPlans, useProfileRevisited } from './api';
import { useTrackEvent } from '../../shared/analytics/useTrackEvent';
import { EventType } from '../../shared/analytics/EventTypes';
import ProfileActivityGraph from './components/ProfileActivityGraph';

interface ProfileDashboardProps {
  user: any;
  onSelectContent?: (id: string) => void;
}

export default function ProfileDashboard({ user, onSelectContent }: ProfileDashboardProps) {
  const { track } = useTrackEvent();
  const { theme, toggleTheme } = useLayoutStore();
  
  const { data: overviewRes, isLoading: isLoadingOverview } = useProfileOverview();
  const { data: interestsRes, isLoading: isLoadingInterests } = useProfileInterests();
  const { data: categoriesRes, isLoading: isLoadingCategories } = useProfileCategories();
  const { data: plansRes, isLoading: isLoadingPlans } = useProfileUpcomingPlans();
  const { data: revisitedRes, isLoading: isLoadingRevisited } = useProfileRevisited();

  useEffect(() => {
    track(EventType.Viewed, user?.id, { type: 'profile' });
  }, [track, user?.id]);

  const stats = overviewRes || { totalSaves: 0, aiQueries: 0, deepDives: 0, activeSince: new Date().getFullYear() };
  
  const hasInterests = interestsRes?.hasData;
  const safeInterests = interestsRes?.data || [];

  const hasCategories = categoriesRes?.hasData;
  const safeCategories = categoriesRes?.data || [];

  const hasPlans = plansRes?.hasData;
  const safePlans = plansRes?.data || [];

  const hasRevisited = revisitedRes?.hasData;
  const safeRevisited = revisitedRes?.data || [];

  return (
    <div className="min-h-full bg-background pb-20 md:pb-8">
      
      {/* Header Profile Section */}
      <div className="bg-card border-b border-border">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8 md:py-12 flex flex-col md:flex-row items-center md:items-start gap-6">
          <div className="w-24 h-24 rounded-full bg-gradient-to-br from-primary to-accent p-1 shadow-lg">
            <div className="w-full h-full bg-card rounded-full flex items-center justify-center text-primary overflow-hidden">
              {user?.avatarUrl ? (
                <img src={user.avatarUrl} alt="Avatar" className="w-full h-full object-cover" />
              ) : (
                <User size={40} />
              )}
            </div>
          </div>
          
          <div className="flex-1 text-center md:text-left">
            <h1 className="text-3xl font-extrabold tracking-tight text-foreground mb-1">
              {user?.name || 'Knowledge Explorer'}
            </h1>
            <p className="text-muted-foreground flex items-center justify-center md:justify-start gap-2">
              <BrainCircuit size={16} className="text-primary" /> Active since {stats.activeSince}
            </p>
            
            <div className="flex flex-wrap justify-center md:justify-start gap-4 mt-6">
              <div className="bg-secondary/50 border border-border/50 px-4 py-3 rounded-2xl flex flex-col items-center md:items-start min-w-[120px]">
                <span className="text-2xl font-black text-foreground">{isLoadingOverview ? '-' : stats.totalSaves}</span>
                <span className="text-xs text-muted-foreground font-medium uppercase tracking-wider">Total Saves</span>
              </div>
              <div className="bg-secondary/50 border border-border/50 px-4 py-3 rounded-2xl flex flex-col items-center md:items-start min-w-[120px]">
                <span className="text-2xl font-black text-primary">{isLoadingOverview ? '-' : stats.aiQueries}</span>
                <span className="text-xs text-muted-foreground font-medium uppercase tracking-wider">AI Queries</span>
              </div>
              <div className="bg-secondary/50 border border-border/50 px-4 py-3 rounded-2xl flex flex-col items-center md:items-start min-w-[120px]">
                <span className="text-2xl font-black text-foreground">{isLoadingOverview ? '-' : stats.deepDives}</span>
                <span className="text-xs text-muted-foreground font-medium uppercase tracking-wider">Deep Dives</span>
              </div>
            </div>
          </div>

          <div className="md:self-start flex gap-2">
            <button 
              onClick={toggleTheme}
              className="p-2 rounded-full bg-secondary/50 hover:bg-secondary text-muted-foreground transition-colors"
              title="Toggle Theme"
            >
              {theme === 'dark' ? <Sun size={20} /> : <Moon size={20} />}
            </button>
            <button className="p-2 rounded-full bg-secondary/50 hover:bg-secondary text-muted-foreground transition-colors">
              <Settings size={20} />
            </button>
          </div>
        </div>
      </div>

      <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8 space-y-8">
        
        {/* Activity Graph Row */}
        <ProfileActivityGraph />

        {/* Row 1: Trends & Categories */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
          
          {/* Interest Trends */}
          <div className="bg-card border border-border rounded-3xl p-6 shadow-sm min-h-[250px]">
            <div className="flex items-center gap-2 mb-6">
              <Activity className="text-primary" size={20} />
              <h2 className="text-lg font-bold">Interest Trends</h2>
            </div>
            {isLoadingInterests ? (
               <div className="flex justify-center items-center h-32 text-muted-foreground">Loading trends...</div>
            ) : hasInterests === false ? (
               <div className="flex justify-center items-center h-32 text-muted-foreground text-center px-4 text-sm leading-relaxed">We need at least 10 interactions to generate interest trends.<br/>Start saving and exploring content!</div>
            ) : safeInterests.length === 0 ? (
               <div className="flex justify-center items-center h-32 text-muted-foreground">No trends detected yet. Start saving content!</div>
            ) : (
              <div className="space-y-4">
                {safeInterests.map((trend: any) => (
                  <div key={trend.topic} className="flex items-center justify-between">
                    <span className="font-medium text-foreground">{trend.topic}</span>
                    <div className={`flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-bold ${trend.bg} ${trend.color}`}>
                      {trend.momentum === 'up' ? <TrendingUp size={14} /> : <TrendingDown size={14} />}
                      {Math.round(trend.percentage)}%
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Top Categories */}
          <div className="bg-card border border-border rounded-3xl p-6 shadow-sm min-h-[250px]">
            <div className="flex items-center gap-2 mb-6">
              <BarChart3 className="text-primary" size={20} />
              <h2 className="text-lg font-bold">Most Saved Categories</h2>
            </div>
            {isLoadingCategories ? (
               <div className="flex justify-center items-center h-32 text-muted-foreground">Loading categories...</div>
            ) : hasCategories === false ? (
               <div className="flex justify-center items-center h-32 text-muted-foreground text-center px-4 text-sm leading-relaxed">Save more content items to unlock your top categories.<br/>We need more data to analyze!</div>
            ) : safeCategories.length === 0 ? (
               <div className="flex justify-center items-center h-32 text-muted-foreground">No categories analyzed yet.</div>
            ) : (
              <div className="space-y-5">
                {safeCategories.map((cat: any) => (
                  <div key={cat.name} className="space-y-1.5">
                    <div className="flex justify-between text-sm">
                      <span className="font-medium text-foreground">{cat.name}</span>
                      <span className="text-muted-foreground font-semibold">{cat.count}</span>
                    </div>
                    <div className="h-2 w-full bg-secondary rounded-full overflow-hidden">
                      <div 
                        className="h-full bg-gradient-to-r from-primary to-accent rounded-full" 
                        style={{ width: `${cat.percentage}%` }}
                      />
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Row 2: Upcoming Plans */}
        <div className="bg-card border border-border rounded-3xl p-6 shadow-sm relative overflow-hidden group">
          <div className="absolute inset-0 bg-gradient-to-r from-primary/5 to-accent/5 opacity-0 group-hover:opacity-100 transition-opacity" />
          <div className="flex items-center gap-2 mb-6 relative z-10">
            <CalendarDays className="text-primary" size={20} />
            <h2 className="text-lg font-bold">Upcoming Plans (AI Detected)</h2>
          </div>
          
          {isLoadingPlans ? (
             <div className="flex justify-center items-center h-20 text-muted-foreground relative z-10">Detecting plans...</div>
          ) : hasPlans === false ? (
             <div className="flex justify-center items-center h-20 text-muted-foreground text-center px-4 text-sm relative z-10">We need at least 5 related saves to detect upcoming plans.<br/>Keep researching your next trip or project!</div>
          ) : safePlans.length === 0 ? (
             <div className="flex justify-center items-center h-20 text-muted-foreground relative z-10">No upcoming plans detected yet.</div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4 relative z-10">
              {safePlans.map((plan: any) => (
                <div 
                  key={plan.id} 
                  onClick={() => window.location.href = `/search?q=${encodeURIComponent(plan.title)}`}
                  className="bg-secondary/50 border border-border/50 rounded-2xl p-4 flex flex-col gap-2 group/plan cursor-pointer hover:bg-secondary transition-all hover:shadow-md hover:-translate-y-0.5"
                >
                  <div className="flex items-center gap-4">
                    <div className="bg-primary/20 p-3 rounded-xl text-primary text-xl flex-shrink-0">{plan.icon || '🎯'}</div>
                    <div className="flex-1">
                      <h3 className="font-bold text-foreground line-clamp-2">{plan.title}</h3>
                      <p className="text-xs text-muted-foreground mt-1">{plan.description}</p>
                    </div>
                  </div>
                  <div className="flex justify-end mt-1">
                     <span className="text-xs font-semibold text-primary flex items-center gap-1 opacity-0 group-hover/plan:opacity-100 transition-opacity">
                        Start Deep Dive &rarr;
                     </span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Row 3: Most Revisited */}
        <div>
          <div className="flex items-center gap-2 mb-6 px-2">
            <Clock className="text-primary" size={20} />
            <h2 className="text-xl font-bold tracking-tight">Most Revisited Content</h2>
          </div>
          
          {isLoadingRevisited ? (
             <div className="flex justify-center items-center h-32 text-muted-foreground">Loading recent content...</div>
          ) : hasRevisited === false ? (
             <div className="flex justify-center items-center h-32 text-muted-foreground text-center px-4 text-sm">Return to at least 20 items to see your most revisited content.<br/>Your deep dives will appear here.</div>
          ) : safeRevisited.length === 0 ? (
             <div className="flex justify-center items-center h-32 text-muted-foreground">You haven't revisited any content yet.</div>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
              {safeRevisited.map((item: any) => (
                <FeedCard key={item.id} item={item} onClick={onSelectContent || (() => {})} />
              ))}
            </div>
          )}
        </div>

      </div>
    </div>
  );
}
