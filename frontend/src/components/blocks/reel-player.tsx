import * as React from "react"
import { Bookmark, Share2, MessageSquare, BrainCircuit, Play } from "lucide-react"

export interface ReelData {
  id: string
  videoUrl: string
  title: string
  description?: string
  author?: string
}

interface ReelPlayerProps {
  reel: ReelData
  isActive: boolean
  onAction?: (action: 'bookmark' | 'share' | 'comment' | 'ai', reelId: string) => void
}

export function ReelPlayer({ reel, isActive, onAction }: ReelPlayerProps) {
  const videoRef = React.useRef<HTMLVideoElement>(null)
  const [isPlaying, setIsPlaying] = React.useState(isActive)

  React.useEffect(() => {
    if (isActive) {
      videoRef.current?.play().catch(() => setIsPlaying(false))
      setIsPlaying(true)
    } else {
      videoRef.current?.pause()
      setIsPlaying(false)
    }
  }, [isActive])

  const togglePlay = () => {
    if (videoRef.current) {
      if (isPlaying) {
        videoRef.current.pause()
        setIsPlaying(false)
      } else {
        videoRef.current.play()
        setIsPlaying(true)
      }
    }
  }

  return (
    <div className="relative w-full h-full bg-black flex items-center justify-center overflow-hidden group">
      <video
        ref={videoRef}
        src={reel.videoUrl}
        className="w-full h-full object-cover"
        loop
        playsInline
        muted
        onClick={togglePlay}
      />
      
      {/* Play/Pause Overlay */}
      {!isPlaying && (
        <div className="absolute inset-0 flex items-center justify-center pointer-events-none bg-black/20">
          <div className="w-16 h-16 bg-black/50 rounded-full flex items-center justify-center ">
            <Play size={32} className="text-white ml-2" />
          </div>
        </div>
      )}

      {/* Info Overlay */}
      <div className="absolute bottom-0 left-0 right-16 p-4 bg-gradient-to-t from-black/80 via-black/40 to-transparent pointer-events-none">
        <h3 className="text-white font-bold text-lg mb-1 drop-shadow-md">{reel.title}</h3>
        {reel.author && <p className="text-white/80 text-sm font-medium">@{reel.author}</p>}
        {reel.description && <p className="text-white/90 text-sm mt-2 line-clamp-2">{reel.description}</p>}
      </div>

      {/* Floating Action Bar */}
      <div className="absolute right-4 bottom-8 flex flex-col gap-6 items-center z-10">
        <ActionButton icon={<Bookmark size={24} />} label="Save" onClick={() => onAction?.('bookmark', reel.id)} />
        <ActionButton icon={<MessageSquare size={24} />} label="Note" onClick={() => onAction?.('comment', reel.id)} />
        <ActionButton icon={<Share2 size={24} />} label="Share" onClick={() => onAction?.('share', reel.id)} />
        <ActionButton 
          icon={<BrainCircuit size={24} />} 
          label="Ask AI" 
          onClick={() => onAction?.('ai', reel.id)} 
          className="text-primary hover:text-primary animate-pulse-slow"
        />
      </div>
    </div>
  )
}

function ActionButton({ icon, label, onClick, className = "" }: { icon: React.ReactNode, label: string, onClick: () => void, className?: string }) {
  return (
    <button 
      onClick={(e) => { e.stopPropagation(); onClick(); }}
      className={`flex flex-col items-center gap-1 text-white/90 hover:text-white transition-colors drop-shadow-lg ${className}`}
    >
      <div className="p-3 bg-black/20 rounded-full active:scale-95 transition-transform">
        {icon}
      </div>
      <span className="text-[11px] font-semibold">{label}</span>
    </button>
  )
}
