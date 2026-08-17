import { Sparkles, User } from 'lucide-react';
import { ForecastCard, CorrelationCard, RecommendationCard, DataAnalysisCard } from './ChatResultCards';

export default function ChatMessage({ message }) {
  const isUser = message.role === 'user';

  return (
    <div className={`chat-message ${isUser ? 'user-message' : 'assistant-message'}`}>
      <div className="chat-avatar">
        {isUser ? <User size={14} color="#ffffff" /> : <Sparkles size={14} color="#818cf8" />}
      </div>
      <div className="chat-bubble-content">
        <div className="chat-text">{message.content}</div>

        {message.sqlUsed && (
          <div style={{ marginTop: '8px', padding: '6px 8px', background: '#0f172a', borderRadius: '4px', border: '1px solid #334155', fontSize: '0.72rem', color: '#94a3b8', fontFamily: 'monospace', wordBreak: 'break-all' }}>
            <span style={{ color: '#818cf8', fontWeight: 'bold' }}>SQL: </span>{message.sqlUsed}
          </div>
        )}

        {/* Structured Result Cards */}
        {message.type === 'forecast' && message.data && <ForecastCard data={message.data} />}
        {message.type === 'correlation_analysis' && message.data && <CorrelationCard data={message.data} />}
        {message.type === 'business_recommendation' && message.data && <RecommendationCard data={message.data} />}
        {message.type === 'data_analysis' && message.data && <DataAnalysisCard data={message.data} />}

        <span className="chat-timestamp">{message.timestamp}</span>
      </div>
    </div>
  );
}
