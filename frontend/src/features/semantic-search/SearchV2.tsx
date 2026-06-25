import { useState, useEffect, useRef } from 'react';
import { Search, Sparkles, Send, PanelLeft, Plus, MessageSquare, Trash2, ArrowUpRight, Activity } from 'lucide-react';
import { request } from '../../api';
import { ChatMapWidget, ChatCarouselWidget } from './components/ChatWidgets';
import { useProfileUpcomingPlans, useProfileInterests } from '../profile/api';

export default function SearchV2({ onSelectContent, hideSidebar = false }: any) {
  const { data: plansRes } = useProfileUpcomingPlans();
  const { data: interestsRes } = useProfileInterests();
  const safePlans = plansRes?.data || [];
  const safeInterests = interestsRes?.data || [];
  const [sessions, setSessions] = useState<any[]>([]);
  const [activeSessionId, setActiveSessionId] = useState<string | null>(null);
  const [messages, setMessages] = useState<any[]>([]);
  const [query, setQuery] = useState('');
  const [isTyping, setIsTyping] = useState(false);
  const [sidebarOpen, setSidebarOpen] = useState(!hideSidebar);
  const messagesEndRef = useRef<HTMLDivElement>(null);

  const hasInitializedFromUrl = useRef(false);

  useEffect(() => {
    fetchSessions().then(() => {
      const urlParams = new URLSearchParams(window.location.search);
      const initialQuery = urlParams.get('q');
      if (initialQuery && !hasInitializedFromUrl.current) {
        hasInitializedFromUrl.current = true;
        window.history.replaceState({}, '', window.location.pathname);
        setTimeout(() => handleSendMessage(undefined, initialQuery), 300);
      }
    });
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

  const fetchMessages = async (sessionId: string) => {
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

  const handleDeleteSession = async (e: React.MouseEvent, sessionId: string) => {
    e.stopPropagation();
    const res = await request(`/search/chats/${sessionId}`, { method: 'DELETE' });
    if (res.success) {
      setSessions(sessions.filter(s => s.id !== sessionId));
      if (activeSessionId === sessionId) setActiveSessionId(null);
    }
  };

  const executeMockInterceptor = (msg: string) => {
    const lowerMsg = msg.toLowerCase();
    
    if (lowerMsg.includes('cafe') && lowerMsg.includes('gurgaon')) {
      return {
        text: "I found 4 cafes you've saved in Gurgaon. Most of them are clustered around Cyber Hub and Golf Course Road.",
        widget: {
          type: 'map',
          data: { locations: ['Cyber Hub', 'Golf Course Road'] }
        }
      };
    }
    
    if (lowerMsg.includes('gadget')) {
      return {
        text: "You've been heavily researching mechanical keyboards and noise-cancelling headphones lately. Here are your top saved gadgets:",
        widget: {
          type: 'carousel',
          data: {
            items: [
              { id: 'mock-1', title: 'Sony WH-1000XM6 Rumors', platformType: 1, energyLevel: 1, summary: 'Next gen noise cancellation.', url: 'https://example.com/sony', thumbnailUrl: 'https://images.unsplash.com/photo-1618366712010-f4ae9c647dcb?q=80&w=800&auto=format&fit=crop' },
              { id: 'mock-2', title: 'Keychron Q1 Pro Review', platformType: 0, energyLevel: 1, summary: 'Wireless custom mechanical keyboard.', url: 'https://example.com/keychron', thumbnailUrl: 'https://images.unsplash.com/photo-1595225476474-87563907a212?q=80&w=800&auto=format&fit=crop' }
            ]
          }
        }
      };
    }

    if (lowerMsg.includes('weekend plans')) {
      return {
        text: "Based on your recent saves and the nice weather forecast, here are some perfect weekend plans:",
        widget: {
          type: 'carousel',
          data: {
            items: [
              { id: 'mock-3', title: 'Aravalli Biodiversity Park Hiking Trail', platformType: 1, energyLevel: 2, summary: 'A great morning hike near the city.', url: 'https://example.com/hike', thumbnailUrl: 'https://images.unsplash.com/photo-1551632811-561732d1e306?q=80&w=800&auto=format&fit=crop' },
              { id: 'mock-4', title: 'Artisan Coffee Tasting at Blue Tokai', platformType: 2, energyLevel: 0, summary: 'Relaxing afternoon coffee brewing workshop.', url: 'https://example.com/coffee', thumbnailUrl: 'https://images.unsplash.com/photo-1497935586351-b67a49e012bf?q=80&w=800&auto=format&fit=crop' }
            ]
          }
        }
      };
    }

    return null; // No mock matched
  };

  const handleSendMessage = async (e?: React.FormEvent, promptOverride?: string) => {
    if (e) e.preventDefault();
    const textToSend = promptOverride || query;
    if (!textToSend.trim()) return;

    let targetSessionId = activeSessionId;
    const messageToSend = textToSend.trim();
    setQuery('');

    // Telemetry: Track search
    import('../../shared/analytics/AnalyticsService').then(({ analytics }) => {
      analytics.track('Searched', undefined, { searchQuery: messageToSend });
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

    // Run Mock Interceptor for rich UI demo (Phase FE-8)
    const mockResponse = executeMockInterceptor(messageToSend);

    if (mockResponse) {
      // Simulate network delay for AI thinking
      setTimeout(() => {
        setIsTyping(false);
        setMessages(prev => [...prev, {
          id: (Date.now() + 1).toString(),
          role: 'Assistant',
          content: mockResponse.text,
          widget: mockResponse.widget
        }]);
      }, 1500);
      return;
    }

    // Fallback to real backend API (which only returns text right now)
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
    <div className="flex h-full w-full bg-background overflow-hidden relative">
      {/* Sidebar */}
      <div className={`${sidebarOpen ? 'w-64' : 'w-0'} flex-shrink-0 border-r border-border bg-card transition-all duration-300 ease-in-out overflow-hidden flex flex-col z-10`}>
        <div className="p-4 flex items-center justify-between border-b border-border/50">
          <button 
            className="flex flex-1 items-center gap-2 text-sm font-semibold hover:bg-secondary py-2.5 px-3 rounded-xl transition-colors text-foreground" 
            onClick={handleCreateSession}
          >
            <Plus size={16} /> New Chat
          </button>
          <button 
            className="p-2.5 ml-2 hover:bg-secondary rounded-xl transition-colors text-muted-foreground hover:text-foreground" 
            onClick={() => setSidebarOpen(false)}
          >
            <PanelLeft size={18} />
          </button>
        </div>

        <div className="flex-1 overflow-y-auto p-3 space-y-1 no-scrollbar">
          {sessions.map(s => (
            <div 
              key={s.id} 
              onClick={() => setActiveSessionId(s.id)} 
              className={`flex items-center justify-between group cursor-pointer p-3 rounded-xl text-sm transition-all ${
                activeSessionId === s.id 
                  ? 'bg-secondary text-foreground font-semibold shadow-sm' 
                  : 'text-muted-foreground hover:bg-secondary/50 hover:text-foreground'
              }`}
            >
              <div className="flex items-center gap-3 truncate">
                <MessageSquare size={16} className={activeSessionId === s.id ? 'text-primary' : ''} /> 
                <span className="truncate">{s.title}</span>
              </div>
              <button 
                onClick={(e) => handleDeleteSession(e, s.id)} 
                className="opacity-0 group-hover:opacity-100 hover:text-destructive transition-opacity p-1 rounded-md hover:bg-background"
              >
                <Trash2 size={14} />
              </button>
            </div>
          ))}
        </div>
      </div>

      {/* Main Chat Area */}
      <div className="flex-1 flex flex-col relative h-full min-w-0 bg-background">
        {/* Floating Sidebar Toggle (When sidebar is hidden) */}
        {!sidebarOpen && !hideSidebar && (
          <button 
            className="absolute top-4 left-4 z-20 p-2.5 bg-card/80 backdrop-blur-md border border-border shadow-sm rounded-xl hover:bg-secondary transition-colors text-foreground" 
            onClick={() => setSidebarOpen(true)}
          >
            <PanelLeft size={18} />
          </button>
        )}

        <div className="flex-1 overflow-y-auto scroll-smooth pb-32 w-full">
          {!activeSessionId && messages.length === 0 ? (
            <div className="flex flex-col items-center justify-center h-full max-w-3xl mx-auto px-6 py-12">
              <div className="w-16 h-16 bg-primary/10 rounded-2xl flex items-center justify-center mb-6 shadow-sm border border-primary/20">
                <Sparkles size={32} className="text-primary" />
              </div>
              <h1 className="text-4xl md:text-5xl font-black tracking-tight mb-4 text-foreground text-center">Refind AI</h1>
              <p className="text-muted-foreground text-center mb-10 max-w-lg text-lg leading-relaxed">
                Your personal intelligence assistant. Ask about your saved memories, collections, or request new recommendations.
              </p>
              
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 w-full">
                {safePlans.slice(0, 2).map((plan: any) => (
                  <button 
                    key={plan.id}
                    onClick={() => handleSendMessage(undefined, `Help me plan: ${plan.title}`)} 
                    className="bg-card border border-border/50 hover:border-primary/30 hover:bg-secondary/50 p-4 rounded-2xl text-left transition-all hover:shadow-sm group"
                  >
                    <p className="text-sm font-semibold text-foreground mb-1 group-hover:text-primary transition-colors flex items-center gap-2">{plan.icon} {plan.title}</p>
                    <p className="text-xs text-muted-foreground">{plan.description}</p>
                  </button>
                ))}
                {safeInterests.slice(0, 2).map((interest: any) => (
                  <button 
                    key={interest.topic}
                    onClick={() => handleSendMessage(undefined, `Show me deep dives about ${interest.topic}`)} 
                    className="bg-card border border-border/50 hover:border-primary/30 hover:bg-secondary/50 p-4 rounded-2xl text-left transition-all hover:shadow-sm group"
                  >
                    <p className="text-sm font-semibold text-foreground mb-1 group-hover:text-primary transition-colors">Deep dive into {interest.topic}</p>
                    <p className="text-xs text-muted-foreground">Based on your recent saves</p>
                  </button>
                ))}

                {/* Fallbacks if no data */}
                {safePlans.length === 0 && safeInterests.length === 0 && (
                   <>
                     <button 
                       onClick={() => handleSendMessage(undefined, "Show cafes I saved in Gurgaon.")} 
                       className="bg-card border border-border/50 hover:border-primary/30 hover:bg-secondary/50 p-4 rounded-2xl text-left transition-all hover:shadow-sm group"
                     >
                       <p className="text-sm font-semibold text-foreground mb-1 group-hover:text-primary transition-colors">Show cafes I saved in Gurgaon</p>
                       <p className="text-xs text-muted-foreground">Search through your saved locations</p>
                     </button>
                     <button 
                       onClick={() => handleSendMessage(undefined, "What gadgets am I interested in?")} 
                       className="bg-card border border-border/50 hover:border-primary/30 hover:bg-secondary/50 p-4 rounded-2xl text-left transition-all hover:shadow-sm group"
                     >
                       <p className="text-sm font-semibold text-foreground mb-1 group-hover:text-primary transition-colors">What gadgets am I interested in?</p>
                       <p className="text-xs text-muted-foreground">Analyze your recent tech saves</p>
                     </button>
                   </>
                )}
              </div>
            </div>
          ) : (
            <div className="max-w-3xl mx-auto px-4 py-8 space-y-8 w-full">
              {messages.map(msg => {
                const isUser = msg.role.toLowerCase() === 'user';
                return (
                  <div key={msg.id} className={`flex w-full ${isUser ? 'justify-end' : 'justify-start'}`}>
                    <div 
                      className={`max-w-[85%] md:max-w-[75%] rounded-3xl px-5 py-4 shadow-sm ${
                        isUser 
                          ? 'bg-primary text-primary-foreground rounded-tr-sm' 
                          : 'bg-card border border-border/60 text-foreground rounded-tl-sm'
                      }`}
                    >
                      <div className="text-[15px] leading-relaxed whitespace-pre-wrap">{msg.content}</div>
                      
                      {/* Render Rich Widgets if present */}
                      <div className="mt-4">
                        {msg.widget && msg.widget.type === 'map' && (
                          <div className="bg-background rounded-xl overflow-hidden border border-border/50"><ChatMapWidget data={msg.widget.data} /></div>
                        )}
                        {msg.widget && msg.widget.type === 'carousel' && (
                          <ChatCarouselWidget data={msg.widget.data} onSelectContent={onSelectContent} />
                        )}
                      </div>

                      {msg.sources && msg.sources.length > 0 && (
                        <div className="mt-4 flex flex-wrap gap-2 pt-3 border-t border-border/20">
                          {msg.sources.map((src: any, i: number) => (
                            <span 
                              key={i} 
                              className="text-xs bg-secondary/80 hover:bg-secondary text-secondary-foreground px-2.5 py-1.5 rounded-lg flex items-center gap-1 cursor-pointer transition-colors border border-border/30" 
                              onClick={() => onSelectContent(src.contentItemId)}
                            >
                              <ArrowUpRight size={12} className="text-muted-foreground" /> 
                              <span className="font-medium truncate max-w-[150px]">{src.title}</span>
                            </span>
                          ))}
                        </div>
                      )}
                    </div>
                  </div>
                );
              })}
              {isTyping && (
                <div className="flex w-full justify-start">
                  <div className="bg-card border border-border/60 rounded-3xl rounded-tl-sm px-5 py-4 shadow-sm flex items-center gap-2">
                    <Activity size={18} className="text-primary animate-pulse" />
                    <span className="text-sm font-medium text-muted-foreground">Assistant is thinking...</span>
                  </div>
                </div>
              )}
              <div ref={messagesEndRef} className="h-4" />
            </div>
          )}
        </div>

        {/* Input Area */}
        <div className="absolute bottom-0 left-0 right-0 p-4 md:p-6 bg-gradient-to-t from-background via-background/95 to-transparent pt-12 z-20">
          <div className="max-w-3xl mx-auto relative">
            <form onSubmit={handleSendMessage} className="relative flex items-center shadow-lg rounded-2xl bg-card border border-border/80 focus-within:ring-2 focus-within:ring-primary/50 focus-within:border-primary transition-all">
              <input 
                type="text" 
                className="w-full bg-transparent py-4 pl-5 pr-14 text-[15px] text-foreground focus:outline-none placeholder:text-muted-foreground"
                placeholder="Ask your assistant anything..."
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                disabled={isTyping}
              />
              <button 
                type="submit" 
                className="absolute right-2 p-2.5 bg-primary text-primary-foreground rounded-xl hover:bg-primary/90 transition-transform active:scale-95 disabled:opacity-50 disabled:active:scale-100 flex items-center justify-center" 
                disabled={!query.trim() || isTyping}
              >
                <Send size={18} className="translate-x-[1px] translate-y-[1px]" />
              </button>
            </form>
            <div className="text-center mt-2">
              <span className="text-[10px] text-muted-foreground font-medium uppercase tracking-widest">Refind AI can make mistakes. Check important info.</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
