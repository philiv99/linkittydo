import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { GameBoard } from '../components/GameBoard';

// Mock useGame hook
vi.mock('../hooks/useGame', () => ({
  useGame: vi.fn(),
}));

// Mock useUser hook
vi.mock('../hooks/useUser', () => ({
  useUser: vi.fn(),
}));

// Mock useAudio hook
vi.mock('../hooks/useAudio', () => ({
  useAudioSequence: vi.fn(() => ({
    playSequence: vi.fn(),
    stopAll: vi.fn(),
  })),
  useAudio: vi.fn(() => ({
    play: vi.fn(),
    stop: vi.fn(),
  })),
}));

// Mock useSoundEffects hook
vi.mock('../hooks/useSoundEffects', () => ({
  useSoundEffects: vi.fn(() => ({
    playCorrect: vi.fn(),
    playIncorrect: vi.fn(),
    playSolved: vi.fn(),
    playGaveUp: vi.fn(),
    setMuted: vi.fn(),
    isMuted: vi.fn(() => false),
  })),
}));

// Mock api service
vi.mock('../services/api', () => ({
  api: {
    startGame: vi.fn(),
    getUserByEmail: vi.fn(),
  },
}));

import { useGame } from '../hooks/useGame';
import { useUser } from '../hooks/useUser';

const mockUseGame = vi.mocked(useGame);
const mockUseUser = vi.mocked(useUser);

const defaultGameState = {
  sessionId: 'test-session',
  phraseText: 'the quick brown fox',
  words: [
    { index: 0, displayText: 'the', isHidden: false, isRevealed: true },
    { index: 1, displayText: '???', isHidden: true, isRevealed: false },
    { index: 2, displayText: '???', isHidden: true, isRevealed: false },
    { index: 3, displayText: '???', isHidden: true, isRevealed: false },
  ],
  score: 0,
  isComplete: false,
};

const defaultUserState = {
  user: { uniqueId: '', name: 'Guest', email: '', lifetimePoints: 0, preferredDifficulty: 10, games: [], createdAt: '' },
  isGuest: true,
  isAdmin: false,
  allUsers: [],
  loading: false,
  error: null,
  registerUser: vi.fn(),
  loginUser: vi.fn(),
  updateUser: vi.fn(),
  switchUser: vi.fn(),
  updateDifficulty: vi.fn(),
  addPoints: vi.fn(),
  checkNameAvailability: vi.fn(),
  checkEmailAvailability: vi.fn(),
  resetToGuest: vi.fn(),
  signOut: vi.fn(),
  clearError: vi.fn(),
  fetchAllUsers: vi.fn(),
};

beforeEach(() => {
  vi.clearAllMocks();
  mockUseUser.mockReturnValue(defaultUserState as ReturnType<typeof useUser>);
});

describe('GameBoard Layout', () => {
  it('renders phrase bar, clue area, and footer sections', () => {
    mockUseGame.mockReturnValue({
      gameState: defaultGameState,
      loading: false,
      error: null,
      startGame: vi.fn(),
      submitGuess: vi.fn(),
      getClue: vi.fn(),
      giveUp: vi.fn(),
    } as ReturnType<typeof useGame>);

    const { container } = render(
      <MemoryRouter>
        <GameBoard />
      </MemoryRouter>
    );

    expect(container.querySelector('.game-layout')).toBeInTheDocument();
    expect(container.querySelector('.phrase-bar')).toBeInTheDocument();
    expect(container.querySelector('.clue-area')).toBeInTheDocument();
    expect(container.querySelector('.game-footer')).toBeInTheDocument();
  });

  it('displays version in footer', () => {
    mockUseGame.mockReturnValue({
      gameState: defaultGameState,
      loading: false,
      error: null,
      startGame: vi.fn(),
      submitGuess: vi.fn(),
      getClue: vi.fn(),
      giveUp: vi.fn(),
    } as ReturnType<typeof useGame>);

    render(
      <MemoryRouter>
        <GameBoard />
      </MemoryRouter>
    );

    expect(screen.getByText(/LinkittyDo v/)).toBeInTheDocument();
  });

  it('shows game status in footer', () => {
    mockUseGame.mockReturnValue({
      gameState: defaultGameState,
      loading: false,
      error: null,
      startGame: vi.fn(),
      submitGuess: vi.fn(),
      getClue: vi.fn(),
      giveUp: vi.fn(),
    } as ReturnType<typeof useGame>);

    render(
      <MemoryRouter>
        <GameBoard />
      </MemoryRouter>
    );

    expect(screen.getByText(/words? remaining/)).toBeInTheDocument();
  });

  it('shows keyboard shortcuts in footer', () => {
    mockUseGame.mockReturnValue({
      gameState: defaultGameState,
      loading: false,
      error: null,
      startGame: vi.fn(),
      submitGuess: vi.fn(),
      getClue: vi.fn(),
      giveUp: vi.fn(),
    } as ReturnType<typeof useGame>);

    render(
      <MemoryRouter>
        <GameBoard />
      </MemoryRouter>
    );

    expect(screen.getByText('G')).toBeInTheDocument();
    expect(screen.getByText('N')).toBeInTheDocument();
  });

  it('shows loading state when game is starting', () => {
    mockUseGame.mockReturnValue({
      gameState: null,
      loading: true,
      error: null,
      startGame: vi.fn(),
      submitGuess: vi.fn(),
      getClue: vi.fn(),
      giveUp: vi.fn(),
    } as ReturnType<typeof useGame>);

    render(
      <MemoryRouter>
        <GameBoard />
      </MemoryRouter>
    );

    expect(screen.getByText('Finding the perfect phrase...')).toBeInTheDocument();
  });

  it('shows score display in phrase bar during active game', () => {
    mockUseGame.mockReturnValue({
      gameState: defaultGameState,
      loading: false,
      error: null,
      startGame: vi.fn(),
      submitGuess: vi.fn(),
      getClue: vi.fn(),
      giveUp: vi.fn(),
    } as ReturnType<typeof useGame>);

    const { container } = render(
      <MemoryRouter>
        <GameBoard />
      </MemoryRouter>
    );

    expect(container.querySelector('.phrase-bar-stats')).toBeInTheDocument();
  });

  it('renders mute toggle button in footer', () => {
    mockUseGame.mockReturnValue({
      gameState: defaultGameState,
      loading: false,
      error: null,
      startGame: vi.fn(),
      submitGuess: vi.fn(),
      getClue: vi.fn(),
      giveUp: vi.fn(),
    } as ReturnType<typeof useGame>);

    render(
      <MemoryRouter>
        <GameBoard />
      </MemoryRouter>
    );

    const muteBtn = screen.getByRole('button', { name: /mute sound effects/i });
    expect(muteBtn).toBeInTheDocument();
    expect(muteBtn).toHaveTextContent('Sound On');
  });
});
