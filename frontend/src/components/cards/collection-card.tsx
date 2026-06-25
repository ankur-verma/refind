import * as React from "react"
import { FolderHeart, Play } from "lucide-react"

interface CollectionCardProps {
  title: string
  itemCount: number
  colorClass?: string
  onClick?: () => void
  className?: string
}

export function CollectionCard({ title, itemCount, colorClass = "bg-purple-500/20 text-purple-500", onClick, className = "" }: CollectionCardProps) {
  return (
    <div 
      onClick={onClick}
      className={`relative overflow-hidden rounded-2xl bg-card border border-border p-6 cursor-pointer group transition-all hover:-translate-y-1 hover:shadow-lg hover:border-primary/50 ${className}`}
    >
      <div className={`w-12 h-12 rounded-xl flex items-center justify-center mb-4 transition-transform group-hover:scale-110 ${colorClass}`}>
        <FolderHeart size={24} />
      </div>
      
      <span className="text-[10px] uppercase tracking-wider font-bold text-muted-foreground mb-1 block">
        {itemCount} Items
      </span>
      
      <h3 className="text-xl font-semibold leading-tight text-foreground mb-6">
        {title}
      </h3>
      
      <div className="flex items-center gap-2 text-sm font-medium text-primary opacity-80 group-hover:opacity-100 transition-opacity">
        <Play size={16} fill="currentColor" /> Play Collection
      </div>
    </div>
  )
}
