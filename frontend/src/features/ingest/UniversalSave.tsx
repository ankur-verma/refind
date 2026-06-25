import { useState, useEffect, DragEvent } from 'react';
import { Plus, Link as LinkIcon, UploadCloud, Globe, Play, Camera, FileText, Briefcase } from 'lucide-react';
import { request } from '../../api';
import ProcessingCard, { QueuedItem } from './components/ProcessingCard';

export default function UniversalSave() {
  const [inputText, setInputText] = useState('');
  const [queue, setQueue] = useState<QueuedItem[]>([]);
  const [isDragging, setIsDragging] = useState(false);

  // Listen for SignalR ContentProcessed events
  useEffect(() => {
    const handleContentProcessed = (event: Event) => {
      const customEvent = event as CustomEvent;
      const data = customEvent.detail;
      
      if (data && data.id) {
        setQueue(prev => prev.map(item => {
          if (item.id === data.id) {
            return {
              ...item,
              status: data.status === 1 ? 'completed' : 'error',
              errorMsg: data.status === 2 ? 'AI Processing Pipeline Failed' : undefined
            };
          }
          return item;
        }));
      }
    };

    window.addEventListener('content-processed', handleContentProcessed);
    return () => window.removeEventListener('content-processed', handleContentProcessed);
  }, []);

  const processUrl = async (url: string) => {
    // Add to queue as detecting
    const tempId = `temp-${Date.now()}-${Math.random()}`;
    const newItem: QueuedItem = { id: tempId, url, status: 'detecting' };
    
    setQueue(prev => [newItem, ...prev]);

    try {
      const res = await request('/content', { method: 'POST', body: JSON.stringify({ url }) });
      
      if (res.success && res.data) {
        // The backend returns the Guid in res.data (ApiResponse<Guid>.Value is usually mapped to data)
        // Actually, request usually maps ApiResponse to { success, data } if we destructure properly.
        // Wait, looking at ContentController.cs, it returns ApiResponse<Guid>.Ok(result.Value)
        const realId = res.data || res.value; 

        setQueue(prev => prev.map(item => 
          item.id === tempId ? { ...item, id: realId || tempId, status: 'processing' } : item
        ));
      } else if (res.isDuplicate) {
        setQueue(prev => prev.map(item => 
          item.id === tempId ? { ...item, status: 'duplicate' } : item
        ));
      } else {
        setQueue(prev => prev.map(item => 
          item.id === tempId ? { ...item, status: 'error', errorMsg: res.errors?.[0] || 'Submission failed' } : item
        ));
      }
    } catch (err) {
      setQueue(prev => prev.map(item => 
        item.id === tempId ? { ...item, status: 'error', errorMsg: 'Network error' } : item
      ));
    }
  };

  const handleInputSubmit = (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    if (!inputText.trim()) return;

    const urlRegex = /https?:\/\/[^\s$.?#].[^\s]*/gi;
    const matches = inputText.match(urlRegex) || [];
    
    if (matches.length > 0) {
      const validUrls = [...new Set(matches.map(url => url.replace(/[.,;:})\]]+$/, '')))];
      validUrls.forEach(url => processUrl(url));
      setInputText('');
    }
  };

  const handlePaste = (e: React.ClipboardEvent) => {
    // Automatically trigger submit if a URL is pasted
    setTimeout(() => {
      handleInputSubmit();
    }, 50);
  };

  const handleDragOver = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(true);
  };

  const handleDragLeave = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(false);
  };

  const handleDrop = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(false);
    
    const textData = e.dataTransfer.getData('text');
    if (textData) {
      setInputText(textData);
      setTimeout(() => handleInputSubmit(), 50);
    }
  };

  return (
    <div 
      className={`relative min-h-[80vh] p-6 transition-colors duration-300 ${isDragging ? 'bg-primary/5' : ''}`}
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
    >
      {/* Drag Overlay */}
      {isDragging && (
        <div className="absolute inset-0 z-50 flex items-center justify-center bg-background/80 border-2 border-dashed border-primary rounded-xl m-6 pointer-events-none">
          <div className="flex flex-col items-center text-primary">
            <UploadCloud size={64} className="mb-4 animate-bounce" />
            <h2 className="text-3xl font-bold">Drop to Ingest</h2>
            <p className="text-muted-foreground mt-2">URLs will be parsed and processed instantly.</p>
          </div>
        </div>
      )}

      <div className="max-w-4xl mx-auto space-y-12">
        <header className="text-center space-y-4 pt-8">
          <h1 className="text-4xl md:text-5xl font-extrabold tracking-tight bg-gradient-to-r from-primary to-accent bg-clip-text text-transparent">
            Universal Save
          </h1>
          <p className="text-xl text-muted-foreground">
            Paste a link, drop a file, or sync a platform.
          </p>
        </header>

        {/* Smart Input Bar */}
        <form onSubmit={handleInputSubmit} className="relative max-w-2xl mx-auto group">
          <div className="absolute inset-y-0 left-4 flex items-center pointer-events-none text-muted-foreground group-focus-within:text-primary transition-colors">
            <LinkIcon size={24} />
          </div>
          <input 
            type="text"
            className="w-full pl-14 pr-16 py-5 rounded-2xl bg-card border-2 border-border focus:border-primary/50 focus:ring-4 focus:ring-primary/10 transition-all outline-none text-lg shadow-xl"
            placeholder="Paste a URL or drop content here..."
            value={inputText}
            onChange={(e) => setInputText(e.target.value)}
            onPaste={handlePaste}
          />
          <button 
            type="submit"
            className="absolute inset-y-2 right-2 px-4 bg-primary text-primary-foreground rounded-xl font-medium hover:bg-primary/90 transition-transform active:scale-95 disabled:opacity-50 disabled:pointer-events-none"
            disabled={!inputText.trim()}
          >
            Save
          </button>
        </form>

        {/* Queue Status */}
        {queue.length > 0 && (
          <div className="space-y-4 animate-in fade-in">
            <h3 className="text-lg font-semibold text-foreground flex items-center gap-2">
              Processing Queue <span className="bg-primary/20 text-primary px-2 py-0.5 rounded-full text-xs">{queue.length}</span>
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {queue.map(item => (
                <ProcessingCard key={item.id} item={item} />
              ))}
            </div>
          </div>
        )}

        {/* Platform Integration Area */}
        {!queue.length && (
          <section className="pt-8 border-t border-border/50">
            <h2 className="text-lg font-semibold mb-6">Connect Data Sources</h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
              {[
                { name: 'Browser Extension', icon: Globe, bg: 'bg-blue-500', desc: 'Save with one click' },
                { name: 'YouTube', icon: Play, bg: 'bg-red-500', desc: 'Sync liked videos' },
                { name: 'Instagram', icon: Camera, bg: 'bg-pink-500', desc: 'Upload JSON export' },
                { name: 'LinkedIn', icon: Briefcase, bg: 'bg-sky-500', desc: 'Upload CSV export' }
              ].map(source => (
                <div key={source.name} className="flex flex-col items-center text-center p-6 rounded-2xl border border-border bg-card/50 hover:bg-card hover:shadow-xl hover:-translate-y-1 transition-all cursor-pointer group">
                  <div className={`w-12 h-12 rounded-full ${source.bg} text-white flex items-center justify-center mb-4 group-hover:scale-110 transition-transform shadow-lg`}>
                    <source.icon size={24} />
                  </div>
                  <h3 className="font-semibold text-foreground">{source.name}</h3>
                  <p className="text-xs text-muted-foreground mt-2">{source.desc}</p>
                </div>
              ))}
            </div>
          </section>
        )}
      </div>
    </div>
  );
}
