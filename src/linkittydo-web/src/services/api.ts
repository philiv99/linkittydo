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
  LeaderboardEntry,
  LlmTestRequest,
  LlmTestResponse,
  ApiResponse,
  AuthResponse,
  RegisterRequest,
  LoginRequest,
} from '../types';

// Ensure the API base URL always ends with /api
const getApiBaseUrl = (): string => {
  const baseUrl = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:5157/api').replace(/\/$/, '');
  // Append /api if not already present
  return baseUrl.endsWith('/api') ? baseUrl : `${baseUrl}/api`;
};

const API_BASE_URL = getApiBaseUrl();

const TOKEN_KEY = 'linkittydo_token';
const REFRESH_TOKEN_KEY = 'linkittydo_refresh_token';

export const getStoredToken = (): string | null => localStorage.getItem(TOKEN_KEY);
export const getStoredRefreshToken = (): string | null => localStorage.getItem(REFRESH_TOKEN_KEY);
export const storeTokens = (accessToken: string, refreshToken: string): void => {
  localStorage.setItem(TOKEN_KEY, accessToken);
  localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
};
export const clearTokens = (): void => {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
};

const authHeaders = (): Record<string, string> => {
  const token = getStoredToken();
  return token ? { 'Authorization': `Bearer ${token}` } : {};
};

export const api = {
  // Auth endpoints
  async register(request: RegisterRequest): Promise<AuthResponse> {
    const response = await fetch(`${API_BASE_URL}/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
    if (!response.ok) {
      const errorData: ErrorResponse = await response.json();
      throw new Error(errorData.error?.message || 'Registration failed');
    }
    const wrapper: ApiResponse<AuthResponse> = await response.json();
    storeTokens(wrapper.data.accessToken, wrapper.data.refreshToken);
    return wrapper.data;
  },

  async login(request: LoginRequest): Promise<AuthResponse> {
    const response = await fetch(`${API_BASE_URL}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
    if (!response.ok) {
      const errorData: ErrorResponse = await response.json();
      throw new Error(errorData.error?.message || 'Login failed');
    }
    const wrapper: ApiResponse<AuthResponse> = await response.json();
    storeTokens(wrapper.data.accessToken, wrapper.data.refreshToken);
    return wrapper.data;
  },

  async refreshToken(): Promise<AuthResponse | null> {
    const refreshToken = getStoredRefreshToken();
    if (!refreshToken) return null;
    const response = await fetch(`${API_BASE_URL}/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken }),
    });
    if (!response.ok) {
      clearTokens();
      return null;
    }
    const wrapper: ApiResponse<AuthResponse> = await response.json();
    storeTokens(wrapper.data.accessToken, wrapper.data.refreshToken);
    return wrapper.data;
  },

  logout(): void {
    clearTokens();
  },

  // Game endpoints
  async startGame(request?: StartGameRequest): Promise<GameState> {
    const response = await fetch(`${API_BASE_URL}/game/start`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request ?? {}),
    });
    if (!response.ok) throw new Error('Failed to start game');
    const wrapper: ApiResponse<GameState> = await response.json();
    return wrapper.data;
  },

  async getGame(sessionId: string): Promise<GameState> {
    const response = await fetch(`${API_BASE_URL}/game/${sessionId}`);
    if (!response.ok) throw new Error('Failed to get game');
    const wrapper: ApiResponse<GameState> = await response.json();
    return wrapper.data;
  },

  async submitGuess(sessionId: string, request: GuessRequest): Promise<GuessResponse> {
    const response = await fetch(`${API_BASE_URL}/game/${sessionId}/guess`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request),
    });
    if (!response.ok) throw new Error('Failed to submit guess');
    const wrapper: ApiResponse<GuessResponse> = await response.json();
    return wrapper.data;
  },

  async getClue(sessionId: string, wordIndex: number, excludedUrls: string[] = []): Promise<ClueResponse> {
    const params = new URLSearchParams();
    excludedUrls.forEach(url => params.append('excludeUrl', url));
    const queryString = params.toString();
    const url = `${API_BASE_URL}/clue/${sessionId}/${wordIndex}${queryString ? `?${queryString}` : ''}`;
    
    const response = await fetch(url);
    if (!response.ok) throw new Error('Failed to get clue');
    const wrapper: ApiResponse<ClueResponse> = await response.json();
    return wrapper.data;
  },

  async giveUp(sessionId: string): Promise<GameState> {
    const response = await fetch(`${API_BASE_URL}/game/${sessionId}/give-up`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
    });
    if (!response.ok) throw new Error('Failed to give up');
    const wrapper: ApiResponse<GameState> = await response.json();
    return wrapper.data;
  },

  async getGameRecord(sessionId: string): Promise<GameRecord | null> {
    const response = await fetch(`${API_BASE_URL}/game/${sessionId}/record`);
    if (response.status === 404) return null;
    if (!response.ok) throw new Error('Failed to get game record');
    const wrapper: ApiResponse<GameRecord> = await response.json();
    return wrapper.data;
  },

  // User endpoints
  async getAllUsers(): Promise<UserResponse[]> {
    const response = await fetch(`${API_BASE_URL}/user`);
    if (!response.ok) throw new Error('Failed to get users');
    const wrapper: ApiResponse<UserResponse[]> = await response.json();
    return wrapper.data;
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
    
    const wrapper: ApiResponse<UserResponse> = await response.json();
    return wrapper.data;
  },

  async getUser(uniqueId: string): Promise<UserResponse | null> {
    const response = await fetch(`${API_BASE_URL}/user/${uniqueId}`);
    if (response.status === 404) return null;
    if (!response.ok) throw new Error('Failed to get user');
    const wrapper: ApiResponse<UserResponse> = await response.json();
    return wrapper.data;
  },

  async getUserByEmail(email: string): Promise<UserResponse | null> {
    const response = await fetch(`${API_BASE_URL}/user/by-email/${encodeURIComponent(email)}`);
    if (response.status === 404) return null;
    if (!response.ok) throw new Error('Failed to get user');
    const wrapper: ApiResponse<UserResponse> = await response.json();
    return wrapper.data;
  },

  async updateUser(uniqueId: string, request: UpdateUserRequest): Promise<UserResponse> {
    const response = await fetch(`${API_BASE_URL}/user/${uniqueId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', ...authHeaders() },
      body: JSON.stringify(request),
    });
    
    if (!response.ok) {
      const errorData: ErrorResponse = await response.json();
      throw new Error(errorData.error?.message || 'Failed to update user');
    }
    
    const wrapper: ApiResponse<UserResponse> = await response.json();
    return wrapper.data;
  },

  async deleteUser(uniqueId: string): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/user/${uniqueId}`, {
      method: 'DELETE',
      headers: { ...authHeaders() },
    });
    if (!response.ok && response.status !== 204) {
      throw new Error('Failed to delete user');
    }
  },

  async updateDifficulty(uniqueId: string, difficulty: number): Promise<DifficultyResponse> {
    const response = await fetch(`${API_BASE_URL}/user/${uniqueId}/difficulty`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json', ...authHeaders() },
      body: JSON.stringify({ difficulty }),
    });
    
    if (!response.ok) {
      const errorData: ErrorResponse = await response.json();
      throw new Error(errorData.error?.message || 'Failed to update difficulty');
    }
    
    const wrapper: ApiResponse<DifficultyResponse> = await response.json();
    return wrapper.data;
  },

  async addPoints(uniqueId: string, points: number): Promise<PointsResponse> {
    const response = await fetch(`${API_BASE_URL}/user/${uniqueId}/points`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', ...authHeaders() },
      body: JSON.stringify({ points }),
    });
    
    if (!response.ok) {
      const errorData: ErrorResponse = await response.json();
      throw new Error(errorData.error?.message || 'Failed to add points');
    }
    
    const wrapper: ApiResponse<PointsResponse> = await response.json();
    return wrapper.data;
  },

  async checkNameAvailability(name: string): Promise<boolean> {
    const response = await fetch(`${API_BASE_URL}/user/check-name/${encodeURIComponent(name)}`);
    if (!response.ok) throw new Error('Failed to check name availability');
    const wrapper: ApiResponse<AvailabilityResponse> = await response.json();
    return wrapper.data.available;
  },

  async checkEmailAvailability(email: string): Promise<boolean> {
    const response = await fetch(`${API_BASE_URL}/user/check-email/${encodeURIComponent(email)}`);
    if (!response.ok) throw new Error('Failed to check email availability');
    const wrapper: ApiResponse<AvailabilityResponse> = await response.json();
    return wrapper.data.available;
  },

  async getUserGames(uniqueId: string): Promise<GameRecord[]> {
    const response = await fetch(`${API_BASE_URL}/user/${uniqueId}/games`, {
      headers: { ...authHeaders() },
    });
    if (!response.ok) throw new Error('Failed to get user games');
    const wrapper: ApiResponse<GameRecord[]> = await response.json();
    return wrapper.data;
  },

  async getLeaderboard(top: number = 10): Promise<LeaderboardEntry[]> {
    const response = await fetch(`${API_BASE_URL}/user/leaderboard?top=${top}`);
    if (!response.ok) throw new Error('Failed to get leaderboard');
    const wrapper: ApiResponse<LeaderboardEntry[]> = await response.json();
    return wrapper.data;
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

    const wrapper: ApiResponse<LlmTestResponse> = await response.json();
    const data = wrapper.data;

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
