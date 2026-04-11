import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { GameHistoryPage } from '../pages/GameHistoryPage';
import type { GameRecord } from '../types';

// Mock useUser hook
vi.mock('../hooks/useUser', () => ({
  useUser: vi.fn(),
}));

// Mock api service
vi.mock('../services/api', () => ({
  api: {
    getUserGames: vi.fn(),
    getGameDetail: vi.fn(),
  },
}));

// Mock useNavigate
const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

import { useUser } from '../hooks/useUser';
import { api } from '../services/api';

const mockUseUser = vi.mocked(useUser);
const mockGetUserGames = vi.mocked(api.getUserGames);

const sampleGames: GameRecord[] = [
  {
    gameId: 'GAME-1-ABC',
    playedAt: '2026-04-08T10:00:00Z',
    completedAt: '2026-04-08T10:05:00Z',
    score: 500,
    phraseId: 1,
    phraseText: 'the quick brown fox',
    difficulty: 20,
    result: 'Solved',
    isCompleted: true,
    events: [
      { eventType: 'clue', timestamp: '2026-04-08T10:01:00Z', wordIndex: 1, searchTerm: 'fast', url: 'http://example.com' },
      { eventType: 'guess', timestamp: '2026-04-08T10:02:00Z', wordIndex: 1, guessText: 'quick', isCorrect: true, pointsAwarded: 100 },
      { eventType: 'gameend', timestamp: '2026-04-08T10:05:00Z', reason: 'solved' },
    ],
  },
  {
    gameId: 'GAME-2-DEF',
    playedAt: '2026-04-07T14:00:00Z',
    completedAt: '2026-04-07T14:03:00Z',
    score: 0,
    phraseId: 2,
    phraseText: 'a stitch in time',
    difficulty: 40,
    result: 'GaveUp',
    isCompleted: true,
    events: [
      { eventType: 'gameend', timestamp: '2026-04-07T14:03:00Z', reason: 'gaveup' },
    ],
  },
];

const guestUser = {
  user: { uniqueId: 'USR-1-ABC', name: 'Guest', email: '', lifetimePoints: 0, preferredDifficulty: 10 },
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

const registeredUser = {
  ...guestUser,
  user: { uniqueId: 'USR-2-XYZ', name: 'Alice', email: 'a@b.com', lifetimePoints: 1000, preferredDifficulty: 30 },
  isGuest: false,
};

describe('GameHistoryPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows guest message when not logged in', () => {
    mockUseUser.mockReturnValue(guestUser);

    render(
      <MemoryRouter>
        <GameHistoryPage />
      </MemoryRouter>
    );

    expect(screen.getByText('Create a profile to track your game history and see your progress over time.')).toBeInTheDocument();
    expect(screen.getByText('Go Play')).toBeInTheDocument();
  });

  it('shows loading state', () => {
    mockUseUser.mockReturnValue(registeredUser);
    mockGetUserGames.mockReturnValue(new Promise(() => {})); // never resolves

    render(
      <MemoryRouter>
        <GameHistoryPage />
      </MemoryRouter>
    );

    expect(screen.getByText('Loading your games...')).toBeInTheDocument();
  });

  it('shows empty state when no games', async () => {
    mockUseUser.mockReturnValue(registeredUser);
    mockGetUserGames.mockResolvedValue([]);

    render(
      <MemoryRouter>
        <GameHistoryPage />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('No games played yet. Time to get started!')).toBeInTheDocument();
    });
  });

  it('renders game list with correct data', async () => {
    mockUseUser.mockReturnValue(registeredUser);
    mockGetUserGames.mockResolvedValue(sampleGames);

    render(
      <MemoryRouter>
        <GameHistoryPage />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('2 games played')).toBeInTheDocument();
    });

    expect(screen.getByText('the quick brown fox')).toBeInTheDocument();
    expect(screen.getByText('a stitch in time')).toBeInTheDocument();
    expect(screen.getByText('500 pts')).toBeInTheDocument();
    expect(screen.getByText('0 pts')).toBeInTheDocument();
    expect(screen.getByText('Solved')).toBeInTheDocument();
    expect(screen.getByText('Gave Up')).toBeInTheDocument();
  });

  it('expands game detail to show event timeline', async () => {
    mockUseUser.mockReturnValue(registeredUser);
    mockGetUserGames.mockResolvedValue(sampleGames);
    vi.mocked(api.getGameDetail).mockResolvedValue(sampleGames[0]);

    render(
      <MemoryRouter>
        <GameHistoryPage />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('the quick brown fox')).toBeInTheDocument();
    });

    const user = userEvent.setup();
    // Click the first game card header to expand
    await user.click(screen.getByText('the quick brown fox'));

    await waitFor(() => {
      expect(screen.getByText('Event Timeline')).toBeInTheDocument();
    });
  });

  it('navigates to /play when Go Play is clicked (guest)', async () => {
    mockUseUser.mockReturnValue(guestUser);

    render(
      <MemoryRouter>
        <GameHistoryPage />
      </MemoryRouter>
    );

    const user = userEvent.setup();
    await user.click(screen.getByText('Go Play'));
    expect(mockNavigate).toHaveBeenCalledWith('/play');
  });
});
