import type { GameState, GuessRequest, GuessResponse, ClueResponse } from '../types';

const API_BASE_URL = 'http://localhost:5157/api';

export const api = {
  async startGame(): Promise<GameState> {
    const response = await fetch(`${API_BASE_URL}/game/start`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
    });
    if (!response.ok) throw new Error('Failed to start game');
    return response.json();
  },

  async getGame(sessionId: string): Promise<GameState> {
    const response = await fetch(`${API_BASE_URL}/game/${sessionId}`);
    if (!response.ok) throw new Error('Failed to get game');
    return response.json();
  },

  async submitGuess(sessionId: string, request: GuessRequest): Promise<GuessResponse> {
    const response = await fetch(`${API_BASE_URL}/game/${sessionId}/guess`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
    if (!response.ok) throw new Error('Failed to submit guess');
    return response.json();
  },

  async getClue(sessionId: string, wordIndex: number): Promise<ClueResponse> {
    const response = await fetch(`${API_BASE_URL}/clue/${sessionId}/${wordIndex}`);
    if (!response.ok) throw new Error('Failed to get clue');
    return response.json();
  },
};
