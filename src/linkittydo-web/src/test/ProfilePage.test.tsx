import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { ProfilePage } from '../pages/ProfilePage';

const mockUseAuth = vi.fn();
vi.mock('../contexts/AuthContext', () => ({
  useAuth: () => mockUseAuth(),
}));

vi.mock('../services/api', () => ({
  api: {
    getUserProfile: vi.fn(),
  },
}));

import { api } from '../services/api';

const mockGetUserProfile = vi.mocked(api.getUserProfile);

const sampleProfile = {
  uniqueId: 'USR-123-ABC',
  name: 'TestPlayer',
  email: 'test@example.com',
  lifetimePoints: 1500,
  preferredDifficulty: 20,
  createdAt: '2026-01-01T00:00:00Z',
  gamesPlayed: 10,
  gamesSolved: 7,
  gamesGaveUp: 3,
  solveRate: 70,
  avgScore: 250,
  bestScore: 500,
  currentStreak: 3,
  bestStreak: 5,
  lastPlayedAt: '2026-04-10T10:00:00Z',
  recentGames: [
    {
      gameId: 'GAME-1-ABC',
      playedAt: '2026-04-10T10:00:00Z',
      completedAt: '2026-04-10T10:05:00Z',
      score: 300,
      phraseId: 1,
      phraseText: 'the quick brown fox',
      difficulty: 20,
      result: 'Solved',
      isCompleted: true,
      events: [],
    },
  ],
};

const renderProfile = () =>
  render(
    <MemoryRouter initialEntries={['/profile']}>
      <Routes>
        <Route path="/profile" element={<ProfilePage />} />
        <Route path="/play" element={<div>Play Page</div>} />
      </Routes>
    </MemoryRouter>
  );

describe('ProfilePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('redirects to /play when not authenticated', async () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, authUser: null });

    renderProfile();

    await waitFor(() => {
      expect(screen.getByText('Play Page')).toBeInTheDocument();
    });
  });

  it('shows loading state initially', () => {
    mockUseAuth.mockReturnValue({
      isAuthenticated: true,
      authUser: { uniqueId: 'USR-123-ABC' },
    });
    mockGetUserProfile.mockReturnValue(new Promise(() => {}));

    renderProfile();

    expect(screen.getByText('Loading profile...')).toBeInTheDocument();
  });

  it('renders profile data after loading', async () => {
    mockUseAuth.mockReturnValue({
      isAuthenticated: true,
      authUser: { uniqueId: 'USR-123-ABC' },
    });
    mockGetUserProfile.mockResolvedValue(sampleProfile);

    renderProfile();

    await waitFor(() => {
      expect(screen.getByText('TestPlayer')).toBeInTheDocument();
    });
    expect(screen.getByText('test@example.com')).toBeInTheDocument();
    expect(screen.getByText('1,500')).toBeInTheDocument();
  });

  it('displays stats grid', async () => {
    mockUseAuth.mockReturnValue({
      isAuthenticated: true,
      authUser: { uniqueId: 'USR-123-ABC' },
    });
    mockGetUserProfile.mockResolvedValue(sampleProfile);

    renderProfile();

    await waitFor(() => {
      expect(screen.getByText('Games Played')).toBeInTheDocument();
    });
    expect(screen.getByText('Games Solved')).toBeInTheDocument();
    expect(screen.getByText('Solve Rate')).toBeInTheDocument();
    expect(screen.getByText('Best Score')).toBeInTheDocument();
  });

  it('shows recent games', async () => {
    mockUseAuth.mockReturnValue({
      isAuthenticated: true,
      authUser: { uniqueId: 'USR-123-ABC' },
    });
    mockGetUserProfile.mockResolvedValue(sampleProfile);

    renderProfile();

    await waitFor(() => {
      expect(screen.getByText('Recent Games')).toBeInTheDocument();
    });
    expect(screen.getByText('the quick brown fox')).toBeInTheDocument();
  });

  it('shows error state on API failure', async () => {
    mockUseAuth.mockReturnValue({
      isAuthenticated: true,
      authUser: { uniqueId: 'USR-123-ABC' },
    });
    mockGetUserProfile.mockRejectedValue(new Error('Network error'));

    renderProfile();

    await waitFor(() => {
      expect(screen.getByText('Failed to load profile')).toBeInTheDocument();
    });
  });
});
