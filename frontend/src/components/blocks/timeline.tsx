import * as React from "react"
import { CalendarDays } from "lucide-react"

interface TimelineGroup {
  id: string
  title: string
  children: React.ReactNode
}

interface TimelineProps {
  groups: TimelineGroup[]
  className?: string
}

export function Timeline({ groups, className = "" }: TimelineProps) {
  return (
    <div className={`relative border-l-2 border-border/60 ml-4 md:ml-6 space-y-16 ${className}`}>
      {groups.map((group) => (
        <div key={group.id} className="relative">
          {/* Timeline Node & Header */}
          <div className="flex items-center mb-6">
            <div className="absolute -left-[9px] bg-background border-2 border-primary w-4 h-4 rounded-full" />
            <div className="ml-8 flex items-center gap-2 bg-secondary/80 px-4 py-2 rounded-xl border border-border/50 shadow-sm text-foreground">
              <CalendarDays size={18} className="text-primary" />
              <h2 className="text-lg font-bold tracking-tight">{group.title}</h2>
            </div>
          </div>

          {/* Group Content */}
          <div className="ml-8 pl-2">
            {group.children}
          </div>
        </div>
      ))}

      {/* End of Timeline Marker */}
      <div className="relative pt-8 pb-12">
        <div className="absolute -left-[9px] bg-background border-2 border-muted-foreground w-4 h-4 rounded-full" />
        <div className="ml-8 text-muted-foreground text-sm font-medium italic">
          End of timeline.
        </div>
      </div>
    </div>
  )
}
