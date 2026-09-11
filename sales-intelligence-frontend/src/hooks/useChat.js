import { useState, useCallback } from 'react';
import { chatService } from '../services/api';
import { useDataset } from '../context/DatasetContext';

export function useChat() {
  const { selectedDatasetId } = useDataset();
  const [messages, setMessages] = useState([
    {
      id: 'welcome-msg',
      role: 'assistant',
      content: "Hi! I'm your AI Sales Intelligence Assistant. Ask me anything about your sales data — revenue, win rates, top employees, lost deals, industry comparisons, and more. I query your CRM live for every question.",
      type: 'text',
      timestamp: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
    },
  ]);
  const [isOpen, setIsOpen] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState(null);

  const sendMessage = useCallback(async (text) => {
    if (!text || !text.trim()) return;
    const userText = text.trim();
    const timestamp = new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

    const userMessage = {
      id: `user-${Date.now()}`,
      role: 'user',
      content: userText,
      type: 'text',
      timestamp,
    };

    setMessages((prev) => [...prev, userMessage]);
    setIsLoading(true);
    setError(null);

    try {
      const response = await chatService.sendMessage(userText, selectedDatasetId);
      const aiMessage = {
        id: `ai-${Date.now()}`,
        role: 'assistant',
        content: response.answer || 'Here are the insights from your sales data.',
        type: 'text',
        sqlUsed: response.sqlUsed || response.sql_used || '',
        timestamp: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
      };

      setMessages((prev) => [...prev, aiMessage]);
    } catch (err) {
      console.error('Chat error:', err);
      setError('Unable to reach the Sales AI Assistant. Ensure backend is running.');
      setMessages((prev) => [
        ...prev,
        {
          id: `ai-err-${Date.now()}`,
          role: 'assistant',
          content: 'Sorry, I had trouble analyzing that request. Unable to connect to the backend service. Please make sure the backend is running.',
          type: 'text',
          timestamp: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
        },
      ]);
    } finally {
      setIsLoading(false);
    }
  }, [selectedDatasetId]);

  const toggleOpen = useCallback(() => {
    setIsOpen((prev) => !prev);
  }, []);

  return {
    messages,
    isOpen,
    isLoading,
    error,
    sendMessage,
    toggleOpen,
    setIsOpen,
  };
}
