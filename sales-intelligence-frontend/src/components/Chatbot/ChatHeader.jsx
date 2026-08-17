import { Sparkles, Minus, X } from 'lucide-react';

export default function ChatHeader({ onClose }) {
  return (
    <div className="chatbot-header">
      <div className="chatbot-header-info">
        <div className="chatbot-avatar-icon">
          <Sparkles size={18} color="#818cf8" />
        </div>
        <div>
          <h3>Sales AI Assistant</h3>
          <span className="chatbot-online-status">
            <span className="online-dot"></span> Online & Ready
          </span>
        </div>
      </div>
      <div className="chatbot-header-actions">
        <button onClick={onClose} title="Minimize" className="chatbot-icon-btn">
          <Minus size={16} />
        </button>
        <button onClick={onClose} title="Close" className="chatbot-icon-btn">
          <X size={16} />
        </button>
      </div>
    </div>
  );
}
