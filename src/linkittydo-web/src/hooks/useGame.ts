import { useState, useCallback } from 'react';
import type { GameState, GuessResponse } from '../types';
import { api } from '../services/api';

export function useGame() {
  const [gameState, setGameState] = useState<GameState | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const startGame = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const state = await api.startGame();
      setGameState(state);
    } catch (err) {
      setError('Failed to start game. Is the server running?');
      console.error(err);
    } finally {
      setLoading(false);
    }
  }, []);

  const submitGuess = useCallback(async (wordIndex: number, guess: string): Promise<GuessResponse | null> => {
    if (!gameState) return null;
    
    try {
      const response = await api.submitGuess(gameState.sessionId, { wordIndex, guess });
      
      if (response.isCorrect) {
        // Refresh game state to get updated word reveals
        const updatedState = await api.getGame(gameState.sessionId);
        setGameState(updatedState);
      }
      
      return response;
    } catch (err) {
      setError('Failed to submit guess');
      console.error(err);
      return null;
    }
  }, [gameState]);

  const getClue = useCallback(async (wordIndex: number): Promise<string | null> => {
    if (!gameState) return null;
    
    try {
      const clue = await api.getClue(gameState.sessionId, wordIndex);
      return clue.url;
    } catch (err) {
      console.error(err);
      return null;
    }
  }, [gameState]);

  return {
    gameState,
    loading,
    error,
    startGame,
    submitGuess,
    getClue,
  };
}
