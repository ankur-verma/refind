import { CheckCircle2, Loader2, Link as LinkIcon, AlertCircle } from 'lucide-react';
import { Skeleton } from '../../../components/ui/skeleton';

export interface QueuedItem {
  id: string; // Temporary ID or backend GUID
  url: string;
  status: 'detecting' | 'processing' | 'completed' | 'error' | 'duplicate';
  errorMsg?: string;
  data?: any; // For enriched data later
}

export default function ProcessingCard({ item }: { item: QueuedItem }) {
  if (item.status === 'completed') {
    return (
      <div className="flex items-center gap-4 p-4 rounded-xl border border-green-500/30 bg-green-500/10 animate-in slide-in-from-bottom-2">
        <div className="flex items-center justify-center w-10 h-10 rounded-full bg-green-500/20 text-green-500">
          <CheckCircle2 size={24} />
        </div>
        <div className="flex-1 min-w-0">
          <h4 className="text-sm font-semibold text-foreground truncate">Saved Successfully</h4>
          <p className="text-xs text-muted-foreground truncate">{item.url}</p>
        </div>
        <div className="text-xs font-medium text-green-500 bg-green-500/10 px-2 py-1 rounded-full">Enriched</div>
      </div>
    );
  }

  if (item.status === 'error' || item.status === 'duplicate') {
    return (
      <div className="flex items-center gap-4 p-4 rounded-xl border border-red-500/30 bg-red-500/10 animate-in slide-in-from-bottom-2">
        <div className="flex items-center justify-center w-10 h-10 rounded-full bg-red-500/20 text-red-500">
          <AlertCircle size={24} />
        </div>
        <div className="flex-1 min-w-0">
          <h4 className="text-sm font-semibold text-foreground truncate">{item.status === 'duplicate' ? 'Already Saved' : 'Ingestion Failed'}</h4>
          <p className="text-xs text-muted-foreground truncate">{item.errorMsg || item.url}</p>
        </div>
      </div>
    );
  }

  // Processing or Detecting State
  return (
    <div className="relative overflow-hidden flex flex-col gap-3 p-4 rounded-xl border border-border bg-card/60 animate-in slide-in-from-bottom-2">
      <div className="flex items-center gap-3">
        <div className="flex items-center justify-center w-8 h-8 rounded-full bg-primary/20 text-primary">
          <Loader2 size={18} className="animate-spin" />
        </div>
        <div className="flex-1 min-w-0">
          <h4 className="text-sm font-medium text-foreground flex items-center gap-2">
            {item.status === 'detecting' ? 'Analyzing URL...' : 'AI Processing Pipeline...'}
          </h4>
          <p className="text-xs text-muted-foreground truncate">{item.url}</p>
        </div>
      </div>

      {/* AI Scanning Skeleton Animation */}
      {item.status === 'processing' && (
        <div className="relative mt-2 p-3 rounded-lg bg-background/50 border border-border/50">
          {/* Scanning Laser Effect */}
          <div className="absolute top-0 bottom-0 left-0 w-full bg-gradient-to-b from-transparent via-primary/20 to-transparent animate-scan z-10 pointer-events-none"></div>
          
          <div className="space-y-2">
            <Skeleton className="h-4 w-3/4" />
            <Skeleton className="h-3 w-full" />
            <Skeleton className="h-3 w-5/6" />
            <div className="flex gap-2 mt-4">
              <Skeleton className="h-6 w-16 rounded-full" />
              <Skeleton className="h-6 w-20 rounded-full" />
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
