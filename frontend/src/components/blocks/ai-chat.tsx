import * as React from "react"
import { Brain, Send, Activity } from "lucide-react"

export interface Message {
  id: string
  role: 'user' | 'assistant'
  content: React.ReactNode
}

interface AIChatProps {
  messages: Message[]
  isTyping?: boolean
  onSendMessage: (text: string) => void
  placeholder?: string
  className?: string
}

export function AIChat({ messages, isTyping = false, onSendMessage, placeholder = "Ask AI...", className = "" }: AIChatProps) {
  const [input, setInput] = React.useState("")
  const scrollRef = React.useRef<HTMLDivElement>(null)

  React.useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight
    }
  }, [messages, isTyping])

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!input.trim() || isTyping) return
    onSendMessage(input)
    setInput("")
  }

  return (
    <div className={`flex flex-col h-full bg-card border border-border rounded-2xl overflow-hidden ${className}`}>
      
      {/* Messages Viewport */}
      <div ref={scrollRef} className="flex-1 overflow-y-auto p-4 space-y-6 custom-scrollbar">
        {messages.map((msg) => (
          <div key={msg.id} className={`flex gap-3 ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
            {msg.role === 'assistant' && (
              <div className="w-8 h-8 rounded-full bg-primary/20 flex items-center justify-center flex-shrink-0 mt-1">
                <Brain size={16} className="text-primary" />
              </div>
            )}
            
            <div className={`max-w-[85%] rounded-2xl px-4 py-3 ${
              msg.role === 'user' 
                ? 'bg-primary text-primary-foreground rounded-tr-sm' 
                : 'bg-secondary text-secondary-foreground rounded-tl-sm'
            }`}>
              {msg.content}
            </div>
          </div>
        ))}

        {isTyping && (
          <div className="flex gap-3 justify-start">
            <div className="w-8 h-8 rounded-full bg-primary/20 flex items-center justify-center flex-shrink-0 mt-1">
              <Brain size={16} className="text-primary" />
            </div>
            <div className="bg-secondary rounded-2xl rounded-tl-sm px-4 py-3 flex items-center gap-1 h-12">
              <span className="w-2 h-2 rounded-full bg-muted-foreground/50 animate-bounce" style={{ animationDelay: '0ms' }} />
              <span className="w-2 h-2 rounded-full bg-muted-foreground/50 animate-bounce" style={{ animationDelay: '150ms' }} />
              <span className="w-2 h-2 rounded-full bg-muted-foreground/50 animate-bounce" style={{ animationDelay: '300ms' }} />
            </div>
          </div>
        )}
      </div>

      {/* Input Area */}
      <div className="p-4 bg-background/50 border-t border-border ">
        <form onSubmit={handleSubmit} className="relative flex items-center">
          <input
            type="text"
            value={input}
            onChange={(e) => setInput(e.target.value)}
            placeholder={placeholder}
            disabled={isTyping}
            className="w-full bg-secondary border border-border rounded-full pl-5 pr-12 py-3.5 text-sm focus:outline-none focus:ring-2 focus:ring-primary/50 transition-all placeholder:text-muted-foreground"
          />
          <button 
            type="submit" 
            disabled={!input.trim() || isTyping}
            className="absolute right-2 p-2 bg-primary text-primary-foreground rounded-full disabled:opacity-50 disabled:cursor-not-allowed hover:bg-primary/90 transition-colors"
          >
            {isTyping ? <Activity size={16} className="animate-spin" /> : <Send size={16} />}
          </button>
        </form>
      </div>
      
    </div>
  )
}
