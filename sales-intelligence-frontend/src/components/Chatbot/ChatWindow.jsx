import ChatHeader from './ChatHeader';
import ChatMessages from './ChatMessages';
import ChatInput from './ChatInput';

export default function ChatWindow({ isOpen, onClose, messages, isLoading, onSendMessage }) {
  if (!isOpen) return null;

  return (
    <div className="chatbot-window-container">
      <ChatHeader onClose={onClose} />
      <ChatMessages messages={messages} isLoading={isLoading} onSelectQuestion={onSendMessage} />
      <ChatInput onSendMessage={onSendMessage} disabled={isLoading} />
    </div>
  );
}
