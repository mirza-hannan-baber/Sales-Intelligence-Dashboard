import { useState } from 'react';
import { Send } from 'lucide-react';

export default function ChatInput({ onSendMessage, disabled }) {
  const [text, setText] = useState('');

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!text.trim() || disabled) return;
    onSendMessage(text);
    setText('');
  };

  const handleKeyDown = (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit(e);
    }
  };

  return (
    <form className="chatbot-input-form" onSubmit={handleSubmit}>
      <input
        type="text"
        className="chatbot-input"
        placeholder="Ask about revenue, forecasts, win rates..."
        value={text}
        onChange={(e) => setText(e.target.value)}
        onKeyDown={handleKeyDown}
        disabled={disabled}
      />
      <button
        type="submit"
        className="chatbot-send-btn"
        disabled={!text.trim() || disabled}
        title="Send Message"
      >
        <Send size={16} />
      </button>
    </form>
  );
}
