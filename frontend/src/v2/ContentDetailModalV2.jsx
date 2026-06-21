import { useState, useEffect, useRef } from 'react';
import { 
  X, Calendar, ArrowUpRight, 
  BookOpen, Sparkles, Loader2, PlayCircle, HelpCircle,
  MessageSquare, Send, Brain, ChevronDown, ChevronUp, RefreshCw, CheckCircle2,
  Activity
} from 'lucide-react';
import { request } from '../api';
import { marked } from 'marked';
import createDOMPurify from 'dompurify';

const DOMPurify = createDOMPurify(window);

function getYouTubeId(url) {
  if (!url) return null;
  const regExp = /^.*(youtu.be\/|v\/|u\/\w\/|embed\/|watch\?v=|\&v=)([^#\&\?]*).*/;
  const match = url.match(regExp);
  return (match && match[2].length === 11) ? match[2] : null;
}

export default function ContentDetailModalV2({ contentId, onClose }) {
  const [detail, setDetail] = useState(null);
  const [loading, setLoading] = useState(true);
  const [seekTime, setSeekTime] = useState(0);

  // Chat state
  const [query, setQuery] = useState('');
  const [loadingChat, setLoadingChat] = useState(false);
  const [chatHistory, setChatHistory] = useState([]);
  const chatEndRef = useRef(null);

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
      import('../utils/telemetry').then(({ trackInteraction }) => {
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
    <div className="v2-modal-overlay" onClick={onClose}>
      <div className="v2-modal-content" onClick={(e) => e.stopPropagation()}>
        
        <header className="v2-modal-header">
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

        <div className="v2-modal-body">
          {/* Left Panel: Content & Video */}
          <div className="v2-modal-left">
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
                  {/* Using Markdown for proper rendering */}
                  <div 
                    className="v2-markdown-body" 
                    style={{ fontSize: '15px', lineHeight: '1.6', color: 'var(--v2-text)' }}
                    dangerouslySetInnerHTML={createMarkup(detail.quickSparkSummary)} 
                  />
                </div>

                {detail.actionItems?.length > 0 && (
                  <div className="v2-modal-section">
                    <div className="v2-modal-section-title">
                      <CheckCircle2 size={16} /> Action Items
                    </div>
                    <div className="v2-action-list">
                      {detail.actionItems.map(action => (
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
          <div className="v2-modal-right">
            <div style={{ padding: '24px', borderBottom: '1px solid var(--v2-border)', display: 'flex', justifyContent: 'space-between' }}>
              <div className="v2-modal-section-title" style={{ margin: 0 }}>
                <MessageSquare size={16} /> Memory Chat
              </div>
              <a href={detail.originalUrl} target="_blank" rel="noreferrer" className="v2-btn-secondary" style={{ padding: '6px 12px', fontSize: '12px' }}>
                Open Original <ArrowUpRight size={14} />
              </a>
            </div>

            <div className="v2-modal-chat-viewport">
              {chatHistory.map((msg, idx) => (
                <div key={idx} className={`v2-msg-row ${msg.role === 'user' ? 'user' : 'assistant'}`}>
                  {msg.role === 'assistant' && (
                    <div className="v2-msg-avatar assistant">
                      <Brain size={16} />
                    </div>
                  )}
                  <div className={`v2-msg-bubble ${msg.role === 'user' ? 'user' : 'assistant'}`}>
                    {msg.role === 'assistant' ? (
                      <div className="v2-markdown-body" dangerouslySetInnerHTML={createMarkup(msg.text)} />
                    ) : (
                      msg.text
                    )}
                  </div>
                </div>
              ))}
              {loadingChat && (
                <div className="v2-msg-row assistant">
                  <div className="v2-msg-avatar assistant"><Brain size={16} /></div>
                  <div className="v2-msg-bubble assistant">
                    <Activity size={16} className="v2-spin" />
                  </div>
                </div>
              )}
              <div ref={chatEndRef} />
            </div>

            <div className="v2-chat-input-area" style={{ background: 'transparent' }}>
              <form onSubmit={handleSendChat} className="v2-chat-form">
                <input
                  className="v2-chat-input"
                  placeholder="Ask about this memory..."
                  value={query}
                  onChange={e => setQuery(e.target.value)}
                  disabled={loadingChat}
                />
                <button type="submit" className="v2-chat-send" disabled={loadingChat || !query.trim()}>
                  {loadingChat ? <Activity size={16} className="v2-spin" /> : <Send size={16} />}
                </button>
              </form>
            </div>
          </div>

        </div>
      </div>
    </div>
  );
}
