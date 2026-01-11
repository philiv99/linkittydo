import React, { useState } from 'react';
import { api } from '../services/api';
import './LlmTestButton.css';

export const LlmTestButton: React.FC = () => {
  const [loading, setLoading] = useState(false);
  const [response, setResponse] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleTestLlm = async () => {
    setLoading(true);
    setError(null);
    setResponse(null);

    try {
      console.log('Testing LLM with "Hello world!" prompt...');
      const result = await api.testLlm({ prompt: 'Hello world!' });
      
      if (result.success) {
        setResponse(result.content ?? 'No content returned');
      } else {
        setError(result.error ?? 'Unknown error');
      }
    } catch (err) {
      const errorMessage = err instanceof Error ? err.message : 'Failed to test LLM';
      setError(errorMessage);
      console.error('LLM test error:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="llm-test-container">
      <button 
        className="llm-test-button" 
        onClick={handleTestLlm}
        disabled={loading}
      >
        {loading ? '🔄 Testing...' : '🤖 Test LLM'}
      </button>
      {response && (
        <div className="llm-response success">
          <strong>Response:</strong> {response}
        </div>
      )}
      {error && (
        <div className="llm-response error">
          <strong>Error:</strong> {error}
        </div>
      )}
    </div>
  );
};
