export default function TypingIndicator() {
  return (
    <div className="chat-message assistant-message typing-indicator-wrapper">
      <div className="typing-dots">
        <span></span>
        <span></span>
        <span></span>
      </div>
      <span className="typing-text">AI is analyzing your sales data...</span>
    </div>
  );
}
