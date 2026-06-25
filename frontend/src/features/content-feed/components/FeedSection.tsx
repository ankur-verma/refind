import React, { useRef } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';

interface FeedSectionProps {
  title: string;
  subtitle?: string;
  children: React.ReactNode;
  emptyMessage?: string;
}

export default function FeedSection({ title, subtitle, children, emptyMessage = 'No items found.' }: FeedSectionProps) {
  const scrollContainerRef = useRef<HTMLDivElement>(null);

  const scroll = (direction: 'left' | 'right') => {
    if (scrollContainerRef.current) {
      const scrollAmount = window.innerWidth > 768 ? 600 : 300;
      scrollContainerRef.current.scrollBy({
        left: direction === 'left' ? -scrollAmount : scrollAmount,
        behavior: 'smooth'
      });
    }
  };

  const hasChildren = React.Children.count(children) > 0;

  if (!hasChildren) return null;

  return (
    <section className="mb-10 relative group/section">
      {/* Header */}
      <div className="px-6 mb-4 flex items-end justify-between">
        <div>
          <h2 className="text-2xl font-bold tracking-tight text-foreground">{title}</h2>
          {subtitle && <p className="text-sm text-muted-foreground mt-1">{subtitle}</p>}
        </div>
      </div>

      {/* Scrolling Container */}
      <div className="relative">
        {/* Left Scroll Button */}
        <button 
          onClick={() => scroll('left')}
          className="hidden md:flex absolute left-2 top-1/2 -translate-y-1/2 z-30 w-10 h-10 items-center justify-center rounded-full bg-background/80 border border-border text-foreground opacity-0 group-hover/section:opacity-100 transition-opacity disabled:opacity-0 shadow-lg"
          aria-label="Scroll left"
        >
          <ChevronLeft size={24} />
        </button>

        <div 
          ref={scrollContainerRef}
          className="flex gap-4 overflow-x-auto snap-x snap-mandatory px-6 pb-4 pt-2 -mt-2 hide-scrollbar"
          style={{ scrollbarWidth: 'none', msOverflowStyle: 'none' }}
        >
          {children}
        </div>

        {/* Right Scroll Button */}
        <button 
          onClick={() => scroll('right')}
          className="hidden md:flex absolute right-2 top-1/2 -translate-y-1/2 z-30 w-10 h-10 items-center justify-center rounded-full bg-background/80 border border-border text-foreground opacity-0 group-hover/section:opacity-100 transition-opacity shadow-lg"
          aria-label="Scroll right"
        >
          <ChevronRight size={24} />
        </button>

        {/* Gradient Fades for edges */}
        <div className="absolute top-0 bottom-0 left-0 w-8 bg-gradient-to-r from-background to-transparent z-20 pointer-events-none"></div>
        <div className="absolute top-0 bottom-0 right-0 w-8 bg-gradient-to-l from-background to-transparent z-20 pointer-events-none"></div>
      </div>
    </section>
  );
}
