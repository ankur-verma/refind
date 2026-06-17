import { useState, useEffect, useRef } from 'react';
import { Search, Sparkles, Loader2, ArrowUpRight, Plus, Trash2, Send, MessageSquare, PanelLeftClose, PanelLeftOpen } from 'lucide-react';
import { request } from './api';
import { marked } from 'marked';
import DOMPurify from 'dompurify';

export default function SemanticSearchView({ onSelectContent }) {
  const [sessions, setSessions] = useState([]);
  const [activeSessionId, setActiveSessionId] = useState(null);
  const [messages, setMessages] = useState([]);
  const [query, setQuery] = useState('');
  const [isTyping, setIsTyping] = useState(false);
  const [userName, setUserName] = useState('');
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const messagesEndRef = useRef(null);

  useEffect(() => {
    try {
      const savedUser = localStorage.getItem('user');
      if (savedUser) {
        const userObj = JSON.parse(savedUser);
        const namePart = userObj.email.split('@')[0];
        setUserName(namePart.charAt(0).toUpperCase() + namePart.slice(1));
      }
    } catch (_) {}
    fetchSessions();
  }, []);

  useEffect(() => {
    if (activeSessionId) {
      fetchMessages(activeSessionId);
    } else {
      setMessages([]);
    }
  }, [activeSessionId]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages, isTyping]);

  const fetchSessions = async () => {
    const res = await request('/search/chats');
    if (res.success && res.data) {
      setSessions(res.data);
    }
  };

  const fetchMessages = async (sessionId) => {
    const res = await request(`/search/chats/${sessionId}/messages`);
    if (res.success && res.data) {
      setMessages(res.data);
    }
  };

  const handleCreateSession = async () => {
    const res = await request('/search/chats', {
      method: 'POST',
      body: JSON.stringify({ initialMessage: 'New Conversation' })
    });
    if (res.success && res.data) {
      setSessions([res.data, ...sessions]);
      setActiveSessionId(res.data.id);
    }
  };

  const handleDeleteSession = async (e, sessionId) => {
    e.stopPropagation();
    const res = await request(`/search/chats/${sessionId}`, {
      method: 'DELETE'
    });
    if (res.success) {
      setSessions(sessions.filter(s => s.id !== sessionId));
      if (activeSessionId === sessionId) {
        setActiveSessionId(null);
      }
    }
  };

  const handleSendMessage = async (e) => {
    e.preventDefault();
    if (!query.trim()) return;

    let targetSessionId = activeSessionId;
    const messageToSend = query.trim();
    setQuery('');

    // Create session if it doesn't exist
    if (!targetSessionId) {
      const res = await request('/search/chats', {
        method: 'POST',
        body: JSON.stringify({ initialMessage: messageToSend })
      });
      if (res.success && res.data) {
        targetSessionId = res.data.id;
        setSessions([res.data, ...sessions]);
        setActiveSessionId(targetSessionId);
      } else {
        return;
      }
    }

    // Optimistically add user message
    const tempUserMsg = {
      id: Date.now().toString(),
      role: 'User',
      content: messageToSend,
      createdAt: new Date().toISOString()
    };
    setMessages(prev => [...prev, tempUserMsg]);
    setIsTyping(true);

    const res = await request(`/search/chats/${targetSessionId}/messages`, {
      method: 'POST',
      body: JSON.stringify({ message: messageToSend })
    });

    setIsTyping(false);

    if (res.success && res.data) {
      // res.data is the assistant's message. We need to refetch to get the actual user message ID too, 
      // or just append the assistant message. Refetching is safer.
      fetchMessages(targetSessionId);
    } else {
      // Handle error gracefully
      setMessages(prev => prev.filter(m => m.id !== tempUserMsg.id));
    }
  };

  const renderMarkdown = (text) => {
    try {
      const html = marked.parse(text, { breaks: true, gfm: true });
      return { __html: DOMPurify.sanitize(html) };
    } catch (e) {
      return { __html: text };
    }
  };

  return (
    <div className="global-chat-layout">
      {/* SIDEBAR */}
      <div className={`chat-sidebar ${sidebarOpen ? '' : 'collapsed'}`}>
        <div className="chat-sidebar-header">
          <button className="new-chat-btn" onClick={handleCreateSession}>
            <Plus size={18} /> {sidebarOpen && 'New Chat'}
          </button>
          <button 
            className="sidebar-toggle-btn" 
            onClick={() => setSidebarOpen(!sidebarOpen)}
            aria-label={sidebarOpen ? 'Collapse sidebar' : 'Expand sidebar'}
          >
            {sidebarOpen ? <PanelLeftClose size={18} /> : <PanelLeftOpen size={18} />}
          </button>
        </div>
        {sidebarOpen && (
          <div className="chat-session-list">
            {sessions.map(session => (
              <div 
                key={session.id} 
                className={`chat-session-item ${activeSessionId === session.id ? 'active' : ''}`}
                onClick={() => setActiveSessionId(session.id)}
              >
                <MessageSquare size={16} className="mr-2 opacity-70" />
                <span className="session-title">{session.title}</span>
                <button 
                  className="delete-session-btn" 
                  onClick={(e) => handleDeleteSession(e, session.id)}
                  aria-label="Delete chat"
                >
                  <Trash2 size={14} />
                </button>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* MAIN CHAT AREA */}
      <div className="chat-main-area">
        {!activeSessionId && messages.length === 0 ? (
          // EMPTY STATE (Gemini Style)
          <div className="gemini-container centered" style={{ flex: 1, borderRadius: 0, border: 'none', boxShadow: 'none' }}>
            <div className="gemini-search-wrapper">
              <h1 className="gemini-hero-title">
                Your move, {userName || 'Ankur'}!
              </h1>
              <form onSubmit={handleSendMessage} className="gemini-search-bar">
                <input
                  type="text"
                  placeholder="Ask Refind..."
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                />
                <button type="submit" className="gemini-icon-btn" aria-label="Send Message" disabled={!query.trim()}>
                  <Send size={20} />
                </button>
              </form>
            </div>
          </div>
        ) : (
          // ACTIVE CHAT STATE
          <>
            <div className="chat-messages-container">
              {messages.map(msg => (
                <div key={msg.id} className={`message-bubble ${msg.role.toLowerCase()}`}>
                  {msg.role === 'User' ? (
                    <div style={{ whiteSpace: 'pre-wrap' }}>{msg.content}</div>
                  ) : (
                    <>
                      <div className="prose" dangerouslySetInnerHTML={renderMarkdown(msg.content)} />
                      {msg.sources && msg.sources.length > 0 && (
                        <div className="chat-sources">
                          {msg.sources.map((src, idx) => (
                            <span 
                              key={idx} 
                              className="source-chip"
                              onClick={() => onSelectContent(src.contentItemId)}
                              title={`Similarity: ${Math.round(src.similarityScore * 100)}%`}
                            >
                              <ArrowUpRight size={12} />
                              {src.title}
                            </span>
                          ))}
                        </div>
                      )}
                    </>
                  )}
                </div>
              ))}
              
              {isTyping && (
                <div className="message-bubble assistant">
                  <Loader2 size={20} className="spin text-purple" />
                </div>
              )}
              <div ref={messagesEndRef} />
            </div>

            <div className="chat-input-container">
              <form onSubmit={handleSendMessage} className="gemini-search-bar">
                <input
                  type="text"
                  placeholder="Ask Refind..."
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                />
                <button type="submit" className="gemini-icon-btn" aria-label="Send Message" disabled={!query.trim() || isTyping}>
                  <Send size={20} />
                </button>
              </form>
            </div>
          </>
        )}
      </div>
    </div>
  );
}
