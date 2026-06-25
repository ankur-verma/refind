import React from 'react';
import { BrainCircuit, BookOpen, Clock, Activity, Zap } from 'lucide-react';
import { useQuery } from '@tanstack/react-query';
import { request } from '../../../api';

export default function HeroCard({ item, onClick }: { item?: any; onClick?: (id: string) => void }) {
  const { data: statsData } = useQuery({
    queryKey: ['userStats'],
    queryFn: () => request('/user/stats'),
  });

  const memoriesSaved = statsData?.memoriesSaved ?? 0;
  const timeSavedMins = statsData?.timeSavedMins ?? 0;
  const timeSavedStr = timeSavedMins > 60 ? `${Math.floor(timeSavedMins / 60)}h ${timeSavedMins % 60}m` : `${timeSavedMins}m`;
  const deepDives = statsData?.deepDives ?? 0;
  const activeStreak = statsData?.activeStreak ?? 0;

  const stats = [
    { label: 'Memories Saved', value: memoriesSaved.toString(), icon: BookOpen, color: 'text-blue-500', bg: 'bg-blue-500/10' },
    { label: 'Time Saved', value: timeSavedStr, icon: Clock, color: 'text-green-500', bg: 'bg-green-500/10' },
    { label: 'Deep Dives', value: deepDives.toString(), icon: BrainCircuit, color: 'text-purple-500', bg: 'bg-purple-500/10' },
    { label: 'Active Streak', value: `${activeStreak} Days`, icon: Zap, color: 'text-yellow-500', bg: 'bg-yellow-500/10' },
  ];

  return (
    <div className="mb-10 mt-2">
      <h2 className="text-lg font-bold mb-4 flex items-center gap-2 text-foreground/80 uppercase tracking-wider">
        <Activity className="text-primary" size={18} /> Brain Activity
      </h2>
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
        {stats.map((stat, i) => (
          <div 
            key={i} 
            className="aspect-square bg-card border border-border rounded-[24px] p-5 flex flex-col justify-between hover:-translate-y-1 transition-transform cursor-default shadow-sm group"
          >
            <div className={`w-12 h-12 rounded-2xl flex items-center justify-center ${stat.bg} ${stat.color} group-hover:scale-110 transition-transform`}>
              <stat.icon size={24} />
            </div>
            <div>
              <div className="text-4xl font-extrabold mb-0.5 tracking-tight">{stat.value}</div>
              <div className="text-xs text-muted-foreground font-semibold uppercase tracking-wider">{stat.label}</div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
