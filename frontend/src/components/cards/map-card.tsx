import * as React from "react"
import { MapPin, ArrowUpRight } from "lucide-react"

interface Location {
  name: string
  lat: number
  lng: number
}

interface MapCardProps {
  locations: Location[]
  className?: string
}

export function MapCard({ locations, className = "" }: MapCardProps) {
  // Placeholder map graphic since we aren't loading Google Maps scripts in this library component
  return (
    <div className={`rounded-xl border border-border bg-card overflow-hidden shadow-sm ${className}`}>
      <div className="relative h-48 w-full bg-secondary">
        <div className="absolute inset-0 opacity-20 bg-[radial-gradient(ellipse_at_center,_var(--tw-gradient-stops))] from-primary via-background to-background" />
        
        {/* Render Fake Pins */}
        {locations.map((loc, i) => (
          <div 
            key={i} 
            className="absolute -translate-x-1/2 -translate-y-1/2 flex flex-col items-center animate-slide-up"
            style={{ 
              top: `${40 + (i * 15)}%`, 
              left: `${30 + (i * 20)}%` 
            }}
          >
            <div className="bg-primary text-primary-foreground text-[10px] font-bold px-2 py-0.5 rounded-full mb-1 shadow-md">
              {loc.name}
            </div>
            <MapPin size={24} className="text-primary drop-shadow-md" fill="currentColor" />
          </div>
        ))}
      </div>
      <div className="p-3 bg-card flex justify-between items-center border-t border-border">
        <span className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
          {locations.length} Locations Mapped
        </span>
        <button className="text-primary hover:bg-primary/10 p-1.5 rounded-md transition-colors">
          <ArrowUpRight size={16} />
        </button>
      </div>
    </div>
  )
}
