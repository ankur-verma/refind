import React from 'react';
import { MapPin, Navigation } from 'lucide-react';
import FeedCard from '../../content-feed/components/FeedCard';

interface ChatWidgetProps {
  data: any;
  onSelectContent?: (id: string) => void;
}

export function ChatMapWidget({ data }: ChatWidgetProps) {
  // A sleek, minimal map representation tailored for a chat bubble
  return (
    <div className="mt-3 w-full rounded-2xl overflow-hidden border border-border bg-card shadow-lg">
      <div className="relative h-48 bg-muted">
        {/* Simulated Map Background */}
        <div className="absolute inset-0 opacity-40 bg-[url('https://images.unsplash.com/photo-1524661135-423995f22d0b?q=80&w=800&auto=format&fit=crop')] bg-cover bg-center" />
        <div className="absolute inset-0 bg-gradient-to-b from-transparent to-background/90" />
        
        {/* Map Pins */}
        <div className="absolute top-1/2 left-1/3 -translate-x-1/2 -translate-y-1/2 flex flex-col items-center">
          <div className="bg-primary text-primary-foreground p-2 rounded-full shadow-lg mb-1 animate-bounce">
            <MapPin size={16} />
          </div>
          <div className="bg-background/80 text-foreground text-[10px] font-bold px-2 py-0.5 rounded shadow">
            {data.locations?.[0] || 'Gurgaon Cafe'}
          </div>
        </div>
        
        <div className="absolute top-1/4 right-1/4 flex flex-col items-center">
          <div className="bg-red-500 text-white p-1.5 rounded-full shadow-lg mb-1">
            <MapPin size={12} />
          </div>
        </div>
      </div>
      <div className="p-3 bg-card flex justify-between items-center">
        <div>
          <h4 className="text-sm font-bold text-foreground">Found 4 Locations</h4>
          <p className="text-xs text-muted-foreground">Tap pins to explore</p>
        </div>
        <button className="flex items-center gap-1.5 bg-secondary text-secondary-foreground px-3 py-1.5 rounded-lg text-xs font-semibold hover:bg-secondary/80 transition-colors">
          <Navigation size={12} /> Directions
        </button>
      </div>
    </div>
  );
}

export function ChatCarouselWidget({ data, onSelectContent }: ChatWidgetProps) {
  if (!data.items || data.items.length === 0) return null;
  
  return (
    <div className="mt-3 w-full -mx-4 px-4 pb-2 overflow-x-auto snap-x flex gap-4 no-scrollbar">
      {data.items.map((item: any) => (
        <div key={item.id} className="snap-start shrink-0" style={{ width: '240px' }}>
          <div className="scale-90 origin-top-left -mb-[10%]">
            <FeedCard item={item} onClick={onSelectContent || (() => {})} />
          </div>
        </div>
      ))}
    </div>
  );
}
