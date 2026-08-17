import { Sparkles, X } from 'lucide-react';

export default function ChatbotButton({ isOpen, onClick }) {
  return (
    <button
      onClick={onClick}
      className={`chatbot-trigger-btn ${isOpen ? 'active' : ''}`}
      title={isOpen ? 'Close Sales AI' : 'Ask Sales AI'}
    >
      {isOpen ? (
        <X size={24} color="#ffffff" />
      ) : (
        <>
          <Sparkles size={24} color="#ffffff" className="chatbot-sparkle-icon" />
          <span className="chatbot-btn-badge">AI</span>
        </>
      )}
    </button>
  );
}
