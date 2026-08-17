import { useChat } from '../../hooks/useChat';
import ChatbotButton from './ChatbotButton';
import ChatWindow from './ChatWindow';

export default function Chatbot() {
  const { messages, isOpen, isLoading, sendMessage, toggleOpen, setIsOpen } = useChat();

  return (
    <div className="chatbot-root-wrapper">
      <ChatWindow
        isOpen={isOpen}
        onClose={() => setIsOpen(false)}
        messages={messages}
        isLoading={isLoading}
        onSendMessage={sendMessage}
      />
      <ChatbotButton isOpen={isOpen} onClick={toggleOpen} />
    </div>
  );
}
