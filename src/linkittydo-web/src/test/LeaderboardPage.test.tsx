import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { LeaderboardPage } from '../pages/LeaderboardPage';
import { api } from '../services/api';

vi.mock('../services/api');

describe('LeaderboardPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('shows loading state initially', () => {
    vi.mocked(api.getLeaderboard).mockReturnValue(new Promise(() => {}));
    render(
      <MemoryRouter>
        <LeaderboardPage />
      </MemoryRouter>
    );
    expect(screen.getByText('Loading rankings...')).toBeDefined();
  });

  it('renders leaderboard entries after loading', async () => {
    vi.mocked(api.getLeaderboard).mockResolvedValue([
      { rank: 1, name: 'Alice', lifetimePoints: 1000, gamesPlayed: 5 },
      { rank: 2, name: 'Bob', lifetimePoints: 500, gamesPlayed: 3 },
    ]);

    render(
      <MemoryRouter>
        <LeaderboardPage />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Alice')).toBeDefined();
    });
    expect(screen.getByText('Bob')).toBeDefined();
    expect(screen.getByText('1,000')).toBeDefined();
    expect(screen.getByText('500')).toBeDefined();
  });

  it('shows empty message when no players exist', async () => {
    vi.mocked(api.getLeaderboard).mockResolvedValue([]);

    render(
      <MemoryRouter>
        <LeaderboardPage />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('No players yet. Be the first to play!')).toBeDefined();
    });
  });

  it('shows error message on API failure', async () => {
    vi.mocked(api.getLeaderboard).mockRejectedValue(new Error('API Error'));

    render(
      <MemoryRouter>
        <LeaderboardPage />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('Failed to load leaderboard')).toBeDefined();
    });
  });

  it('displays medal icons for top 3 ranks', async () => {
    vi.mocked(api.getLeaderboard).mockResolvedValue([
      { rank: 1, name: 'Gold', lifetimePoints: 3000, gamesPlayed: 10 },
      { rank: 2, name: 'Silver', lifetimePoints: 2000, gamesPlayed: 8 },
      { rank: 3, name: 'Bronze', lifetimePoints: 1000, gamesPlayed: 5 },
    ]);

    render(
      <MemoryRouter>
        <LeaderboardPage />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByText('🥇')).toBeDefined();
      expect(screen.getByText('🥈')).toBeDefined();
      expect(screen.getByText('🥉')).toBeDefined();
    });
  });

  it('has proper ARIA attributes', async () => {
    vi.mocked(api.getLeaderboard).mockResolvedValue([
      { rank: 1, name: 'Alice', lifetimePoints: 1000, gamesPlayed: 5 },
    ]);

    render(
      <MemoryRouter>
        <LeaderboardPage />
      </MemoryRouter>
    );

    await waitFor(() => {
      expect(screen.getByRole('main')).toBeDefined();
      expect(screen.getByRole('table')).toBeDefined();
    });
  });
});
