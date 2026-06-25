import React from 'react';
import { ExternalLink, Sparkles, MessageSquarePlus, Archive, Share2, Zap, BookmarkPlus } from 'lucide-react';

interface Props {
  url?: string;
  isDiscovery?: boolean;
  onAskAi: (e: React.MouseEvent) => void;
  onAddNote: (e: React.MouseEvent) => void;
  onArchive: (e: React.MouseEvent) => void;
  onShare: (e: React.MouseEvent) => void;
  onRevisit: (e: React.MouseEvent) => void;
  onSaveToBrain?: (e: React.MouseEvent) => void;
}

export default function CardActions({ url, isDiscovery, onAskAi, onAddNote, onArchive, onShare, onRevisit, onSaveToBrain }: Props) {
  const ActionButton = ({ icon: Icon, label, onClick, className = '' }: any) => (
    <button 
      onClick={onClick}
      className={`flex flex-col items-center justify-center gap-1 p-2 rounded-xl hover:bg-secondary/50 transition-colors text-muted-foreground hover:text-foreground ${className}`}
      title={label}
    >
      <Icon size={18} />
      <span className="text-[9px] font-semibold tracking-wide whitespace-nowrap uppercase">{label}</span>
    </button>
  );

  return (
    <div className="absolute inset-x-0 bottom-0 z-30 bg-background/95 translate-y-full group-hover:translate-y-0 transition-transform duration-300 ease-in-out flex items-center justify-center p-3 border-t border-border shadow-2xl">
      <div className="flex justify-around items-center w-full px-1">
        {url && (
          <a 
            href={url} 
            target="_blank" 
            rel="noopener noreferrer"
            onClick={(e) => e.stopPropagation()}
            className="flex flex-col items-center justify-center gap-1 p-2 rounded-xl hover:bg-secondary/50 transition-colors text-muted-foreground hover:text-foreground"
            title="Open"
          >
            <ExternalLink size={18} />
            <span className="text-[9px] font-semibold tracking-wide whitespace-nowrap uppercase">Open</span>
          </a>
        )}
        
        {isDiscovery && onSaveToBrain && (
          <ActionButton icon={BookmarkPlus} label="Save" onClick={onSaveToBrain} className="text-green-500 hover:text-green-400" />
        )}
        
        <ActionButton icon={Sparkles} label="Ask AI" onClick={onAskAi} className="text-purple-400 hover:text-purple-300" />
        
        {!isDiscovery && (
          <>
            <ActionButton icon={MessageSquarePlus} label="Note" onClick={onAddNote} />
            <ActionButton icon={Zap} label="Revisit" onClick={onRevisit} className="text-yellow-500 hover:text-yellow-400" />
            <ActionButton icon={Share2} label="Share" onClick={onShare} />
            <ActionButton icon={Archive} label="Archive" onClick={onArchive} />
          </>
        )}
      </div>
    </div>
  );
}
