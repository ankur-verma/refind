import React, { useMemo } from 'react';
import { Calendar } from 'lucide-react';

interface Props {
  items: any[];
  onSelect: (id: string) => void;
}

export default function CollectionTimeline({ items, onSelect }: Props) {
  // Sort items chronologically by creation date (descending)
  const sortedItems = useMemo(() => {
    return [...items].sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
  }, [items]);

  return (
    <div className="p-6 md:p-10 max-w-3xl mx-auto">
      <div className="relative border-l-2 border-primary/20 ml-4 md:ml-6 space-y-10">
        {sortedItems.map((item, idx) => {
          const date = new Date(item.createdAt);
          const month = date.toLocaleString('default', { month: 'short' });
          const day = date.getDate();

          return (
            <div key={item.id} className="relative pl-8 md:pl-12 group cursor-pointer" onClick={() => onSelect(item.id)}>
              {/* Timeline Node */}
              <div className="absolute left-[-11px] top-1 w-5 h-5 rounded-full bg-background border-4 border-primary group-hover:scale-125 group-hover:bg-primary transition-all"></div>
              
              <div className="flex flex-col md:flex-row gap-4 md:gap-6">
                {/* Date Marker */}
                <div className="flex-none flex flex-col items-start md:w-16">
                  <span className="text-sm font-bold text-primary">{month} {day}</span>
                  <span className="text-xs text-muted-foreground">{date.getFullYear()}</span>
                </div>

                {/* Content Card */}
                <div className="flex-1 bg-card/50 hover:bg-card border border-border rounded-2xl p-4 shadow-sm group-hover:shadow-md group-hover:-translate-y-1 transition-all">
                  <div className="flex items-start gap-4">
                    {item.thumbnailUrl ? (
                      <img src={item.thumbnailUrl} alt="" className="w-20 h-20 rounded-lg object-cover bg-muted flex-none" />
                    ) : (
                      <div className="w-20 h-20 rounded-lg bg-muted flex items-center justify-center text-muted-foreground opacity-50 flex-none">
                        <Calendar size={24} />
                      </div>
                    )}
                    
                    <div className="min-w-0">
                      <h3 className="font-semibold text-lg text-foreground truncate">{item.title || 'Untitled'}</h3>
                      <p className="text-sm text-muted-foreground line-clamp-2 mt-1">{item.summary || item.url}</p>
                      
                      <div className="mt-3 flex gap-2">
                        {item.tags?.slice(0, 3).map((t: string) => (
                          <span key={t} className="text-[10px] font-medium bg-secondary text-secondary-foreground px-2 py-0.5 rounded-full">
                            {t}
                          </span>
                        ))}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}
