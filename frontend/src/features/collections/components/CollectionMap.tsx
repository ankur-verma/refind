import React, { useMemo } from 'react';
import { MapPin } from 'lucide-react';

interface Props {
  items: any[];
  onSelect: (id: string) => void;
}

export default function CollectionMap({ items, onSelect }: Props) {
  // Generate deterministic "mock" coordinates for items to spread them out on the UI map
  const mapItems = useMemo(() => {
    return items.map((item, i) => {
      // Deterministic pseudo-randomness based on ID length or index
      const seed = item.id.length + i;
      const top = 10 + (seed * 17) % 75; // 10% to 85%
      const left = 10 + (seed * 23) % 80; // 10% to 90%
      return { ...item, top, left };
    });
  }, [items]);

  return (
    <div className="relative w-full h-[calc(100vh-140px)] bg-muted/20 overflow-hidden">
      {/* Aesthetic Map Background Grid */}
      <div 
        className="absolute inset-0 opacity-10 pointer-events-none"
        style={{
          backgroundImage: 'linear-gradient(to right, #888 1px, transparent 1px), linear-gradient(to bottom, #888 1px, transparent 1px)',
          backgroundSize: '40px 40px'
        }}
      ></div>

      {mapItems.map((item) => (
        <div 
          key={item.id}
          className="absolute transform -translate-x-1/2 -translate-y-full cursor-pointer group"
          style={{ top: `${item.top}%`, left: `${item.left}%` }}
          onClick={() => onSelect(item.id)}
        >
          {/* Map Pin */}
          <div className="relative z-10 flex flex-col items-center">
            <div className="w-8 h-8 bg-primary rounded-full flex items-center justify-center text-primary-foreground shadow-lg group-hover:scale-110 transition-transform">
              <MapPin size={16} />
            </div>
            <div className="w-1 h-3 bg-primary/50"></div>
          </div>

          {/* Hover Card */}
          <div className="absolute bottom-full mb-2 left-1/2 transform -translate-x-1/2 w-48 bg-card border border-border rounded-xl p-2 shadow-2xl opacity-0 group-hover:opacity-100 group-hover:translate-y-[-8px] transition-all pointer-events-none z-20">
            {item.thumbnailUrl && (
              <img src={item.thumbnailUrl} alt="" className="w-full h-24 object-cover rounded-md mb-2" />
            )}
            <h4 className="text-xs font-bold truncate text-foreground">{item.title || item.url}</h4>
            <p className="text-[10px] text-muted-foreground truncate">{item.summary}</p>
          </div>
        </div>
      ))}

      {/* Map Legend Overlay */}
      <div className="absolute bottom-6 right-6 bg-card/80 p-4 rounded-xl border border-border shadow-lg">
        <h4 className="text-sm font-semibold mb-2">Simulated Map</h4>
        <p className="text-xs text-muted-foreground max-w-[200px]">
          Location data is mocked. Connect a Maps API to plot real coordinates from extracted metadata.
        </p>
      </div>
    </div>
  );
}
