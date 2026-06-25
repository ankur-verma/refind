import React, { useMemo } from 'react';
import { useProfileActivityGraph } from '../api';
import { Activity } from 'lucide-react';

export default function ProfileActivityGraph() {
  const { data: activityRes, isLoading } = useProfileActivityGraph();

  const data = useMemo(() => {
    if (!activityRes || !activityRes.data) return [];
    return activityRes.data;
  }, [activityRes]);

  const hasData = data.some((d: any) => d.views > 0 || d.saves > 0 || d.searches > 0);

  if (isLoading) {
    return (
      <div className="bg-card border border-border rounded-3xl p-6 shadow-sm min-h-[300px] flex items-center justify-center text-muted-foreground animate-pulse">
        Loading activity data...
      </div>
    );
  }

  if (!hasData) {
    return (
      <div className="bg-card border border-border rounded-3xl p-6 shadow-sm min-h-[300px]">
        <div className="flex items-center gap-2 mb-6">
          <Activity className="text-primary" size={20} />
          <h2 className="text-lg font-bold">ML Activity Graph</h2>
        </div>
        <div className="flex justify-center items-center h-48 text-muted-foreground text-center text-sm leading-relaxed">
          No activity data tracked yet.<br/>Your interactions over the last 14 days will appear here.
        </div>
      </div>
    );
  }

  // --- SVG Chart Calculations ---
  const maxVal = Math.max(...data.map((d: any) => d.views + d.saves + d.searches), 5); // Ensure non-zero max
  const chartHeight = 200;
  const chartWidth = 1000;
  const xStep = chartWidth / (data.length - 1 || 1);

  // Helper to generate path strings
  const generatePath = (key: string, offsetKey?: string) => {
    return data.map((d: any, i: number) => {
      const val = d[key] + (offsetKey ? d[offsetKey] : 0);
      const x = i * xStep;
      const y = chartHeight - (val / maxVal) * chartHeight;
      return `${i === 0 ? 'M' : 'L'} ${x} ${y}`;
    }).join(' ');
  };

  const viewsPath = generatePath('views');
  const savesPath = generatePath('saves', 'views');
  const searchesPath = generatePath('searches');

  return (
    <div className="bg-card border border-border rounded-3xl p-6 shadow-sm min-h-[300px] w-full overflow-hidden">
      <div className="flex items-center gap-2 mb-8">
        <Activity className="text-primary" size={20} />
        <h2 className="text-lg font-bold">ML Activity Graph (14 Days)</h2>
      </div>

      <div className="relative w-full h-[200px]">
        {/* Y-Axis Guidelines */}
        <div className="absolute inset-0 flex flex-col justify-between opacity-10 pointer-events-none">
          <div className="border-t border-foreground w-full"></div>
          <div className="border-t border-foreground w-full"></div>
          <div className="border-t border-foreground w-full"></div>
          <div className="border-t border-foreground w-full"></div>
        </div>

        {/* The SVG Chart */}
        <svg 
          viewBox={`0 -10 ${chartWidth} ${chartHeight + 20}`} 
          preserveAspectRatio="none" 
          className="absolute inset-0 w-full h-full overflow-visible"
        >
          <defs>
            <linearGradient id="gradientViews" x1="0" y1="0" x2="0" y2="1">
              <stop offset="5%" stopColor="#3b82f6" stopOpacity={0.4}/>
              <stop offset="95%" stopColor="#3b82f6" stopOpacity={0}/>
            </linearGradient>
            <linearGradient id="gradientSaves" x1="0" y1="0" x2="0" y2="1">
              <stop offset="5%" stopColor="#10b981" stopOpacity={0.4}/>
              <stop offset="95%" stopColor="#10b981" stopOpacity={0}/>
            </linearGradient>
            <linearGradient id="gradientSearches" x1="0" y1="0" x2="0" y2="1">
              <stop offset="5%" stopColor="#8b5cf6" stopOpacity={0.4}/>
              <stop offset="95%" stopColor="#8b5cf6" stopOpacity={0}/>
            </linearGradient>
          </defs>

          {/* Saves Area (stacked hypothetically) */}
          <path 
            d={`${savesPath} L ${chartWidth} ${chartHeight} L 0 ${chartHeight} Z`} 
            fill="url(#gradientSaves)" 
            stroke="#10b981" 
            strokeWidth="2" 
            vectorEffect="non-scaling-stroke"
          />

          {/* Views Area */}
          <path 
            d={`${viewsPath} L ${chartWidth} ${chartHeight} L 0 ${chartHeight} Z`} 
            fill="url(#gradientViews)" 
            stroke="#3b82f6" 
            strokeWidth="2"
            vectorEffect="non-scaling-stroke"
          />

          {/* Searches Area */}
          <path 
            d={`${searchesPath} L ${chartWidth} ${chartHeight} L 0 ${chartHeight} Z`} 
            fill="url(#gradientSearches)" 
            stroke="#8b5cf6" 
            strokeWidth="2"
            vectorEffect="non-scaling-stroke"
          />
          
          {/* Data Points Tooltips / Dots */}
          {data.map((d: any, i: number) => {
            const x = i * xStep;
            const y = chartHeight - (d.views / maxVal) * chartHeight;
            return (
              <circle key={i} cx={x} cy={y} r="4" fill="#3b82f6" className="transition-all hover:r-6 cursor-pointer">
                <title>{`${d.date}: ${d.views} views, ${d.saves} saves, ${d.searches} searches`}</title>
              </circle>
            );
          })}
        </svg>

        {/* X-Axis Labels */}
        <div className="absolute -bottom-8 left-0 right-0 flex justify-between text-xs text-muted-foreground">
          {data.map((d: any, i: number) => {
            // Show every other label to prevent crowding
            if (i % 2 !== 0 && i !== data.length - 1) return <div key={i} className="flex-1"></div>;
            return (
              <div key={i} className="flex-1 text-center" style={{ transform: `translateX(${(i / (data.length - 1)) * 100 - 50}%)` }}>
                {d.date}
              </div>
            );
          })}
        </div>
      </div>
      
      {/* Legend */}
      <div className="mt-12 flex justify-center gap-6 text-sm font-medium">
        <div className="flex items-center gap-2">
          <div className="w-3 h-3 rounded-full bg-blue-500"></div>
          <span className="text-foreground">Views & Clicks</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-3 h-3 rounded-full bg-emerald-500"></div>
          <span className="text-foreground">Saves & Bookmarks</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-3 h-3 rounded-full bg-purple-500"></div>
          <span className="text-foreground">AI Deep Dives</span>
        </div>
      </div>
    </div>
  );
}
