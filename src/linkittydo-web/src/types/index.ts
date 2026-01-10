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
