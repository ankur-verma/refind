import { useState, useRef } from 'react';
import { Upload, Plus, Link as LinkIcon, Play as Youtube, Camera as Instagram, Briefcase as Linkedin, Loader2, CheckCircle2, XCircle } from 'lucide-react';
import { request } from '../api';

export default function IngestV2() {
  const [inputText, setInputText] = useState('');
  const [ingestionStatus, setIngestionStatus] = useState(null); // 'processing', 'completed'
  const [results, setResults] = useState([]);
  const fileInputRef = useRef(null);

  const handlePaste = async () => {
    if (!inputText.trim()) return;
    
    // Extract URLs
    const urlRegex = /https?:\/\/[^\s$.?#].[^\s]*/gi;
    const matches = inputText.match(urlRegex) || [];
    const validUrls = [...new Set(matches.map(url => url.replace(/[.,;:})\]]+$/, '')))];
    
    if (validUrls.length === 0) return;
    
    setIngestionStatus('processing');
    const newResults = [];
    
    for (let url of validUrls) {
      try {
        const res = await request('/content', { method: 'POST', body: JSON.stringify({ url }) });
        newResults.push({ url, success: res.success, isDuplicate: res.isDuplicate });
      } catch (err) {
        newResults.push({ url, success: false, error: 'Connection failed' });
      }
    }
    
    setResults(newResults);
    setIngestionStatus('completed');
    setInputText('');
  };

  return (
    <div className="v2-ingest">
      <header className="v2-header">
        <h1 className="v2-huge-title">Ingestion Hub</h1>
        <p className="v2-subtitle">Feed your externalized brain.</p>
      </header>

      <div className="v2-ingest-grid">
        {/* URL Paste Area */}
        <section className="v2-ingest-section">
          <h2 className="v2-section-title">Manual Ingestion</h2>
          <div className="v2-card v2-ingest-card">
            <div className="v2-input-wrapper">
              <LinkIcon className="v2-input-icon" size={20} />
              <textarea 
                className="v2-textarea"
                placeholder="Paste links, notes, or raw text here..."
                value={inputText}
                onChange={(e) => setInputText(e.target.value)}
                disabled={ingestionStatus === 'processing'}
              />
            </div>
            <div className="v2-ingest-actions">
              <button 
                className="v2-btn-primary" 
                onClick={handlePaste}
                disabled={!inputText.trim() || ingestionStatus === 'processing'}
              >
                {ingestionStatus === 'processing' ? <Loader2 className="v2-spin" size={18} /> : <Plus size={18} />}
                Ingest Content
              </button>
            </div>
          </div>

          {/* Results Area */}
          {results.length > 0 && (
            <div className="v2-results-list">
              {results.map((r, i) => (
                <div key={i} className="v2-result-item">
                  {r.success ? <CheckCircle2 className="v2-text-green" size={18} /> : <XCircle className="v2-text-red" size={18} />}
                  <span className="v2-result-url">{r.url}</span>
                  <span className="v2-result-status">{r.isDuplicate ? 'Duplicate' : r.success ? 'Ingested' : 'Failed'}</span>
                </div>
              ))}
            </div>
          )}
        </section>

        {/* Platform Integration Area */}
        <section className="v2-ingest-section">
          <h2 className="v2-section-title">Data Sources</h2>
          <div className="v2-sources-grid">
            
            <div className="v2-source-card">
              <div className="v2-source-icon v2-bg-red"><Youtube size={24} /></div>
              <h3>YouTube</h3>
              <p>Sync liked videos automatically.</p>
              <button className="v2-btn-secondary">Connect</button>
            </div>

            <div className="v2-source-card">
              <div className="v2-source-icon v2-bg-pink"><Instagram size={24} /></div>
              <h3>Instagram</h3>
              <p>Upload JSON export data.</p>
              <button className="v2-btn-secondary">Upload</button>
            </div>

            <div className="v2-source-card">
              <div className="v2-source-icon v2-bg-blue"><Linkedin size={24} /></div>
              <h3>LinkedIn</h3>
              <p>Upload CSV/JSON export.</p>
              <button className="v2-btn-secondary">Upload</button>
            </div>

          </div>
        </section>
      </div>
    </div>
  );
}
