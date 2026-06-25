import React from 'react';
import { Play, Globe, Camera, FileText, MapPin, BrainCircuit, Star } from 'lucide-react';
import CardActions from './CardActions';
import { generateAiMetadata } from '../../../utils/mockAi';

const PLATFORM_TYPES = {
  0: { label: 'YouTube', icon: Play, bg: 'bg-red-500/20 text-red-500' },
  1: { label: 'Web', icon: Globe, bg: 'bg-blue-500/20 text-blue-500' },
  2: { label: 'Instagram', icon: Camera, bg: 'bg-pink-500/20 text-pink-500' },
  3: { label: 'PDF', icon: FileText, bg: 'bg-orange-500/20 text-orange-500' },
  4: { label: 'TikTok', icon: Camera, bg: 'bg-slate-500/20 text-slate-300' }
};

const ENERGY_LEVELS = {
  0: { class: 'bg-green-500 shadow-[0_0_8px_rgba(34,197,94,0.6)]' },
  1: { class: 'bg-blue-500 shadow-[0_0_8px_rgba(59,130,246,0.6)]' },
  2: { class: 'bg-purple-500 shadow-[0_0_8px_rgba(168,85,247,0.6)]' }
};

export default function FeedCard({ item, onClick, isDiscovery = false, onSaveToBrain }: { item: any; onClick: (id: string) => void; isDiscovery?: boolean; onSaveToBrain?: (e: React.MouseEvent) => void }) {
  const platform = PLATFORM_TYPES[item.platformType as keyof typeof PLATFORM_TYPES] || PLATFORM_TYPES[1];
  const PlatformIcon = platform.icon;
  const energy = ENERGY_LEVELS[item.energyLevel as keyof typeof ENERGY_LEVELS] || ENERGY_LEVELS[1];
  
  const aiData = generateAiMetadata(item);
  const CategoryIcon = aiData.categoryIcon;

  // Handlers for action buttons
  const handleAction = (e: React.MouseEvent, action: string) => {
    e.stopPropagation();
    if (action === 'askAi' || action === 'addNote' || action === 'revisit') {
      onClick(item.id, action);
    } else {
      console.log(`Action [${action}] triggered for ${item.id}`);
    }
  };

  return (
    <div 
      className="group relative flex-none w-52 md:w-60 aspect-[3/4] rounded-2xl overflow-hidden cursor-pointer snap-start transition-all duration-300 hover:-translate-y-2 hover:shadow-xl border border-border bg-card shadow-sm"
      onClick={() => onClick(item.id)}
    >
      {/* Background Image / Placeholder */}
      <div className="absolute inset-0 z-0 bg-muted/10">
        {item.heroImageUrl || item.thumbnailUrl ? (
          <img src={item.heroImageUrl || item.thumbnailUrl} alt={item.title} className="w-full h-3/5 object-cover transition-transform duration-700 group-hover:scale-105 opacity-90" />
        ) : (
          <div className="w-full h-3/5 bg-secondary flex items-center justify-center">
            <PlatformIcon size={48} className="text-muted/40" />
          </div>
        )}
        {/* Solid gradient transition to text area */}
        <div className="absolute top-0 inset-x-0 h-32 bg-gradient-to-b from-black/40 to-transparent"></div>
        <div className="absolute inset-x-0 top-2/5 h-1/5 bg-gradient-to-b from-transparent to-card z-10"></div>
        <div className="absolute inset-x-0 bottom-0 h-2/5 bg-card z-10"></div>
      </div>

      {/* Top Badges */}
      <div className="absolute top-3 left-3 right-3 z-20 flex justify-between items-start">
        <div className={`px-2 py-0.5 rounded-full text-[10px] font-bold flex items-center gap-1 bg-background/90 backdrop-blur-md shadow-sm border border-border text-foreground`}>
          <PlatformIcon size={10} className={platform.bg.split(' ')[1]} /> {platform.label}
        </div>
        <div className="flex flex-col items-end gap-1.5">
          {!isDiscovery && (
            <div className="flex items-center gap-1.5 bg-background/80 px-2.5 py-1 rounded-full text-xs text-muted-foreground font-medium border border-border/50 shadow-sm">
              <span className={`w-2 h-2 rounded-full ${energy.class}`}></span>
              {item.consumeTimeMins}m
            </div>
          )}
          {/* AI Confidence Badge */}
          <div className="bg-primary/90 text-primary-foreground px-2 py-0.5 rounded-full text-[10px] font-bold shadow-sm opacity-0 group-hover:opacity-100 transition-opacity flex items-center gap-1">
            <BrainCircuit size={10} /> {aiData.confidence}% match
          </div>
        </div>
      </div>

      {/* Content Area */}
      <div className="absolute inset-x-0 bottom-0 z-20 flex flex-col justify-end p-4 pt-8 bg-gradient-to-t from-card via-card to-transparent h-2/5">
        
        {/* Dynamic AI Metadata Pills */}
        <div className="flex flex-wrap gap-1.5 mb-2">
          {aiData.location && (
            <div className="flex items-center gap-1 bg-secondary text-secondary-foreground text-[9px] font-bold px-1.5 py-0.5 rounded border border-border">
              <MapPin size={8} className="text-red-400" /> {aiData.location}
            </div>
          )}
          <div className="flex items-center gap-1 bg-secondary text-secondary-foreground text-[9px] font-bold px-1.5 py-0.5 rounded border border-border">
            <CategoryIcon size={8} className="text-yellow-500" /> {aiData.categoryLabel}
          </div>
        </div>

        {/* Text Area */}
        <div className="space-y-1">
          <h3 className="text-sm font-bold leading-tight text-foreground line-clamp-2 group-hover:text-primary transition-colors">
            {item.title || item.originalUrl || 'Untitled Discovery'}
          </h3>
          <p className="text-xs text-muted-foreground line-clamp-2">
            {item.summary || 'Tap to explore this memory.'}
          </p>
        </div>
        
        {/* Save Date Footer */}
        <div className="mt-3 text-[9px] text-muted-foreground/80 font-bold uppercase tracking-wider">
          {isDiscovery ? 'Recommended' : `Saved ${new Date(item.createdAt).toLocaleDateString()}`}
        </div>
      </div>
      
      {/* Actions Overlay (Hover) */}
      <CardActions 
        url={item.originalUrl || item.url}
        isDiscovery={isDiscovery}
        onAskAi={(e) => handleAction(e, 'askAi')}
        onAddNote={(e) => handleAction(e, 'addNote')}
        onArchive={(e) => handleAction(e, 'archive')}
        onShare={(e) => handleAction(e, 'share')}
        onRevisit={(e) => handleAction(e, 'revisit')}
        onSaveToBrain={onSaveToBrain}
      />

      {/* Premium Border Gradient on Hover */}
      <div className="absolute inset-0 rounded-3xl border-2 border-transparent group-hover:border-primary/50 transition-colors z-40 pointer-events-none"></div>
    </div>
  );
}
