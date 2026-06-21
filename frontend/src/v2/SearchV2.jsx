import { useState, useEffect, useRef } from 'react';
import { Search, Sparkles, Send, PanelLeft, Plus, MessageSquare, Trash2, ArrowUpRight, Activity } from 'lucide-react';
import { request } from '../api';

export default function SearchV2({ onSelectContent }) {
  const [sessions, setSessions] = useState([]);
  const [activeSessionId, setActiveSessionId] = useState(null);
  const [messages, setMessages] = useState([]);
  const [query, setQuery] = useState('');
  const [isTyping, setIsTyping] = useState(false);
  const [sidebarOpen, setSidebarOpen] = useState(true);
  const messagesEndRef = useRef(null);

  useEffect(() => {
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
    if (res.success && res.data) setSessions(res.data);
  };

  const fetchMessages = async (sessionId) => {
    const res = await request(`/search/chats/${sessionId}/messages`);
    if (res.success && res.data) setMessages(res.data);
  };

  const handleCreateSession = async () => {
    const res = await request('/search/chats', {
      method: 'POST', body: JSON.stringify({ initialMessage: 'New Conversation' })
    });
    if (res.success && res.data) {
      setSessions([res.data, ...sessions]);
      setActiveSessionId(res.data.id);
    }
  };

  const handleDeleteSession = async (e, sessionId) => {
    e.stopPropagation();
    const res = await request(`/search/chats/${sessionId}`, { method: 'DELETE' });
    if (res.success) {
      setSessions(sessions.filter(s => s.id !== sessionId));
      if (activeSessionId === sessionId) setActiveSessionId(null);
    }
  };

  const handleSendMessage = async (e) => {
    e.preventDefault();
    if (!query.trim()) return;

    let targetSessionId = activeSessionId;
    const messageToSend = query.trim();
    setQuery('');

    // Telemetry: Track search
    import('../utils/telemetry').then(({ trackInteraction }) => {
      trackInteraction({
        interactionType: 'Search',
        searchQuery: messageToSend
      });
    });

    if (!targetSessionId) {
      const res = await request('/search/chats', {
        method: 'POST', body: JSON.stringify({ initialMessage: messageToSend })
      });
      if (res.success && res.data) {
        targetSessionId = res.data.id;
        setSessions([res.data, ...sessions]);
        setActiveSessionId(targetSessionId);
      } else return;
    }

    const tempUserMsg = {
      id: Date.now().toString(), role: 'User', content: messageToSend
    };
    setMessages(prev => [...prev, tempUserMsg]);
    setIsTyping(true);

    const res = await request(`/search/chats/${targetSessionId}/messages`, {
      method: 'POST', body: JSON.stringify({ message: messageToSend })
    });

    setIsTyping(false);
    if (res.success) {
      fetchMessages(targetSessionId);
    } else {
      setMessages(prev => prev.filter(m => m.id !== tempUserMsg.id));
    }
  };

  return (
    <div className="v2-search">
      <div className={`v2-chat-sidebar ${sidebarOpen ? 'open' : 'closed'}`}>
        <div className="v2-cs-header">
          <button className="v2-cs-new" onClick={handleCreateSession}>
            <Plus size={16} /> New Chat
          </button>
          <button className="v2-cs-toggle" onClick={() => setSidebarOpen(!sidebarOpen)}>
            <PanelLeft size={18} />
          </button>
        </div>

        {sidebarOpen && (
          <div className="v2-cs-list">
            {sessions.map(s => (
              <div 
                key={s.id} 
                className={`v2-cs-item ${activeSessionId === s.id ? 'active' : ''}`}
                onClick={() => setActiveSessionId(s.id)}
              >
                <MessageSquare size={14} />
                <span>{s.title}</span>
                <button onClick={(e) => handleDeleteSession(e, s.id)}><Trash2 size={12} /></button>
              </div>
            ))}
          </div>
        )}
      </div>

      <div className="v2-chat-main">
        {!sidebarOpen && (
          <button className="v2-cs-toggle v2-cs-toggle-floating" onClick={() => setSidebarOpen(true)}>
            <PanelLeft size={18} />
          </button>
        )}

        {!activeSessionId && messages.length === 0 ? (
          <div className="v2-chat-hero">
            <Sparkles size={48} className="v2-hero-icon" />
            <h1 className="v2-huge-title">Ask Refind</h1>
            <p className="v2-subtitle">Query your intelligence network.</p>
          </div>
        ) : (
          <div className="v2-chat-messages">
            {messages.map(msg => (
              <div key={msg.id} className={`v2-msg-bubble ${msg.role === 'User' ? 'user' : 'assistant'}`}>
                <div className="v2-msg-content">{msg.content}</div>
                {msg.sources && msg.sources.length > 0 && (
                  <div className="v2-msg-sources">
                    {msg.sources.map((src, i) => (
                      <span key={i} className="v2-src-chip" onClick={() => onSelectContent(src.contentItemId)}>
                        <ArrowUpRight size={10} /> {src.title}
                      </span>
                    ))}
                  </div>
                )}
              </div>
            ))}
            {isTyping && (
              <div className="v2-msg-bubble assistant typing">
                <Activity size={16} className="v2-pulse-icon" />
              </div>
            )}
            <div ref={messagesEndRef} />
          </div>
        )}

        <div className="v2-chat-input-area">
          <form onSubmit={handleSendMessage} className="v2-chat-form">
            <input 
              type="text" 
              className="v2-chat-input"
              placeholder="Ask anything..."
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              disabled={isTyping}
            />
            <button type="submit" className="v2-chat-send" disabled={!query.trim() || isTyping}>
              <Send size={16} />
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
