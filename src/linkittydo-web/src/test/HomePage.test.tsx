import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { HomePage } from '../pages/HomePage';

// Mock useUser hook
vi.mock('../hooks/useUser', () => ({
  useUser: vi.fn(),
}));

// Mock api service
vi.mock('../services/api', () => ({
  api: {
    getUserGames: vi.fn(),
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

describe('HomePage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockGetUserGames.mockResolvedValue([]);
  });

  it('renders hero section with title and play button', () => {
    mockUseUser.mockReturnValue({
      user: { uniqueId: 'USR-1-ABC', name: 'Guest', email: '', lifetimePoints: 0, preferredDifficulty: 10 },
      isGuest: true,
      allUsers: [],
      loading: false,
      error: null,
      registerUser: vi.fn(),
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
    });

    render(
      <MemoryRouter>
        <HomePage />
      </MemoryRouter>
    );

    expect(screen.getByText('LinkittyDo!')).toBeInTheDocument();
    expect(screen.getByText('Play Now')).toBeInTheDocument();
  });

  it('shows how to play section', () => {
    mockUseUser.mockReturnValue({
      user: { uniqueId: 'USR-1-ABC', name: 'Guest', email: '', lifetimePoints: 0, preferredDifficulty: 10 },
      isGuest: true,
      allUsers: [],
      loading: false,
      error: null,
      registerUser: vi.fn(),
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
    });

    render(
      <MemoryRouter>
        <HomePage />
      </MemoryRouter>
    );

    expect(screen.getByText('How to Play')).toBeInTheDocument();
    expect(screen.getByText('See the Phrase')).toBeInTheDocument();
    expect(screen.getByText('Get Clues')).toBeInTheDocument();
    expect(screen.getByText('Guess & Score')).toBeInTheDocument();
  });

  it('does not show stats section for guest users', () => {
    mockUseUser.mockReturnValue({
      user: { uniqueId: 'USR-1-ABC', name: 'Guest', email: '', lifetimePoints: 0, preferredDifficulty: 10 },
      isGuest: true,
      allUsers: [],
      loading: false,
      error: null,
      registerUser: vi.fn(),
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
    });

    render(
      <MemoryRouter>
        <HomePage />
      </MemoryRouter>
    );

    expect(screen.queryByText('Your Stats')).not.toBeInTheDocument();
  });

  it('shows stats section for registered users', async () => {
    mockUseUser.mockReturnValue({
      user: { uniqueId: 'USR-1-ABC', name: 'Alice', email: 'a@b.com', lifetimePoints: 1500, preferredDifficulty: 30 },
      isGuest: false,
      allUsers: [],
      loading: false,
      error: null,
      registerUser: vi.fn(),
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
    });
    mockGetUserGames.mockResolvedValue([]);

    render(
      <MemoryRouter>
        <HomePage />
      </MemoryRouter>
    );

    expect(screen.getByText('Your Stats')).toBeInTheDocument();
    expect(screen.getByText('1,500')).toBeInTheDocument();
    expect(screen.getByText('Lifetime Points')).toBeInTheDocument();
  });

  it('navigates to /play when Play Now is clicked', async () => {
    mockUseUser.mockReturnValue({
      user: { uniqueId: 'USR-1-ABC', name: 'Guest', email: '', lifetimePoints: 0, preferredDifficulty: 10 },
      isGuest: true,
      allUsers: [],
      loading: false,
      error: null,
      registerUser: vi.fn(),
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
    });

    render(
      <MemoryRouter>
        <HomePage />
      </MemoryRouter>
    );

    const user = userEvent.setup();
    await user.click(screen.getByText('Play Now'));
    expect(mockNavigate).toHaveBeenCalledWith('/play');
  });
});
