import React from 'react';
import DiscoverFeed from '../features/discover/DiscoverFeed';
import { BrainCircuit } from 'lucide-react';

export default function DiscoverPage() {
  return (
    <div className="p-6 md:p-8 lg:p-12 max-w-[1600px] mx-auto min-h-screen">
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-4 mb-8">
        <div>
          <h1 className="text-4xl font-extrabold tracking-tight text-foreground mb-2">Discover</h1>
          <p className="text-lg text-muted-foreground">Find your next favorite place, product, or idea.</p>
        </div>
        
        {/* AI Transparency Badge */}
        <div className="flex items-center gap-2 bg-primary/10 border border-primary/20 rounded-xl px-4 py-2 self-start md:self-auto">
          <BrainCircuit size={16} className="text-primary animate-pulse" />
          <span className="text-sm font-medium text-primary">Curated based on your interests & activity</span>
        </div>
      </div>

      <DiscoverFeed />
    </div>
  );
}
