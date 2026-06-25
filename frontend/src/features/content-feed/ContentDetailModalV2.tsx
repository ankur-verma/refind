import { useState, useEffect, useRef } from 'react';
import { 
  X, Calendar, ArrowUpRight, 
  BookOpen, Sparkles, Loader2, PlayCircle, HelpCircle,
  MessageSquare, Send, Brain, ChevronDown, ChevronUp, RefreshCw, CheckCircle2,
  Activity
} from 'lucide-react';
import { request } from '../../api';
import { marked } from 'marked';
import createDOMPurify from 'dompurify';

const DOMPurify = createDOMPurify(window);

function getYouTubeId(url) {
  if (!url) return null;
  const regExp = /^.*(youtu.be\/|v\/|u\/\w\/|embed\/|watch\?v=|\&v=)([^#\&\?]*).*/;
  const match = url.match(regExp);
  return (match && match[2].length === 11) ? match[2] : null;
}

export default function ContentDetailModalV2({ contentId, initialAction, onClose }) {
  const [detail, setDetail] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [query, setQuery] = useState('');
  const [chatHistory, setChatHistory] = useState([
    { role: 'assistant', text: 'Hello! I am focused on this memory. What would you like to explore?' }
  ]);
  const [loadingChat, setLoadingChat] = useState(false);
  const [seekTime, setSeekTime] = useState(0);

  const chatEndRef = useRef(null);
  const inputRef = useRef(null);

  useEffect(() => {
    import('../../shared/analytics/AnalyticsService').then(({ analytics }) => {
      analytics.track('Opened', contentId, { action: initialAction });
    });

    if (initialAction === 'askAi' && inputRef.current) {
      setTimeout(() => inputRef.current.focus(), 300);
    } else if (initialAction === 'addNote' && inputRef.current) {
      setQuery('Note: ');
      setTimeout(() => inputRef.current.focus(), 300);
    }
  }, [initialAction, loading, contentId]);

  const createMarkup = (text) => {
    if (!text) return { __html: '' };
    try {
      const html = marked.parse(text, { breaks: true, gfm: true });
      return { __html: DOMPurify.sanitize(html) };
    } catch (e) {
      return { __html: text };
    }
  };

  useEffect(() => {
    const fetchDetail = async () => {
      setLoading(true);
      const res = await request(`/content/${contentId}`);
      if (res.success && res.data) {
        setDetail(res.data);
      }
      
      const savedChat = localStorage.getItem(`chat-${contentId}`);
      if (savedChat) {
        try {
          setChatHistory(JSON.parse(savedChat));
        } catch (_) {}
      } else {
        setChatHistory([{
          role: 'assistant',
          text: 'Hello! I am focused on this memory. What would you like to explore?'
        }]);
      }
      setLoading(false);
    };
    fetchDetail();
    
    // Telemetry: record start time
    const viewStartTime = Date.now();
    return () => {
      const durationSeconds = Math.round((Date.now() - viewStartTime) / 1000);
      import('../../utils/telemetry').then(({ trackInteraction }) => {
        trackInteraction({
          contentItemId: contentId,
          interactionType: 'View',
          durationSeconds: durationSeconds
        });
      });
    };
  }, [contentId]);

  useEffect(() => {
    chatEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [chatHistory]);

  const handleSendChat = async (e) => {
    e.preventDefault();
    if (!query.trim() || loadingChat) return;

    const currentQuery = query;
    setQuery('');
    setLoadingChat(true);

    const updatedHistoryUser = [...chatHistory, { role: 'user', text: currentQuery }];
    setChatHistory(updatedHistoryUser);
    localStorage.setItem(`chat-${contentId}`, JSON.stringify(updatedHistoryUser));

    const res = await request('/search/rag', {
      method: 'POST',
      body: JSON.stringify({
        query: currentQuery,
        mode: 'SelectedLinks',
        selectedContentItemIds: [contentId]
      })
    });

    if (res.success && res.data) {
      const updatedHistoryAssistant = [...updatedHistoryUser, {
        role: 'assistant',
        text: res.data.answer
      }];
      setChatHistory(updatedHistoryAssistant);
      localStorage.setItem(`chat-${contentId}`, JSON.stringify(updatedHistoryAssistant));
    } else {
      const errorHistory = [...updatedHistoryUser, {
        role: 'assistant',
        text: 'Error formulating response: ' + (res.errors?.[0] || 'An error occurred.')
      }];
      setChatHistory(errorHistory);
      localStorage.setItem(`chat-${contentId}`, JSON.stringify(errorHistory));
    }
    setLoadingChat(false);
  };

  if (loading) {
    return (
      <div className="v2-modal-overlay">
        <div className="v2-modal-content" style={{ maxWidth: '400px', padding: '48px', alignItems: 'center' }}>
          <Activity size={40} className="v2-pulse-icon" />
          <h3 style={{ marginTop: '24px' }}>Loading Memory...</h3>
        </div>
      </div>
    );
  }

  if (!detail) {
    return (
      <div className="v2-modal-overlay">
        <div className="v2-modal-content" style={{ maxWidth: '400px', padding: '48px', alignItems: 'center' }}>
          <h3>Memory Not Found</h3>
          <button className="v2-btn-secondary" onClick={onClose} style={{ marginTop: '24px' }}>Close</button>
        </div>
      </div>
    );
  }

  const isProcessing = detail.status === 0;
  const youtubeId = getYouTubeId(detail.originalUrl);

  return (
    <div className="fixed inset-0 z-[100] flex justify-center items-end md:items-center bg-black/60 transition-opacity" onClick={onClose}>
      <div className="w-full md:w-[90vw] md:max-w-6xl h-[90dvh] md:h-[85vh] bg-card md:rounded-3xl rounded-t-3xl overflow-hidden flex flex-col shadow-2xl transition-transform animate-in slide-in-from-bottom-10" onClick={(e) => e.stopPropagation()}>
        
        {/* Mobile Drag Handle Indicator */}
        <div className="w-full flex justify-center pt-3 pb-1 md:hidden">
          <div className="w-12 h-1.5 bg-border/80 rounded-full" />
        </div>

        <header className="v2-modal-header border-b border-border p-4 md:p-6 flex justify-between items-center bg-background/50">
          <div className="v2-modal-header-left">
            <span className="v2-pill active">
              {detail.platformType === 0 ? 'YouTube' :
               detail.platformType === 1 ? 'Web' :
               detail.platformType === 2 ? 'Instagram' :
               detail.platformType === 3 ? 'PDF' : 'TikTok'}
            </span>
            <h2 className="v2-modal-title">{detail.title || 'Untitled Memory'}</h2>
          </div>
          <button className="v2-modal-close" onClick={onClose}><X size={20} /></button>
        </header>

        <style>{`
          .v2-modal-chat-viewport { flex: 1 1 0 !important; min-height: 0 !important; overflow-y: auto !important; }
          .v2-modern-msg { display: flex; gap: 16px; width: 100%; margin-bottom: 24px; }
          .v2-modern-msg.user { justify-content: flex-end; }
          .v2-modern-msg.assistant { justify-content: flex-start; align-items: flex-start; }
          .v2-modern-avatar { width: 32px; height: 32px; border-radius: 8px; display: flex; align-items: center; justify-content: center; flex-shrink: 0; margin-top: 2px; background: linear-gradient(135deg, var(--v2-accent) 0%, #6366f1 100%); color: white; }
          .v2-modern-bubble { max-width: 85%; font-size: 15px; line-height: 1.6; }
          .v2-modern-bubble.user { background: rgba(0,0,0,0.05); padding: 12px 18px; border-radius: 20px; border-bottom-right-radius: 4px; color: var(--v2-text); }
          .dark .v2-modern-bubble.user { background: rgba(255,255,255,0.08); }
          .v2-modern-bubble.assistant { color: var(--v2-text); padding-top: 4px; }
          .v2-modern-text { white-space: pre-wrap; word-break: break-word; }
          .v2-modern-typing { display: flex; gap: 4px; padding: 8px 0; }
          .v2-modern-typing .dot { width: 6px; height: 6px; background: var(--v2-text-muted); border-radius: 50%; animation: v2-modern-typing-bounce 1.4s infinite ease-in-out both; }
          .v2-modern-typing .dot:nth-child(1) { animation-delay: -0.32s; }
          .v2-modern-typing .dot:nth-child(2) { animation-delay: -0.16s; }
          @keyframes v2-modern-typing-bounce { 0%, 80%, 100% { transform: scale(0); } 40% { transform: scale(1); } }
          .v2-modern-input-wrapper { padding: 0 20px 24px 20px; display: flex; flex-direction: column; gap: 12px; background: var(--v2-bg-pane); position: relative; z-index: 10; margin-top: auto; border-top: 1px solid rgba(0,0,0,0.02); }
          .v2-modern-suggestions { display: flex; gap: 8px; flex-wrap: nowrap; overflow-x: auto; padding-bottom: 4px; -ms-overflow-style: none; scrollbar-width: none; }
          .v2-modern-suggestions::-webkit-scrollbar { display: none; }
          .v2-modern-chip { padding: 8px 16px; border-radius: 20px; background: var(--v2-bg-body); border: 1px solid rgba(0,0,0,0.1); font-size: 13px; font-weight: 500; color: var(--v2-text); cursor: pointer; transition: all 0.2s; white-space: nowrap; flex-shrink: 0; }
          .dark .v2-modern-chip { border-color: rgba(255,255,255,0.1); }
          .v2-modern-chip:hover { background: rgba(0,0,0,0.03); transform: translateY(-1px); }
          .dark .v2-modern-chip:hover { background: rgba(255,255,255,0.05); }
          .v2-modern-form { position: relative; background: var(--v2-bg-body); border: 1px solid rgba(0,0,0,0.1); border-radius: 24px; padding: 6px 6px 6px 20px; display: flex; alignItems: center; box-shadow: 0 4px 15px rgba(0,0,0,0.03); transition: border-color 0.2s, box-shadow 0.2s; }
          .dark .v2-modern-form { border-color: rgba(255,255,255,0.1); box-shadow: 0 4px 15px rgba(0,0,0,0.2); }
          .v2-modern-form:focus-within { border-color: var(--v2-accent); box-shadow: 0 4px 20px rgba(0,0,0,0.08); }
          .v2-modern-input { flex-grow: 1; border: none; background: transparent; outline: none; font-size: 15px; color: var(--v2-text); padding: 8px 0; }
          .v2-modern-send { background: rgba(0,0,0,0.05); color: var(--v2-text-muted); border: none; border-radius: 50%; width: 36px; height: 36px; display: flex; align-items: center; justify-content: center; cursor: default; transition: all 0.2s; flex-shrink: 0; margin-left: 12px; }
          .dark .v2-modern-send { background: rgba(255,255,255,0.05); }
          .v2-modern-send.active { background: var(--v2-accent); color: #fff; cursor: pointer; }
          .v2-modern-send.active:hover { transform: scale(1.05); }
        `}</style>

        <div className="flex flex-col md:flex-row flex-1 overflow-hidden">
          {/* Left Panel: Content & Video */}
          <div className="flex-1 overflow-y-auto p-4 md:p-6 custom-scrollbar">
            {youtubeId ? (
              <div className="v2-modal-video">
                <iframe
                  width="100%"
                  height="380"
                  src={`https://www.youtube.com/embed/${youtubeId}?start=${seekTime}&autoplay=${seekTime > 0 ? 1 : 0}`}
                  title="YouTube video"
                  frameBorder="0"
                  allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                  allowFullScreen
                ></iframe>
              </div>
            ) : (
              detail.heroImageUrl && (
                <img src={detail.heroImageUrl} alt={detail.title} className="v2-modal-hero-img" />
              )
            )}

            {isProcessing ? (
              <div className="v2-empty" style={{ minHeight: '200px' }}>
                <Activity size={32} className="v2-spin" />
                <p>Synthesizing insights... please wait.</p>
              </div>
            ) : (
              <>
                <div className="v2-modal-section">
                  <div className="v2-modal-section-title">
                    <Sparkles size={16} /> QuickSpark Summary
                  </div>
                  {detail.quickSparkSummary?.toLowerCase().includes("failed to process") || detail.quickSparkSummary?.toLowerCase().includes("unable to generate") ? (
                    <div style={{ padding: '16px', background: 'rgba(239, 68, 68, 0.1)', border: '1px solid rgba(239, 68, 68, 0.2)', color: 'rgb(239, 68, 68)', borderRadius: '12px', display: 'flex', gap: '12px', alignItems: 'flex-start' }}>
                      <HelpCircle size={20} style={{ flexShrink: 0, marginTop: '2px' }} />
                      <div style={{ fontSize: '14px', lineHeight: '1.5' }}>
                        <strong style={{ display: 'block', marginBottom: '4px' }}>Processing Failed</strong>
                        The AI was unable to generate a summary for this content. It might be too long, gated behind a login, or in an unsupported format.
                      </div>
                    </div>
                  ) : (
                    <div 
                      className="v2-markdown-body" 
                      style={{ fontSize: '15px', lineHeight: '1.6', color: 'var(--v2-text)' }}
                      dangerouslySetInnerHTML={createMarkup(detail.quickSparkSummary)} 
                    />
                  )}
                </div>

                {detail.actionItems?.filter(a => !a.description.toLowerCase().includes('failed')).length > 0 && (
                  <div className="v2-modal-section">
                    <div className="v2-modal-section-title">
                      <CheckCircle2 size={16} /> Action Items
                    </div>
                    <div className="v2-action-list">
                      {detail.actionItems.filter(a => !a.description.toLowerCase().includes('failed')).map(action => (
                        <div key={action.id} className="v2-action-item">
                          <CheckCircle2 size={18} className="v2-action-item-icon" />
                          <div style={{ fontSize: '14px', lineHeight: '1.5' }}>{action.description}</div>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {detail.videoSegments?.length > 0 && (
                  <div className="v2-modal-section">
                    <div className="v2-modal-section-title">
                      <PlayCircle size={16} /> Timeline
                    </div>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                      {detail.videoSegments.map((seg, idx) => {
                        const m = Math.floor(seg.startSeconds / 60);
                        const s = seg.startSeconds % 60;
                        const timeStr = `${m}:${s.toString().padStart(2, '0')}`;
                        return (
                          <button
                            key={seg.id || idx}
                            onClick={() => setSeekTime(seg.startSeconds)}
                            style={{ 
                              display: 'flex', gap: '16px', padding: '12px', background: 'var(--v2-bg-pane)',
                              borderRadius: '12px', border: '1px solid var(--v2-border)', textAlign: 'left', cursor: 'pointer'
                            }}
                          >
                            <span style={{ color: 'var(--v2-accent)', fontWeight: '600', fontSize: '13px', width: '40px' }}>{timeStr}</span>
                            <div>
                              <div style={{ fontWeight: '500', fontSize: '14px', color: 'var(--v2-text)' }}>{seg.title}</div>
                              {seg.summary && <div style={{ fontSize: '13px', color: 'var(--v2-text-secondary)', marginTop: '4px' }}>{seg.summary}</div>}
                            </div>
                          </button>
                        );
                      })}
                    </div>
                  </div>
                )}
              </>
            )}
          </div>

          {/* Right Panel: Chat */}
          <div className="w-full md:w-[400px] bg-card flex flex-col border-t md:border-t-0 md:border-l border-border shrink-0" style={{ minHeight: 0 }}>
            <div style={{ padding: '20px 24px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', zIndex: 10, background: 'var(--v2-bg-pane)', borderBottom: '1px solid rgba(0,0,0,0.04)', flexShrink: 0 }}>
              <div className="v2-modal-section-title" style={{ margin: 0, fontSize: '16px', fontWeight: '600' }}>
                <Sparkles size={18} style={{ color: 'var(--v2-accent)' }} /> Memory Chat
              </div>
              <a href={detail.originalUrl} target="_blank" rel="noreferrer" className="v2-btn-secondary" style={{ padding: '6px 14px', fontSize: '13px', fontWeight: 500, background: 'var(--v2-bg-body)', border: '1px solid var(--v2-border)', color: 'var(--v2-text-muted)', textDecoration: 'none', display: 'flex', alignItems: 'center', gap: '6px', borderRadius: '20px', transition: 'all 0.2s' }}>
                Original <ArrowUpRight size={14} />
              </a>
            </div>

            <div className="v2-modal-chat-viewport">
              {chatHistory.map((msg, idx) => (
                <div key={idx} className={`v2-modern-msg ${msg.role === 'user' ? 'user' : 'assistant'}`}>
                  {msg.role === 'assistant' && (
                    <div className="v2-modern-avatar">
                      <Sparkles size={16} />
                    </div>
                  )}
                  <div className={`v2-modern-bubble ${msg.role === 'user' ? 'user' : 'assistant'}`}>
                    {msg.role === 'assistant' ? (
                      <div className="v2-markdown-body" dangerouslySetInnerHTML={createMarkup(msg.text)} />
                    ) : (
                      <div className="v2-modern-text">{msg.text}</div>
                    )}
                  </div>
                </div>
              ))}
              {loadingChat && (
                <div className="v2-modern-msg assistant">
                  <div className="v2-modern-avatar">
                    <Sparkles size={16} />
                  </div>
                  <div className="v2-modern-bubble assistant">
                    <div className="v2-modern-typing">
                      <div className="dot"></div>
                      <div className="dot"></div>
                      <div className="dot"></div>
                    </div>
                  </div>
                </div>
              )}
              <div ref={chatEndRef} />
            </div>

            <div className="v2-modern-input-wrapper">
              {chatHistory.length <= 1 && (
                <div className="v2-modern-suggestions">
                  {['Summarize this', 'Key takeaways', 'Action items', detail.platformType === 0 ? 'Video timeline' : 'Main topics'].map(suggestion => (
                    <button
                      key={suggestion}
                      type="button"
                      onClick={() => setQuery(suggestion)}
                      className="v2-modern-chip"
                    >
                      {suggestion}
                    </button>
                  ))}
                </div>
              )}
              <form onSubmit={handleSendChat} className="v2-modern-form">
                <input
                  ref={inputRef}
                  className="v2-modern-input"
                  placeholder={initialAction === 'addNote' ? "Type your note here..." : "Ask about this memory..."}
                  value={query}
                  onChange={e => setQuery(e.target.value)}
                  disabled={loadingChat}
                />
                <button 
                  type="submit" 
                  className={`v2-modern-send ${query.trim() ? 'active' : ''}`}
                  disabled={loadingChat || !query.trim()}
                >
                  {loadingChat ? <Activity size={16} className="v2-spin" /> : <Send size={16} style={{ marginLeft: '-2px' }} />}
                </button>
              </form>
            </div>
          </div>

        </div>
      </div>
    </div>
  );
}
