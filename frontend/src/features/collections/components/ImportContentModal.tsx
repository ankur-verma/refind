import React, { useState, useCallback, useEffect } from 'react';
import { Upload, Link as LinkIcon, Camera, PlayCircle, FileJson, FileText, CheckCircle2, AlertCircle, Loader2, Globe, Search, X, Sparkles } from 'lucide-react';
import { request } from '../../../api';

interface ImportContentModalProps {
  onClose: () => void;
  onSuccess: (count: number) => void;
}

export default function ImportContentModal({ onClose, onSuccess }: ImportContentModalProps) {
  const [isDragging, setIsDragging] = useState(false);
  const [file, setFile] = useState<File | null>(null);
  const [extractedUrls, setExtractedUrls] = useState<string[]>([]);
  const [isParsing, setIsParsing] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [singleUrl, setSingleUrl] = useState('');
  const [dynamicText, setDynamicText] = useState('anywhere');

  useEffect(() => {
    const words = ['Instagram', 'YouTube', 'websites', 'anywhere'];
    let i = 0;
    const interval = setInterval(() => {
      i = (i + 1) % words.length;
      setDynamicText(words[i]);
    }, 2500);
    return () => clearInterval(interval);
  }, []);

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(true);
  }, []);

  const handleDragLeave = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
  }, []);

  const parseInstagramJson = (json: any): string[] => {
    const urls: string[] = [];
    try {
      const searchForUrls = (obj: any) => {
        if (!obj) return;
        if (typeof obj === 'string' && obj.startsWith('https://www.instagram.com/')) {
          urls.push(obj);
        } else if (typeof obj === 'object') {
          for (const key in obj) {
            if (key === 'href' && typeof obj[key] === 'string') {
              urls.push(obj[key]);
            } else {
              searchForUrls(obj[key]);
            }
          }
        }
      };
      searchForUrls(json);
    } catch (err) {
      console.warn("Instagram JSON parser failed to extract cleanly.", err);
    }
    return [...new Set(urls)];
  };

  const parseFile = async (selectedFile: File) => {
    setFile(selectedFile);
    setIsParsing(true);
    setError(null);
    setExtractedUrls([]);

    try {
      const text = await selectedFile.text();
      let urls: string[] = [];

      if (selectedFile.name.endsWith('.json')) {
        const json = JSON.parse(text);
        if (selectedFile.name.includes('instagram') || selectedFile.name.includes('saved')) {
          urls = parseInstagramJson(json);
        } else {
          const urlRegex = /(https?:\/\/[^\s"']+)/g;
          const matches = text.match(urlRegex);
          if (matches) urls = [...new Set(matches)];
        }
      } else if (selectedFile.name.endsWith('.csv') || selectedFile.name.endsWith('.txt')) {
        const urlRegex = /(https?:\/\/[^\s"']+)/g;
        const matches = text.match(urlRegex);
        if (matches) urls = [...new Set(matches)];
      } else {
        throw new Error('Unsupported file format. Please upload JSON, CSV, or TXT.');
      }

      if (urls.length === 0) {
        throw new Error('No valid URLs could be extracted from this file.');
      }

      setExtractedUrls(urls);
    } catch (err: any) {
      setError(err.message || 'Failed to parse file.');
      setFile(null);
    } finally {
      setIsParsing(false);
    }
  };

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setIsDragging(false);
    const droppedFile = e.dataTransfer.files[0];
    if (droppedFile) parseFile(droppedFile);
  }, []);

  const handleFileInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0];
    if (selectedFile) parseFile(selectedFile);
  };

  const handleSingleUrlSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!singleUrl.trim()) return;
    
    // Attempt to validate URL roughly
    if (!singleUrl.startsWith('http://') && !singleUrl.startsWith('https://')) {
      setError('Please enter a valid URL starting with http:// or https://');
      return;
    }

    setExtractedUrls([singleUrl]);
    handleUpload([singleUrl]);
  };

  const handleUpload = async (urlsToUpload = extractedUrls) => {
    if (urlsToUpload.length === 0) return;
    setIsUploading(true);
    setError(null);

    try {
      const response = await request('/content/bulk', {
        method: 'POST',
        body: JSON.stringify({ urls: urlsToUpload })
      });

      if (response.success) {
        onSuccess(urlsToUpload.length);
      } else {
        throw new Error(response.message || 'Import failed');
      }
    } catch (err: any) {
      setError(err.message || 'Failed to upload links to server.');
      setIsUploading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-background/80 backdrop-blur-sm">
      <div 
        className="w-full max-w-3xl bg-card border border-border rounded-[2.5rem] shadow-2xl overflow-hidden relative"
        onClick={(e) => e.stopPropagation()}
      >
        <button 
          onClick={onClose} 
          className="absolute top-6 right-6 p-2 rounded-full text-muted-foreground hover:bg-secondary hover:text-foreground transition-colors z-20"
        >
          <X size={24} />
        </button>

        <div className="absolute top-0 left-0 w-full h-full pointer-events-none overflow-hidden rounded-[2.5rem]">
           <div className="absolute top-[-10%] left-[-10%] w-[50%] h-[50%] bg-blue-500/10 blur-[100px] rounded-full"></div>
           <div className="absolute top-[-10%] right-[-10%] w-[30%] h-[50%] bg-emerald-500/10 blur-[100px] rounded-full"></div>
        </div>
        
        <div className="relative z-10 p-10 md:p-14">
          <h2 className="text-3xl md:text-4xl font-medium text-center text-foreground mb-8">
            Import your knowledge from <br/>
            <span className="text-primary font-normal transition-all duration-500">{dynamicText}</span>
          </h2>
          
          <form onSubmit={handleSingleUrlSubmit} className="relative mb-6">
            <div className="absolute inset-y-0 left-4 flex items-center gap-2 pointer-events-none hidden sm:flex">
              <div className="flex items-center gap-1.5 bg-secondary text-foreground px-3 py-1.5 rounded-full text-sm font-medium border border-border">
                <Globe size={14}/> Web
              </div>
              <div className="flex items-center gap-1.5 bg-secondary text-foreground px-3 py-1.5 rounded-full text-sm font-medium border border-border">
                <Sparkles size={14} className="text-primary"/> Auto-Extract
              </div>
            </div>
            <input 
              type="text" 
              value={singleUrl}
              onChange={(e) => setSingleUrl(e.target.value)}
              placeholder="Paste a URL or search..."
              className="w-full bg-card/50 border-2 border-primary/20 focus:border-primary/50 focus:ring-4 ring-primary/10 rounded-2xl py-4 sm:pl-[230px] pl-6 pr-14 text-foreground placeholder:text-muted-foreground outline-none transition-all shadow-sm"
              disabled={isUploading || isParsing}
            />
            <button 
              type="submit"
              disabled={isUploading || isParsing || !singleUrl}
              className="absolute inset-y-0 right-4 flex items-center text-muted-foreground hover:text-primary disabled:opacity-50"
            >
              {isUploading && extractedUrls.length === 1 ? <Loader2 size={24} className="animate-spin text-primary" /> : <Search size={24} />}
            </button>
          </form>

          {isUploading && extractedUrls.length > 1 ? (
            <div className="border-2 border-dashed border-border/60 rounded-3xl p-10 flex flex-col items-center justify-center text-center min-h-[280px]">
               <Loader2 size={48} className="text-primary animate-spin mb-4" />
               <p className="text-2xl font-medium text-foreground mb-2">Importing {extractedUrls.length} items</p>
               <p className="text-muted-foreground">The AI is gracefully processing your bulk upload...</p>
            </div>
          ) : file && extractedUrls.length > 0 && !error ? (
            <div className="border-2 border-solid border-emerald-500/30 bg-emerald-500/5 rounded-3xl p-10 flex flex-col items-center justify-center text-center min-h-[280px] relative overflow-hidden">
               <div className="absolute top-0 left-0 w-full h-1 bg-emerald-500/20"></div>
               <CheckCircle2 size={56} className="text-emerald-500 mb-4" />
               <p className="text-2xl font-medium text-foreground mb-2">Found {extractedUrls.length} valid links</p>
               <p className="text-muted-foreground mb-8">from {file.name}</p>
               
               <div className="flex gap-4">
                 <button 
                   onClick={() => setFile(null)}
                   className="px-6 py-2.5 rounded-full font-semibold text-muted-foreground bg-secondary hover:bg-secondary/80 transition-colors"
                 >
                   Cancel
                 </button>
                 <button 
                   onClick={() => handleUpload()}
                   className="px-6 py-2.5 rounded-full font-semibold bg-primary text-primary-foreground hover:bg-primary/90 transition-colors shadow-lg shadow-primary/20"
                 >
                   Start Bulk Import
                 </button>
               </div>
            </div>
          ) : (
            <div 
              onDragOver={handleDragOver}
              onDragLeave={handleDragLeave}
              onDrop={handleDrop}
              className={`border-2 border-dashed rounded-3xl p-8 md:p-10 flex flex-col items-center justify-center text-center transition-all min-h-[280px] ${
                isDragging ? 'border-primary bg-primary/5 scale-[1.01]' : 'border-border/60 hover:border-border'
              }`}
            >
              {isParsing ? (
                <>
                  <Loader2 size={40} className="text-primary animate-spin mb-4" />
                  <p className="text-muted-foreground font-medium">Parsing file...</p>
                </>
              ) : error ? (
                <>
                  <AlertCircle size={40} className="text-red-500 mb-4" />
                  <p className="text-lg font-bold text-red-500 mb-2">Import Error</p>
                  <p className="text-muted-foreground text-sm max-w-sm mb-6">{error}</p>
                  <button 
                    onClick={() => { setError(null); setFile(null); }}
                    className="px-6 py-2 rounded-full bg-secondary text-foreground font-medium hover:bg-secondary/80"
                  >
                    Try again
                  </button>
                </>
              ) : (
                <>
                  <p className="text-2xl font-medium text-foreground mb-2">or drop your files</p>
                  <p className="text-muted-foreground text-sm mb-8">
                    json, csv, txt, <span className="underline decoration-muted-foreground/30 underline-offset-4">and more</span>
                  </p>
                  
                  <div className="flex flex-wrap items-center justify-center gap-3">
                    <label className="flex items-center gap-2 px-5 py-2.5 rounded-full border border-border bg-card hover:bg-secondary transition-colors cursor-pointer text-sm font-medium text-foreground shadow-sm group">
                      <Upload size={16} className="text-muted-foreground group-hover:text-foreground" />
                      Upload files
                      <input type="file" className="hidden" accept=".json,.csv,.txt" onChange={handleFileInput}/>
                    </label>
                    
                    <button className="flex items-center gap-2 px-5 py-2.5 rounded-full border border-border bg-card hover:bg-secondary transition-colors cursor-pointer text-sm font-medium text-foreground shadow-sm">
                      <LinkIcon size={16} className="text-blue-500" />
                      Websites
                    </button>

                    <button className="flex items-center gap-2 px-5 py-2.5 rounded-full border border-border bg-card hover:bg-secondary transition-colors cursor-pointer text-sm font-medium text-foreground shadow-sm">
                      <Camera size={16} className="text-pink-500" />
                      Instagram
                    </button>
                    
                    <button className="flex items-center gap-2 px-5 py-2.5 rounded-full border border-border bg-card hover:bg-secondary transition-colors cursor-pointer text-sm font-medium text-foreground shadow-sm">
                      <PlayCircle size={16} className="text-red-500" />
                      YouTube
                    </button>
                  </div>
                </>
              )}
            </div>
          )}

          <div className="mt-8 flex items-center justify-center gap-4">
            <div className="h-1.5 w-full max-w-[200px] bg-secondary rounded-full overflow-hidden">
              <div className="h-full bg-primary/40 w-[15%] rounded-full"></div>
            </div>
            <span className="text-xs font-medium text-muted-foreground whitespace-nowrap">Daily Limits</span>
          </div>
        </div>
      </div>
    </div>
  );
}
