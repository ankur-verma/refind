import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { Map, Coffee, Compass, Users, ShoppingBag, Plane, Sparkles, Pin, DownloadCloud, Loader2 } from 'lucide-react';
import { useCollections, useGenerateCollections } from '../features/collections/api';
import ImportContentModal from '../features/collections/components/ImportContentModal';
import { queryClient } from '../api';

const CATEGORY_ICONS: Record<string, any> = {
  'food': Coffee,
  'travel': Map,
  'leisure': Compass,
  'family': Users,
  'shopping': ShoppingBag,
  'default': Sparkles
};

const CATEGORY_COLORS: Record<string, { bg: string, color: string }> = {
  'food': { bg: 'from-orange-500/20 to-red-500/20', color: 'text-orange-500' },
  'travel': { bg: 'from-blue-500/20 to-cyan-500/20', color: 'text-blue-500' },
  'leisure': { bg: 'from-green-500/20 to-emerald-500/20', color: 'text-green-500' },
  'family': { bg: 'from-purple-500/20 to-fuchsia-500/20', color: 'text-purple-500' },
  'shopping': { bg: 'from-pink-500/20 to-rose-500/20', color: 'text-pink-500' },
  'default': { bg: 'from-slate-500/20 to-gray-500/20', color: 'text-slate-500' }
};

export default function CollectionsPage() {
  const { data: collections, isLoading } = useCollections();
  const generateMutation = useGenerateCollections();
  const [showImportModal, setShowImportModal] = useState(false);

  const handleGenerate = () => {
    generateMutation.mutate();
  };

  const handleImportSuccess = (count: number) => {
    setShowImportModal(false);
    queryClient.invalidateQueries({ queryKey: ['feed'] });
    alert(`Successfully queued ${count} links for import! Our AI will analyze them shortly.`);
  };

  return (
    <div className="p-6 md:p-10 max-w-7xl mx-auto space-y-12 relative">
      <header className="space-y-4 flex flex-col md:flex-row md:items-start justify-between gap-6">
        <div>
          <h1 className="text-4xl md:text-5xl font-extrabold tracking-tight text-foreground">
            Smart Collections
          </h1>
          <p className="text-xl text-muted-foreground max-w-3xl mt-4">
            Your externalized brain, intelligently grouped. These collections evolve automatically based on your content.
          </p>
        </div>
        <div className="flex items-center gap-3 shrink-0 flex-wrap">
          <button 
            onClick={() => setShowImportModal(true)}
            className="flex items-center gap-2 bg-secondary text-foreground border border-border px-6 py-3 rounded-full font-semibold hover:bg-secondary/80 transition-colors"
          >
            <DownloadCloud size={20} />
            Bulk Import
          </button>
          <button 
            onClick={handleGenerate}
            disabled={generateMutation.isPending}
            className="flex items-center gap-2 bg-primary text-primary-foreground px-6 py-3 rounded-full font-semibold hover:bg-primary/90 transition-colors disabled:opacity-50"
          >
            {generateMutation.isPending ? (
              <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-primary-foreground"></div>
            ) : (
              <Sparkles size={20} />
            )}
            Generate
          </button>
        </div>
      </header>

      {showImportModal && (
        <ImportContentModal 
          onClose={() => setShowImportModal(false)} 
          onSuccess={handleImportSuccess} 
        />
      )}

      {isLoading ? (
        <div className="flex items-center justify-center py-20">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary"></div>
        </div>
      ) : collections?.length === 0 ? (
        <div className="relative flex flex-col items-center justify-center py-24 px-6 md:px-12 text-center rounded-3xl bg-secondary/20 border border-dashed border-border/60 overflow-hidden">
          <div className="absolute inset-0 bg-gradient-to-b from-transparent to-background/20 pointer-events-none"></div>
          
          <div className="relative z-10 flex flex-col items-center max-w-2xl">
            <div className="flex -space-x-4 mb-8">
              <div className="w-16 h-16 rounded-2xl bg-blue-500/10 border border-blue-500/20 flex items-center justify-center shadow-xl rotate-[-10deg]">
                <DownloadCloud className="text-blue-500" size={28} />
              </div>
              <div className="w-20 h-20 rounded-2xl bg-primary/10 border border-primary/20 flex items-center justify-center shadow-2xl z-10">
                <Sparkles className="text-primary" size={36} />
              </div>
              <div className="w-16 h-16 rounded-2xl bg-orange-500/10 border border-orange-500/20 flex items-center justify-center shadow-xl rotate-[10deg]">
                <Map className="text-orange-500" size={28} />
              </div>
            </div>

            <h2 className="text-3xl font-extrabold text-foreground mb-4">
              Your Knowledge, Unorganized
            </h2>
            <p className="text-lg text-muted-foreground mb-10 leading-relaxed">
              You haven't generated any collections yet. To get started, you can either let our AI organize your existing saved items automatically, or import your data from Instagram and YouTube!
            </p>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 w-full">
              <button
                onClick={() => setShowImportModal(true)}
                className="group flex flex-col items-start p-6 bg-card border border-border rounded-2xl hover:border-blue-500/50 hover:shadow-lg transition-all text-left"
              >
                <div className="p-3 bg-blue-500/10 text-blue-500 rounded-xl mb-4 group-hover:scale-110 transition-transform">
                  <DownloadCloud size={24} />
                </div>
                <h3 className="font-bold text-foreground text-lg mb-1">Bulk Import</h3>
                <p className="text-sm text-muted-foreground">Upload your YouTube CSV or Instagram export file.</p>
              </button>

              <button
                onClick={handleGenerate}
                disabled={generateMutation.isPending}
                className="group flex flex-col items-start p-6 bg-card border border-border rounded-2xl hover:border-primary/50 hover:shadow-lg transition-all text-left disabled:opacity-50 disabled:pointer-events-none"
              >
                <div className="p-3 bg-primary/10 text-primary rounded-xl mb-4 group-hover:scale-110 transition-transform">
                  {generateMutation.isPending ? <Loader2 size={24} className="animate-spin" /> : <Sparkles size={24} />}
                </div>
                <h3 className="font-bold text-foreground text-lg mb-1">Auto-Generate</h3>
                <p className="text-sm text-muted-foreground">Let AI scan your library and build smart groups.</p>
              </button>
            </div>
          </div>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {collections?.map((col) => {
            const Icon = CATEGORY_ICONS[col.category?.toLowerCase()] || CATEGORY_ICONS['default'];
            const colors = CATEGORY_COLORS[col.category?.toLowerCase()] || CATEGORY_COLORS['default'];
            
            return (
              <Link 
                key={col.id} 
                to={`/collections/${col.id}`}
                className="group relative flex flex-col p-6 rounded-3xl border border-border bg-card/40 overflow-hidden hover:shadow-2xl transition-all duration-300 hover:-translate-y-1"
              >
                {col.isPinned && (
                  <div className="absolute top-4 right-4 text-primary z-20 bg-background/50 p-1 rounded-full ">
                    <Pin size={16} className="fill-primary" />
                  </div>
                )}
                <div className={`absolute inset-0 bg-gradient-to-br ${colors.bg} opacity-50 group-hover:opacity-100 transition-opacity z-0`}></div>
                <div className="relative z-10 flex flex-col h-full">
                  <div className={`w-14 h-14 rounded-2xl bg-background/80 flex items-center justify-center mb-6 shadow-sm ${colors.color}`}>
                    <Icon size={28} />
                  </div>
                  <div className="flex items-center gap-2 mb-2">
                    <h2 className="text-2xl font-bold text-foreground group-hover:text-primary transition-colors">
                      {col.title}
                    </h2>
                    {col.isAutoGenerated && (
                      <Sparkles size={16} className="text-primary opacity-70" />
                    )}
                  </div>
                  <p className="text-muted-foreground flex-1">
                    {col.description || `${col.itemCount} saved items organized for you.`}
                  </p>
                  
                  {col.previewItems && col.previewItems.length > 0 && (
                    <div className="mt-6 flex -space-x-3">
                      {col.previewItems.map((item, idx) => (
                        <div key={idx} className="w-10 h-10 rounded-full border-2 border-background overflow-hidden bg-secondary">
                          {item.thumbnailUrl ? (
                            <img src={item.thumbnailUrl} alt="" className="w-full h-full object-cover" />
                          ) : (
                            <div className="w-full h-full flex items-center justify-center text-xs font-bold text-muted-foreground">
                              {item.title.charAt(0)}
                            </div>
                          )}
                        </div>
                      ))}
                    </div>
                  )}
                  
                  <div className="mt-6 flex items-center gap-2 text-sm font-semibold text-primary opacity-0 group-hover:opacity-100 transform translate-y-2 group-hover:translate-y-0 transition-all">
                    Explore Collection <span aria-hidden="true">&rarr;</span>
                  </div>
                </div>
              </Link>
            );
          })}
        </div>
      )}
    </div>
  );
}
