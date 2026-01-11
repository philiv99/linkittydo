export interface WordState {
  index: number;
  displayText: string | null;
  isHidden: boolean;
  isRevealed: boolean;
}

export interface GameState {
  sessionId: string;
  words: WordState[];
  score: number;
  isComplete: boolean;
}

export interface GuessRequest {
  wordIndex: number;
  guess: string;
}

export interface GuessResponse {
  isCorrect: boolean;
  isPhraseComplete: boolean;
  currentScore: number;
  revealedWord: string | null;
}

export interface ClueResponse {
  url: string;
  searchTerm: string;
}

// Game Event types
export type GameEventType = 'clue' | 'guess' | 'gameend';

export interface GameEventBase {
  eventType: GameEventType;
  timestamp: string;
}

export interface ClueEvent extends GameEventBase {
  eventType: 'clue';
  wordIndex: number;
  searchTerm: string;
  url: string;
}

export interface GuessEvent extends GameEventBase {
  eventType: 'guess';
  wordIndex: number;
  guessText: string;
  isCorrect: boolean;
  pointsAwarded: number;
}

export type GameEndReason = 'solved' | 'gaveup';

export interface GameEndEvent extends GameEventBase {
  eventType: 'gameend';
  reason: GameEndReason;
}

export type GameEvent = ClueEvent | GuessEvent | GameEndEvent;

export type GameResult = 'InProgress' | 'Solved' | 'GaveUp';

export interface GameRecord {
  gameId: string;
  playedAt: string;
  completedAt?: string;
  score: number;
  phraseId: number;
  phraseText: string;
  difficulty: number;
  result: GameResult;
  events: GameEvent[];
  isCompleted: boolean;
}

export interface StartGameRequest {
  userId?: string;
  difficulty?: number;
}

// User types
export interface User {
  uniqueId: string;
  name: string;
  email: string;
  lifetimePoints: number;
  preferredDifficulty: number;
  createdAt?: string;
}

export interface CreateUserRequest {
  name: string;
  email: string;
}

export interface UpdateUserRequest {
  name: string;
  email: string;
}

export interface UserResponse {
  uniqueId: string;
  name: string;
  email: string;
  lifetimePoints: number;
  preferredDifficulty: number;
  gamesPlayed: number;
  createdAt: string;
}

export interface UpdateDifficultyRequest {
  difficulty: number;
}

export interface DifficultyResponse {
  uniqueId: string;
  preferredDifficulty: number;
}

export interface AddPointsRequest {
  points: number;
}

export interface PointsResponse {
  uniqueId: string;
  lifetimePoints: number;
  pointsAdded: number;
}

export interface AvailabilityResponse {
  available: boolean;
}

export interface ErrorResponse {
  error: {
    code: string;
    message: string;
  };
}

// LLM types
export interface LlmTestRequest {
  prompt: string;
  systemPrompt?: string;
}

export interface LlmTestResponse {
  success: boolean;
  content?: string;
  model?: string;
  promptTokens: number;
  completionTokens: number;
  totalTokens: number;
  error?: string;
}
