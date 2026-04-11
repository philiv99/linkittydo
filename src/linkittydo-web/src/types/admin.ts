export interface DashboardStats {
  totalUsers: number;
  totalGamesPlayed: number;
  totalGamesSolved: number;
  totalGamesGaveUp: number;
  totalPhrases: number;
  activePhrases: number;
  simulatedUsers: number;
  simulatedGames: number;
  overallSolveRate: number;
  avgScore: number;
  gamesPlayedToday: number;
  newUsersToday: number;
  computedAt: string;
}

export interface AdminUser {
  uniqueId: string;
  name: string;
  email: string;
  lifetimePoints: number;
  preferredDifficulty: number;
  isActive: boolean;
  isSimulated: boolean;
  createdAt: string;
}

export interface PaginatedResponse<T> {
  data: T[];
  pagination: {
    page: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

export interface PlayerAnalytics {
  userId: string;
  gamesPlayed: number;
  gamesSolved: number;
  gamesGaveUp: number;
  avgScore: number;
  avgCluesPerGame: number;
  avgGuessesPerGame: number;
  bestScore: number;
  currentStreak: number;
  bestStreak: number;
  lastPlayedAt: string | null;
  computedAt: string;
}

export interface AdminGame {
  gameId: string;
  userId: string;
  playerName: string;
  phraseText: string;
  difficulty: number;
  result: string;
  score: number;
  isSimulated: boolean;
  playedAt: string;
  completedAt: string | null;
}

export interface GameDetail extends AdminGame {
  eventCount: number;
  events: GameEventSummary[];
}

export interface GameEventSummary {
  id: number;
  eventType: string;
  sequenceNumber: number;
  timestamp: string;
  // Clue event fields
  wordIndex?: number;
  phraseWord?: string;
  searchTerm?: string;
  url?: string;
  relationshipType?: string;
  // Guess event fields
  guessText?: string;
  isCorrect?: boolean;
  pointsAwarded?: number;
  // Game end fields
  reason?: string;
}

export interface SiteConfigEntry {
  key: string;
  value: string;
  valueType: string;
  description: string | null;
  updatedAt: string;
  updatedBy: string | null;
}

export interface DataSummary {
  totalUsers: number;
  totalPhrases: number;
  totalGames: number;
  totalEvents: number;
  simulatedUsers: number;
  simulatedGames: number;
}

export interface SimulationSummary {
  totalSimulatedUsers: number;
  totalSimulatedGames: number;
  simulatedGamesSolved: number;
  simulatedGamesGaveUp: number;
  simulatedSolveRate: number;
  avgSimulatedScore: number;
}

export interface PlayerDetail {
  user: AdminUser;
  stats: PlayerAnalytics | null;
  recentGames: {
    gameId: string;
    phraseText: string;
    result: string;
    score: number;
    playedAt: string;
  }[];
}

export interface AdminPhrase {
  uniqueId: string;
  text: string;
  wordCount: number;
  difficulty: number;
  isActive: boolean;
  generatedByLlm: boolean;
  createdAt: string;
}

export interface PhraseStats {
  phraseUniqueId: string;
  timesPlayed: number;
  timesSolved: number;
  timesGaveUp: number;
  solveRate: number;
  avgCluesToSolve: number | null;
  avgTimeToSolveSeconds: number | null;
  giveUpRate: number;
  calibratedDifficulty: number | null;
  lastComputedAt: string;
}

export interface UserRoles {
  uniqueId: string;
  roles: string[];
}

export interface AuditLogEntry {
  id: number;
  userId: string | null;
  action: string;
  entityType: string | null;
  entityId: string | null;
  details: string | null;
  ipAddress: string | null;
  timestamp: string;
}
