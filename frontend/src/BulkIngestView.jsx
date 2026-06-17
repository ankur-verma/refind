import { useState, useEffect, useRef } from 'react';
import { Link2, Trash2, ArrowRight, CheckCircle, XCircle, Loader2, Sparkles, Upload, ExternalLink } from 'lucide-react';
import { request } from './api';

// Platform configs
const PLATFORMS = {
  youtube: {
    name: 'YouTube',
    color: '#FF0000',
    bgColor: 'rgba(255, 0, 0, 0.06)',
    borderColor: 'rgba(255, 0, 0, 0.15)',
    icon: '▶',
    type: 'oauth',
    description: 'Import your liked videos automatically via Google sign-in.',
  },
  instagram: {
    name: 'Instagram',
    color: '#E1306C',
    bgColor: 'rgba(225, 48, 108, 0.06)',
    borderColor: 'rgba(225, 48, 108, 0.15)',
    icon: '📷',
    type: 'file',
    accept: '.json',
    description: 'Upload your Instagram data export (JSON) to import saved posts.',
    guide: [
      'Open Instagram → Settings → Your Activity → Download Your Information',
      'Select "Saved" data and request JSON format',
      'Download and upload the file here'
    ]
  },
  linkedin: {
    name: 'LinkedIn',
    color: '#0A66C2',
    bgColor: 'rgba(10, 102, 194, 0.06)',
    borderColor: 'rgba(10, 102, 194, 0.15)',
    icon: '💼',
    type: 'file',
    accept: '.json,.csv',
    description: 'Upload your LinkedIn data export (JSON/CSV) to import saved content.',
    guide: [
      'Open LinkedIn → Settings → Data Privacy → Get a copy of your data',
      'Select data categories and request your archive',
      'Download and upload the file here'
    ]
  }
};

export default function BulkIngestView() {
  const [inputText, setInputText] = useState('');
  const [extractedUrls, setExtractedUrls] = useState([]);
  const [ingestionStatus, setIngestionStatus] = useState(null);
  const [processingIndex, setProcessingIndex] = useState(-1);
  const [results, setResults] = useState({});
  const [activePlatform, setActivePlatform] = useState(null);
  const [importStatus, setImportStatus] = useState(null); // { platform, status, message, data }
  const fileInputRef = useRef(null);

  const urlRegex = /https?:\/\/[^\s$.?#].[^\s]*/gi;

  // Check for YouTube import callback in URL params
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    if (params.get('ytImport') === 'true') {
      setImportStatus({
        platform: 'youtube',
        status: 'success',
        message: `YouTube import complete!`,
        data: {
          imported: parseInt(params.get('imported') || '0'),
          duplicates: parseInt(params.get('duplicates') || '0'),
          total: parseInt(params.get('total') || '0'),
        }
      });
      // Clean URL
      window.history.replaceState({}, '', window.location.pathname);
    }
  }, []);

  useEffect(() => {
    if (!inputText.trim()) {
      setExtractedUrls([]);
      return;
    }
    const matches = inputText.match(urlRegex) || [];
    const uniqueUrls = [...new Set(matches.map(url => url.replace(/[.,;:})\]]+$/, '')))];
    setExtractedUrls(uniqueUrls.map(url => ({ url, isValid: validateUrl(url) })));
  }, [inputText]);

  const validateUrl = (string) => {
    try {
      const parsed = new URL(string);
      return parsed.protocol === 'http:' || parsed.protocol === 'https:';
    } catch (_) { return false; }
  };

  const handleClear = () => {
    setInputText('');
    setExtractedUrls([]);
    setIngestionStatus(null);
    setProcessingIndex(-1);
    setResults({});
  };

  const startIngestion = async () => {
    const validUrls = extractedUrls.filter(item => item.isValid).map(item => item.url);
    if (validUrls.length === 0) return;
    setIngestionStatus('processing');
    setProcessingIndex(0);
    const newResults = {};
    validUrls.forEach(url => { newResults[url] = { status: 'pending', message: 'Queued' }; });
    setResults(newResults);

    for (let i = 0; i < validUrls.length; i++) {
      const url = validUrls[i];
      setProcessingIndex(i);
      setResults(prev => ({ ...prev, [url]: { status: 'processing', message: 'Saving...' } }));
      try {
        const res = await request('/content', { method: 'POST', body: JSON.stringify({ url }) });
        if (res.success) {
          if (res.isDuplicate) {
            setResults(prev => ({ ...prev, [url]: { status: 'duplicate', message: res.message || 'Already saved.' } }));
          } else {
            setResults(prev => ({ ...prev, [url]: { status: 'success', message: 'Saved.' } }));
          }
        } else {
          setResults(prev => ({ ...prev, [url]: { status: 'error', message: res.errors?.[0] || 'Failed' } }));
        }
      } catch (err) {
        setResults(prev => ({ ...prev, [url]: { status: 'error', message: 'Connection failed.' } }));
      }
    }
    setIngestionStatus('completed');
    setProcessingIndex(-1);
  };

  // ── YouTube OAuth ──
  const handleYouTubeConnect = async () => {
    setImportStatus({ platform: 'youtube', status: 'loading', message: 'Connecting to YouTube...' });
    try {
      const res = await request('/content/social/youtube/auth-url');
      if (res.success && res.data?.authUrl) {
        window.location.href = res.data.authUrl;
      } else {
        setImportStatus({ platform: 'youtube', status: 'error', message: 'Could not get YouTube auth URL. Check your Google OAuth config.' });
      }
    } catch (err) {
      setImportStatus({ platform: 'youtube', status: 'error', message: 'Failed to connect.' });
    }
  };

  // ── File Upload (Instagram / LinkedIn) ──
  const handleFileUpload = async (platform, file) => {
    if (!file) return;
    setImportStatus({ platform, status: 'loading', message: `Parsing ${PLATFORMS[platform].name} export...` });
    
    const formData = new FormData();
    formData.append('file', file);

    try {
      const token = localStorage.getItem('token');
      const res = await fetch(`http://localhost:5183/api/v1/content/social/${platform}/import`, {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${token}` },
        body: formData,
      });
      const data = await res.json();
      if (data.success && data.data) {
        setImportStatus({
          platform,
          status: 'success',
          message: `${PLATFORMS[platform].name} import complete!`,
          data: data.data,
        });
      } else {
        setImportStatus({ platform, status: 'error', message: data.errors?.[0] || 'Import failed.' });
      }
    } catch (err) {
      setImportStatus({ platform, status: 'error', message: 'Connection to server failed.' });
    }
  };

  const totalUrls = extractedUrls.length;
  const validUrlsCount = extractedUrls.filter(u => u.isValid).length;
  const processedCount = Object.values(results).filter(r => r.status === 'success' || r.status === 'error').length;
  const progressPercent = totalUrls > 0 ? Math.round((processedCount / validUrlsCount) * 100) : 0;

  return (
    <div className="ingest-container">
      <div className="view-header">
        <h1>Memory Ingestion Hub</h1>
        <p>Import content from social platforms or paste links directly. AI will extract, parse, and structure them into your cognitive memory.</p>
      </div>

      {/* ── Platform Import Cards ── */}
      <div className="social-import-section">
        <h3 style={{ marginBottom: '16px', fontSize: '16px', fontWeight: 600 }}>Import from Platforms</h3>
        <div className="platform-cards-grid">
          {Object.entries(PLATFORMS).map(([key, platform]) => (
            <div 
              key={key}
              className={`platform-card ${activePlatform === key ? 'expanded' : ''}`}
              style={{ 
                '--platform-color': platform.color,
                '--platform-bg': platform.bgColor,
                '--platform-border': platform.borderColor,
              }}
            >
              <div className="platform-card-header" onClick={() => setActivePlatform(activePlatform === key ? null : key)}>
                <span className="platform-icon">{platform.icon}</span>
                <div className="platform-info">
                  <strong>{platform.name}</strong>
                  <span className="platform-desc">{platform.description}</span>
                </div>
              </div>

              {activePlatform === key && (
                <div className="platform-card-body">
                  {platform.type === 'oauth' ? (
                    <button 
                      className="btn btn-primary platform-action-btn"
                      style={{ background: platform.color }}
                      onClick={handleYouTubeConnect}
                      disabled={importStatus?.platform === key && importStatus?.status === 'loading'}
                    >
                      {importStatus?.platform === key && importStatus?.status === 'loading' ? (
                        <><Loader2 size={16} className="spin" /> Connecting...</>
                      ) : (
                        <><ExternalLink size={16} /> Sign in with Google</>
                      )}
                    </button>
                  ) : (
                    <>
                      <div className="import-guide">
                        <p style={{ fontWeight: 500, marginBottom: '8px', fontSize: '13px', color: 'var(--text-primary)' }}>How to export:</p>
                        <ol>
                          {platform.guide.map((step, i) => (
                            <li key={i}>{step}</li>
                          ))}
                        </ol>
                      </div>
                      <div 
                        className="file-dropzone"
                        onClick={() => {
                          fileInputRef.current.accept = platform.accept;
                          fileInputRef.current.dataset.platform = key;
                          fileInputRef.current.click();
                        }}
                      >
                        <Upload size={24} style={{ color: platform.color }} />
                        <span>Click to upload {platform.accept} file</span>
                      </div>
                    </>
                  )}

                  {/* Import result feedback */}
                  {importStatus?.platform === key && importStatus?.status === 'success' && (
                    <div className="import-result success">
                      <CheckCircle size={16} />
                      <span>{importStatus.message} — {importStatus.data?.imported || 0} imported, {importStatus.data?.duplicates || 0} already saved.</span>
                    </div>
                  )}
                  {importStatus?.platform === key && importStatus?.status === 'error' && (
                    <div className="import-result error">
                      <XCircle size={16} />
                      <span>{importStatus.message}</span>
                    </div>
                  )}
                </div>
              )}
            </div>
          ))}
        </div>
      </div>

      {/* Hidden file input */}
      <input 
        type="file" 
        ref={fileInputRef}
        style={{ display: 'none' }}
        onChange={(e) => {
          const file = e.target.files[0];
          const platform = e.target.dataset.platform;
          if (file && platform) handleFileUpload(platform, file);
          e.target.value = '';
        }}
      />

      {/* ── Manual URL Paste (existing feature) ── */}
      <div className="grid-2col" style={{ marginTop: '32px' }}>
        <div className="card glass">
          <div className="card-header">
            <h3><Sparkles size={18} className="icon-purple" /> Paste Raw Content & Links</h3>
            <button className="btn btn-secondary-sm" onClick={handleClear} disabled={ingestionStatus === 'processing'}>
              <Trash2 size={16} /> Clear
            </button>
          </div>
          <textarea
            className="ingest-textarea"
            placeholder={"Paste your links or general notes here. E.g.,\n- https://youtube.com/watch?v=xyz for machine learning tutorial.\n- Also check out https://example.com/net10-guide.pdf for modular monoliths!"}
            value={inputText}
            onChange={(e) => setInputText(e.target.value)}
            disabled={ingestionStatus === 'processing'}
          />
          <div className="ingest-actions">
            <div className="stats">
              <span>Found: <strong>{totalUrls}</strong> URLs</span>
              {totalUrls > 0 && (
                <span className="valid-count text-green">({validUrlsCount} valid)</span>
              )}
            </div>
            <button
              className="btn btn-primary"
              disabled={validUrlsCount === 0 || ingestionStatus === 'processing'}
              onClick={startIngestion}
            >
              {ingestionStatus === 'processing' ? (
                <><Loader2 size={18} className="spin" /> Ingesting...</>
              ) : (
                <>Ingest Memories <ArrowRight size={18} /></>
              )}
            </button>
          </div>
        </div>

        <div className="card glass">
          <div className="card-header">
            <h3>Ingestion Status Queue</h3>
            {ingestionStatus === 'processing' && (
              <span className="badge badge-purple animate-pulse">Processing ({progressPercent}%)</span>
            )}
            {ingestionStatus === 'completed' && (
              <span className="badge badge-green">Completed</span>
            )}
          </div>
          <div className="url-queue-list">
            {extractedUrls.length === 0 ? (
              <div className="empty-state">
                <Link2 size={48} className="empty-icon" />
                <p>Waiting for text paste...</p>
                <span>URLs will be automatically extracted from your input notes.</span>
              </div>
            ) : (
              extractedUrls.map((item, idx) => {
                const urlResult = results[item.url];
                return (
                  <div key={idx} className={`url-queue-item ${item.isValid ? 'valid' : 'invalid'}`}>
                    <div className="url-info">
                      <span className="url-protocol">{item.url.split('://')[0]}://</span>
                      <span className="url-host" title={item.url}>
                        {item.url.replace(/^https?:\/\//i, '').substring(0, 45)}
                        {item.url.replace(/^https?:\/\//i, '').length > 45 ? '...' : ''}
                      </span>
                    </div>
                    <div className="url-status-action">
                      {!item.isValid ? (
                        <span className="status-badge error"><XCircle size={16} /> Invalid URL</span>
                      ) : !urlResult ? (
                        <span className="status-badge pending">Ready</span>
                      ) : urlResult.status === 'processing' ? (
                        <span className="status-badge processing"><Loader2 size={14} className="spin" /> Ingesting</span>
                      ) : urlResult.status === 'success' ? (
                        <span className="status-badge success" title={urlResult.message}><CheckCircle size={16} /> Ingested</span>
                      ) : urlResult.status === 'duplicate' ? (
                        <span className="status-badge warning" title={urlResult.message} style={{color: '#d97706', borderColor: '#fcd34d', backgroundColor: '#fef3c7'}}><CheckCircle size={16} /> Already Saved</span>
                      ) : (
                        <span className="status-badge error" title={urlResult.message}><XCircle size={16} /> Failed</span>
                      )}
                    </div>
                  </div>
                );
              })
            )}
          </div>
          {ingestionStatus === 'processing' && (
            <div className="progress-bar-container">
              <div className="progress-bar-fill" style={{ width: `${progressPercent}%` }}></div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
