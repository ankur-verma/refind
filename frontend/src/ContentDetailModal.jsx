import { useState, useEffect, useRef } from 'react';
import { 
  X, Calendar, ArrowUpRight, 
  BookOpen, Sparkles, Loader2, PlayCircle, HelpCircle,
  MessageSquare, Send, Brain, ChevronDown, ChevronUp, RefreshCw
} from 'lucide-react';
import { request } from './api';
import { marked } from 'marked';
import createDOMPurify from 'dompurify';

const DOMPurify = createDOMPurify(window);

export default function ContentDetailModal({ contentId, onClose }) {
  const [detail, setDetail] = useState(null);
  const [loading, setLoading] = useState(true);

  // Chat state
  const [query, setQuery] = useState('');
  const [loadingChat, setLoadingChat] = useState(false);
  const [chatHistory, setChatHistory] = useState([]);
  const chatEndRef = useRef(null);

  // Accordion state for Left Column
  const [actionsExpanded, setActionsExpanded] = useState(false);

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
      
      // Load local chat history for this specific document
      const savedChat = localStorage.getItem(`chat-${contentId}`);
      if (savedChat) {
        try {
          setChatHistory(JSON.parse(savedChat));
        } catch (_) {}
      } else {
        // Default first message
        setChatHistory([{
          role: 'assistant',
          text: 'Hello! I am focused on this specific memory. What would you like to know or explore further about it?'
        }]);
      }
      setLoading(false);
    };
    
    fetchDetail();
  }, [contentId]);

  useEffect(() => {
    // Scroll chat to bottom
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

    // Hit the RAG endpoint but restricted only to THIS content id
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
      <div className="modal-overlay">
        <div className="modal-content glass modal-loading">
          <Loader2 size={40} className="spin loader-big" />
          <p>Analyzing and retrieving content metadata...</p>
        </div>
      </div>
    );
  }

  if (!detail) {
    return (
      <div className="modal-overlay">
        <div className="modal-content glass" role="dialog" aria-modal="true" aria-labelledby="modal-error-title">
          <div className="modal-header">
            <h3 id="modal-error-title">Error</h3>
            <button className="close-btn" onClick={onClose} aria-label="Close modal" data-tooltip="Close modal"><X size={20} /></button>
          </div>
          <p>Failed to load memory details.</p>
        </div>
      </div>
    );
  }

  const isProcessing = detail.status === 0;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content glass animate-scale-up" onClick={(e) => e.stopPropagation()} role="dialog" aria-modal="true" aria-labelledby="modal-title">
        <div className="modal-header">
          <div className="modal-header-title">
            <span className="badge badge-purple">
              {detail.platformType === 0 ? 'YouTube' :
               detail.platformType === 1 ? 'Web' :
               detail.platformType === 2 ? 'Instagram' :
               detail.platformType === 3 ? 'PDF' : 'TikTok'}
            </span>
            <h3 id="modal-title">{detail.title || 'Untitled Memory'}</h3>
          </div>
          <button className="close-btn" onClick={onClose} aria-label="Close modal" data-tooltip="Close modal"><X size={20} /></button>
        </div>

        <div className="modal-body-scroll">
          {/* Hero header block */}
          {detail.heroImageUrl && (
            <div className="modal-hero-img">
              <img src={detail.heroImageUrl} alt={detail.title} referrerPolicy="no-referrer" />
            </div>
          )}

          <div className="modal-grid">
            {/* Left Column: Summary, Raw details, and Action Items */}
            <div className="modal-main-col">
              {isProcessing ? (
                <div className="processing-notice">
                  <Loader2 size={24} className="spin text-purple" />
                  <div>
                    <h4>AI Engine Ingesting...</h4>
                    <p>We are scraping the URL, generating summary points, extracting actionable blueprints, and indexing search vectors. This card will update automatically.</p>
                  </div>
                </div>
              ) : (
                <>
                  <div className="detail-section">
                    <h4 className="section-title"><Sparkles size={16} className="icon-purple" /> QuickSpark Summary</h4>
                    <p className="summary-text">{detail.quickSparkSummary || 'No summary generated yet.'}</p>
                  </div>

                  {detail.actionItems?.length > 0 && (
                    <div className="detail-section">
                      <button 
                        className="section-toggle-btn" 
                        onClick={() => setActionsExpanded(!actionsExpanded)}
                      >
                        <h4 className="section-title" style={{ margin: 0 }}>
                          <PlayCircle size={16} className="icon-purple" /> Action Items Blueprint
                        </h4>
                        {actionsExpanded ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
                      </button>
                      
                      {actionsExpanded && (
                        <div className="action-items-list animate-slide-down" style={{ marginTop: '10px' }}>
                          <ul style={{ paddingLeft: '20px', margin: 0, color: 'var(--text-secondary)' }}>
                            {detail.actionItems.map(action => (
                              <li key={action.id} style={{ marginBottom: '8px' }}>
                                {action.description}
                              </li>
                            ))}
                          </ul>
                        </div>
                      )}
                    </div>
                  )}

                  {detail.rawText && (
                    <div className="detail-section">
                      <h4 className="section-title"><BookOpen size={16} /> Extracted Text Context</h4>
                      <details className="raw-text-details">
                        <summary>Expand raw parsed text ({detail.rawText.length} characters)</summary>
                        <div className="raw-text-content">{detail.rawText}</div>
                      </details>
                    </div>
                  )}
                </>
              )}
            </div>

            {/* Right Column: 1-on-1 Chat */}
            <div className="modal-sidebar-col" style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              <div className="sidebar-card" style={{ padding: '0.75rem 1rem' }}>
                <a 
                  href={detail.originalUrl} 
                  target="_blank" 
                  rel="noreferrer" 
                  className="btn btn-secondary-sm btn-full"
                  aria-label="Open Original Reference in new tab"
                >
                  Open Original Reference <ArrowUpRight size={14} />
                </a>
              </div>

              {!isProcessing && (
                <div className="chat-card card glass" style={{ flex: 1, display: 'flex', flexDirection: 'column', height: '100%', minHeight: '400px', margin: 0 }}>
                  <div className="chat-settings-bar" style={{ padding: '0.75rem', borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                    <span className="selector-title"><MessageSquare size={14} className="icon-purple" /> Memory Chat</span>
                  </div>

                  {/* Chat message bubbles area */}
                  <div className="chat-messages-viewport" style={{ padding: '1rem', flex: 1, maxHeight: '350px' }}>
                    {chatHistory.map((msg, index) => (
                      <div key={index} className={`chat-message-row ${msg.role}`}>
                        <div className="avatar-side">
                          {msg.role === 'user' ? (
                            <div className="user-avatar">ME</div>
                          ) : (
                            <div className="assistant-avatar"><Brain size={16} /></div>
                          )}
                        </div>

                        <div className="message-content-bubble">
                          {msg.role === 'assistant' ? (
                            <div 
                              className="markdown-body"
                              dangerouslySetInnerHTML={createMarkup(msg.text)}
                            />
                          ) : (
                            <p className="message-text" style={{ fontSize: '0.9rem' }}>{msg.text || ''}</p>
                          )}
                        </div>
                      </div>
                    ))}
                    
                    {loadingChat && (
                      <div className="chat-message-row assistant">
                        <div className="avatar-side">
                          <div className="assistant-avatar"><Brain size={16} /></div>
                        </div>
                        <div className="message-content-bubble loading">
                          <div className="typing-indicator">
                            <span></span><span></span><span></span>
                          </div>
                        </div>
                      </div>
                    )}
                    <div ref={chatEndRef} />
                  </div>

                  {/* Query Form */}
                  <form onSubmit={handleSendChat} className="chat-input-bar" style={{ padding: '0.75rem' }}>
                    <div className="chat-input-wrapper">
                      <input
                        type="text"
                        placeholder="Ask about this specific memory..."
                        value={query}
                        onChange={(e) => setQuery(e.target.value)}
                        disabled={loadingChat}
                        style={{ fontSize: '0.9rem', padding: '0.5rem 0.75rem' }}
                      />
                    </div>
                    <button 
                      type="submit" 
                      className="btn btn-primary send-btn"
                      disabled={loadingChat || !query.trim()}
                      style={{ padding: '0.5rem' }}
                      aria-label="Send message"
                      data-tooltip="Send message"
                    >
                      {loadingChat ? <RefreshCw size={14} className="spin" /> : <Send size={14} />}
                    </button>
                  </form>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
