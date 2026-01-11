import { useState, useCallback, useRef } from 'react';
import type { GameState, GuessResponse, StartGameRequest } from '../types';
import { api } from '../services/api';

const EXCLUDED_URLS_KEY = 'linkittydo_excluded_urls';
const MAX_CLUE_RETRIES = 5;

// Load excluded URLs from localStorage
const getExcludedUrls = (): Set<string> => {
  try {
    const stored = localStorage.getItem(EXCLUDED_URLS_KEY);
    if (stored) {
      return new Set(JSON.parse(stored));
    }
  } catch (e) {
    console.warn('Failed to load excluded URLs:', e);
  }
  return new Set();
};

// Save excluded URLs to localStorage
const saveExcludedUrls = (urls: Set<string>): void => {
  try {
    localStorage.setItem(EXCLUDED_URLS_KEY, JSON.stringify([...urls]));
  } catch (e) {
    console.warn('Failed to save excluded URLs:', e);
  }
};

// Validate that a URL is accessible
const validateUrl = async (url: string): Promise<boolean> => {
  try {
    // Use a HEAD request with no-cors mode to check if URL is reachable
    // Note: Due to CORS, we can't get the actual status, but we can detect network failures
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 5000); // 5 second timeout

    await fetch(url, {
      method: 'HEAD',
      mode: 'no-cors', // This will return opaque response but won't throw on CORS
      signal: controller.signal,
    });

    clearTimeout(timeoutId);
    
    // With no-cors, response.type will be 'opaque' for successful requests
    // If the request fails entirely, it will throw
    return true;
  } catch (error) {
    console.warn(`URL validation failed for ${url}:`, error);
    return false;
  }
};

export function useGame() {
  const [gameState, setGameState] = useState<GameState | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const excludedUrlsRef = useRef<Set<string>>(getExcludedUrls());

  const startGame = useCallback(async (request?: StartGameRequest) => {
    setLoading(true);
    setError(null);
    try {
      const state = await api.startGame(request);
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
    
    const sessionExcluded: string[] = [];
    
    for (let attempt = 0; attempt < MAX_CLUE_RETRIES; attempt++) {
      try {
        // Combine persisted excluded URLs with session-specific exclusions
        const allExcluded = [...excludedUrlsRef.current, ...sessionExcluded];
        const clue = await api.getClue(gameState.sessionId, wordIndex, allExcluded);
        
        if (!clue.url) {
          console.warn('No clue URL returned');
          return null;
        }

        // Validate the URL
        const isValid = await validateUrl(clue.url);
        
        if (isValid) {
          return clue.url;
        }

        // URL is invalid, add to exclusion lists
        console.warn(`Clue URL invalid, excluding: ${clue.url}`);
        excludedUrlsRef.current.add(clue.url);
        saveExcludedUrls(excludedUrlsRef.current);
        sessionExcluded.push(clue.url);
        
      } catch (err) {
        console.error('Failed to get clue:', err);
        return null;
      }
    }

    console.error(`Failed to find valid clue after ${MAX_CLUE_RETRIES} attempts`);
    return null;
  }, [gameState]);

  const giveUp = useCallback(async (): Promise<void> => {
    if (!gameState) return;
    
    try {
      const state = await api.giveUp(gameState.sessionId);
      setGameState(state);
    } catch (err) {
      setError('Failed to give up');
      console.error(err);
    }
  }, [gameState]);

  return {
    gameState,
    loading,
    error,
    startGame,
    submitGuess,
    getClue,
    giveUp,
  };
}
