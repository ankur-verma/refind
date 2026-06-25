import * as React from "react"

// A simplified generic Sheet component acting as a Bottom Sheet on Mobile and a Modal on Desktop
export function Sheet({ 
  isOpen, 
  onClose, 
  children, 
  title, 
  description 
}: { 
  isOpen: boolean; 
  onClose: () => void; 
  children: React.ReactNode;
  title?: React.ReactNode;
  description?: React.ReactNode;
}) {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex justify-center items-end md:items-center bg-black/60 transition-opacity" onClick={onClose}>
      <div 
        className="w-full md:w-[600px] max-h-[90dvh] bg-card text-card-foreground md:rounded-2xl rounded-t-3xl overflow-hidden flex flex-col shadow-2xl transition-transform animate-in slide-in-from-bottom-10" 
        onClick={(e) => e.stopPropagation()}
      >
        {/* Mobile Drag Handle Indicator */}
        <div className="w-full flex justify-center pt-3 pb-1 md:hidden">
          <div className="w-12 h-1.5 bg-border rounded-full" />
        </div>

        {/* Header */}
        {(title || description) && (
          <div className="px-6 py-4 border-b border-border bg-background/50 flex flex-col gap-1">
            {title && <h2 className="text-xl font-semibold tracking-tight">{title}</h2>}
            {description && <p className="text-sm text-muted-foreground">{description}</p>}
          </div>
        )}

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-6 custom-scrollbar">
          {children}
        </div>
      </div>
    </div>
  );
}
