import type { 
  GameState, 
  GuessRequest, 
  GuessResponse, 
  ClueResponse,
  CreateUserRequest,
  UpdateUserRequest,
  UserResponse,
  AvailabilityResponse,
  ErrorResponse,
  DifficultyResponse,
  PointsResponse,
  StartGameRequest,
  GameRecord,
  LlmTestRequest,
  LlmTestResponse
} from '../types';

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:5157/api').replace(/\/$/, '');

export const api = {
  // Game endpoints
  async startGame(request?: StartGameRequest): Promise<GameState> {
    const response = await fetch(`${API_BASE_URL}/game/start`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request ?? {}),
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

  async getClue(sessionId: string, wordIndex: number, excludedUrls: string[] = []): Promise<ClueResponse> {
    const params = new URLSearchParams();
    excludedUrls.forEach(url => params.append('excludeUrl', url));
    const queryString = params.toString();
    const url = `${API_BASE_URL}/clue/${sessionId}/${wordIndex}${queryString ? `?${queryString}` : ''}`;
    
    const response = await fetch(url);
    if (!response.ok) throw new Error('Failed to get clue');
    return response.json();
  },

  async giveUp(sessionId: string): Promise<GameState> {
    const response = await fetch(`${API_BASE_URL}/game/${sessionId}/give-up`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
    });
    if (!response.ok) throw new Error('Failed to give up');
    return response.json();
  },

  async getGameRecord(sessionId: string): Promise<GameRecord | null> {
    const response = await fetch(`${API_BASE_URL}/game/${sessionId}/record`);
    if (response.status === 404) return null;
    if (!response.ok) throw new Error('Failed to get game record');
    return response.json();
  },

  // User endpoints
  async getAllUsers(): Promise<UserResponse[]> {
    const response = await fetch(`${API_BASE_URL}/user`);
    if (!response.ok) throw new Error('Failed to get users');
    return response.json();
  },

  async createUser(request: CreateUserRequest): Promise<UserResponse> {
    const response = await fetch(`${API_BASE_URL}/user`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
    
    if (!response.ok) {
      const errorData: ErrorResponse = await response.json();
      throw new Error(errorData.error?.message || 'Failed to create user');
    }
    
    return response.json();
  },

  async getUser(uniqueId: string): Promise<UserResponse | null> {
    const response = await fetch(`${API_BASE_URL}/user/${uniqueId}`);
    if (response.status === 404) return null;
    if (!response.ok) throw new Error('Failed to get user');
    return response.json();
  },

  async getUserByEmail(email: string): Promise<UserResponse | null> {
    const response = await fetch(`${API_BASE_URL}/user/by-email/${encodeURIComponent(email)}`);
    if (response.status === 404) return null;
    if (!response.ok) throw new Error('Failed to get user');
    return response.json();
  },

  async updateUser(uniqueId: string, request: UpdateUserRequest): Promise<UserResponse> {
    const response = await fetch(`${API_BASE_URL}/user/${uniqueId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
    
    if (!response.ok) {
      const errorData: ErrorResponse = await response.json();
      throw new Error(errorData.error?.message || 'Failed to update user');
    }
    
    return response.json();
  },

  async deleteUser(uniqueId: string): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/user/${uniqueId}`, {
      method: 'DELETE',
    });
    if (!response.ok && response.status !== 204) {
      throw new Error('Failed to delete user');
    }
  },

  async updateDifficulty(uniqueId: string, difficulty: number): Promise<DifficultyResponse> {
    const response = await fetch(`${API_BASE_URL}/user/${uniqueId}/difficulty`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ difficulty }),
    });
    
    if (!response.ok) {
      const errorData: ErrorResponse = await response.json();
      throw new Error(errorData.error?.message || 'Failed to update difficulty');
    }
    
    return response.json();
  },

  async addPoints(uniqueId: string, points: number): Promise<PointsResponse> {
    const response = await fetch(`${API_BASE_URL}/user/${uniqueId}/points`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ points }),
    });
    
    if (!response.ok) {
      const errorData: ErrorResponse = await response.json();
      throw new Error(errorData.error?.message || 'Failed to add points');
    }
    
    return response.json();
  },

  async checkNameAvailability(name: string): Promise<boolean> {
    const response = await fetch(`${API_BASE_URL}/user/check-name/${encodeURIComponent(name)}`);
    if (!response.ok) throw new Error('Failed to check name availability');
    const data: AvailabilityResponse = await response.json();
    return data.available;
  },

  async checkEmailAvailability(email: string): Promise<boolean> {
    const response = await fetch(`${API_BASE_URL}/user/check-email/${encodeURIComponent(email)}`);
    if (!response.ok) throw new Error('Failed to check email availability');
    const data: AvailabilityResponse = await response.json();
    return data.available;
  },

  async getUserGames(uniqueId: string): Promise<GameRecord[]> {
    const response = await fetch(`${API_BASE_URL}/user/${uniqueId}/games`);
    if (!response.ok) throw new Error('Failed to get user games');
    return response.json();
  },

  // LLM endpoints
  async testLlm(request: LlmTestRequest): Promise<LlmTestResponse> {
    console.log('=== LLM Test Request ===');
    console.log('Prompt:', request.prompt);
    console.log('System Prompt:', request.systemPrompt ?? '(none)');

    const response = await fetch(`${API_BASE_URL}/llm/test`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });

    const data: LlmTestResponse = await response.json();

    console.log('=== LLM Test Response ===');
    console.log('Success:', data.success);
    console.log('Content:', data.content);
    console.log('Model:', data.model);
    console.log('Token Usage - Prompt:', data.promptTokens, 'Completion:', data.completionTokens, 'Total:', data.totalTokens);
    if (data.error) {
      console.error('Error:', data.error);
    }

    return data;
  },
};
