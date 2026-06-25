import React from 'react';
import { ExternalLink, Tag } from 'lucide-react';

interface Props {
  items: any[];
  onSelect: (id: string) => void;
}

export default function CollectionList({ items, onSelect }: Props) {
  return (
    <div className="p-6 md:p-10 max-w-5xl mx-auto">
      <div className="bg-card border border-border rounded-2xl overflow-hidden shadow-sm">
        {items.map((item, index) => (
          <div 
            key={item.id} 
            onClick={() => onSelect(item.id)}
            className={`group flex items-center gap-4 p-4 hover:bg-muted/50 cursor-pointer transition-colors ${
              index !== items.length - 1 ? 'border-b border-border/50' : ''
            }`}
          >
            {item.thumbnailUrl ? (
              <img src={item.thumbnailUrl} alt="" className="w-16 h-16 rounded-lg object-cover bg-muted" />
            ) : (
              <div className="w-16 h-16 rounded-lg bg-muted flex items-center justify-center">
                <ExternalLink className="text-muted-foreground opacity-50" />
              </div>
            )}
            
            <div className="flex-1 min-w-0">
              <h3 className="font-semibold text-foreground truncate group-hover:text-primary transition-colors">
                {item.title || item.url}
              </h3>
              <p className="text-sm text-muted-foreground line-clamp-1 mt-1">
                {item.summary || item.url}
              </p>
            </div>
            
            <div className="hidden sm:flex items-center gap-2 text-xs text-muted-foreground">
              {item.tags?.slice(0, 2).map((t: string) => (
                <span key={t} className="bg-background px-2 py-1 rounded-md border border-border/50 flex items-center gap-1">
                  <Tag size={10} /> {t}
                </span>
              ))}
            </div>
            
            <div className="text-xs text-muted-foreground whitespace-nowrap">
              {new Date(item.createdAt).toLocaleDateString()}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
