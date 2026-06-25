import * as React from "react"
import { ChevronLeft, ChevronRight } from "lucide-react"
import { Button } from "../ui/button"

interface CarouselProps {
  children: React.ReactNode
  title?: string
  className?: string
}

export function Carousel({ children, title, className = "" }: CarouselProps) {
  const scrollRef = React.useRef<HTMLDivElement>(null)

  const scroll = (direction: 'left' | 'right') => {
    if (scrollRef.current) {
      const scrollAmount = 300
      scrollRef.current.scrollBy({
        left: direction === 'left' ? -scrollAmount : scrollAmount,
        behavior: 'smooth'
      })
    }
  }

  return (
    <div className={`relative ${className}`}>
      {title && (
        <div className="flex items-center justify-between mb-4 px-2">
          <h3 className="text-lg font-semibold tracking-tight">{title}</h3>
          <div className="hidden md:flex gap-2">
            <Button variant="outline" size="icon" className="h-8 w-8 rounded-full" onClick={() => scroll('left')}>
              <ChevronLeft size={16} />
            </Button>
            <Button variant="outline" size="icon" className="h-8 w-8 rounded-full" onClick={() => scroll('right')}>
              <ChevronRight size={16} />
            </Button>
          </div>
        </div>
      )}
      
      <div 
        ref={scrollRef}
        className="flex gap-4 overflow-x-auto pb-4 snap-x snap-mandatory custom-scrollbar"
        style={{ scrollbarWidth: 'none', msOverflowStyle: 'none' }}
      >
        {React.Children.map(children, (child) => (
          <div className="snap-start flex-none">
            {child}
          </div>
        ))}
      </div>
    </div>
  )
}
