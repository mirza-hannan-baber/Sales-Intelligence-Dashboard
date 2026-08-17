import { useEffect, useRef } from 'react';
import ChatMessage from './ChatMessage';
import TypingIndicator from './TypingIndicator';
import SuggestedQuestions from './SuggestedQuestions';

export default function ChatMessages({ messages, isLoading, onSelectQuestion }) {
  const messagesEndRef = useRef(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages, isLoading]);

  const lastMessage = messages[messages.length - 1];
  const suggestedQuestions = lastMessage?.suggestedQuestions || [];

  return (
    <div className="chatbot-messages-body">
      {messages.map((msg) => (
        <ChatMessage key={msg.id} message={msg} />
      ))}

      {isLoading && <TypingIndicator />}

      <div ref={messagesEndRef} />

      {!isLoading && suggestedQuestions.length > 0 && (
        <SuggestedQuestions questions={suggestedQuestions} onSelectQuestion={onSelectQuestion} />
      )}
    </div>
  );
}
