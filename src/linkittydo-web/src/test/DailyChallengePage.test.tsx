import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { DailyChallengePage } from '../pages/DailyChallengePage';

const mockUseAuth = vi.fn();
vi.mock('../contexts/AuthContext', () => ({
  useAuth: () => mockUseAuth(),
}));

const mockUseUser = vi.fn();
vi.mock('../hooks/useUser', () => ({
  useUser: () => mockUseUser(),
}));

vi.mock('../services/api', () => ({
  api: {
    getDailyChallenge: vi.fn(),
    startDailyChallenge: vi.fn(),
    getDailyChallengeLeaderboard: vi.fn(),
  },
}));

import { api } from '../services/api';

const mockGetDailyChallenge = vi.mocked(api.getDailyChallenge);
const mockStartDailyChallenge = vi.mocked(api.startDailyChallenge);
const mockGetLeaderboard = vi.mocked(api.getDailyChallengeLeaderboard);

const renderDaily = () =>
  render(
    <MemoryRouter>
      <DailyChallengePage />
    </MemoryRouter>
  );

describe('DailyChallengePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockUseAuth.mockReturnValue({ isAuthenticated: true, authUser: { uniqueId: 'USR-123-ABC' } });
    mockUseUser.mockReturnValue({ user: { preferredDifficulty: 20 } });
  });

  it('shows loading state initially', () => {
    mockGetDailyChallenge.mockReturnValue(new Promise(() => {}));
    mockGetLeaderboard.mockReturnValue(new Promise(() => {}));

    renderDaily();

    expect(screen.getByText(/loading today/i)).toBeInTheDocument();
  });

  it('renders challenge not yet played', async () => {
    mockGetDailyChallenge.mockResolvedValue({
      date: new Date().toISOString(),
      phraseUniqueId: 'PH-1',
      alreadyPlayed: false,
    });
    mockGetLeaderboard.mockResolvedValue([]);

    renderDaily();

    await waitFor(() => {
      expect(screen.getByText('Daily Challenge')).toBeInTheDocument();
    });
    expect(screen.getByText(/everyone gets the same phrase/i)).toBeInTheDocument();
    expect(screen.getByText("Play Today's Challenge")).toBeInTheDocument();
  });

  it('renders already played state with score', async () => {
    mockGetDailyChallenge.mockResolvedValue({
      date: new Date().toISOString(),
      phraseUniqueId: 'PH-1',
      alreadyPlayed: true,
      previousResult: {
        score: 350,
        result: 'Solved',
        completedAt: new Date().toISOString(),
      },
    });
    mockGetLeaderboard.mockResolvedValue([]);

    renderDaily();

    await waitFor(() => {
      expect(screen.getByText('Challenge Complete!')).toBeInTheDocument();
    });
    expect(screen.getByText('350')).toBeInTheDocument();
    expect(screen.getByText(/come back tomorrow/i)).toBeInTheDocument();
  });

  it('renders leaderboard entries', async () => {
    mockGetDailyChallenge.mockResolvedValue({
      date: new Date().toISOString(),
      phraseUniqueId: 'PH-1',
      alreadyPlayed: false,
    });
    mockGetLeaderboard.mockResolvedValue([
      { rank: 1, playerName: 'Alice', score: 500, result: 'Solved' },
      { rank: 2, playerName: 'Bob', score: 300, result: 'Solved' },
    ]);

    renderDaily();

    await waitFor(() => {
      expect(screen.getByText("Today's Leaderboard")).toBeInTheDocument();
    });
    expect(screen.getByText('Alice')).toBeInTheDocument();
    expect(screen.getByText('Bob')).toBeInTheDocument();
  });

  it('shows login hint for unauthenticated users', async () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, authUser: null });

    mockGetDailyChallenge.mockResolvedValue({
      date: new Date().toISOString(),
      phraseUniqueId: 'PH-1',
      alreadyPlayed: false,
    });
    mockGetLeaderboard.mockResolvedValue([]);

    renderDaily();

    await waitFor(() => {
      expect(screen.getByText(/log in to track/i)).toBeInTheDocument();
    });
  });

  it('shows error when start fails', async () => {
    mockGetDailyChallenge.mockResolvedValue({
      date: new Date().toISOString(),
      phraseUniqueId: 'PH-1',
      alreadyPlayed: false,
    });
    mockGetLeaderboard.mockResolvedValue([]);
    mockStartDailyChallenge.mockRejectedValue(new Error('Server error'));

    renderDaily();

    await waitFor(() => {
      expect(screen.getByText("Play Today's Challenge")).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText("Play Today's Challenge"));

    await waitFor(() => {
      expect(screen.getByText('Server error')).toBeInTheDocument();
    });
  });
});
