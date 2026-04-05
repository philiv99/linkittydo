import { describe, it, expect, vi, beforeEach } from 'vitest';
import { api } from '../services/api';

// Mock global fetch
const mockFetch = vi.fn();
global.fetch = mockFetch;

beforeEach(() => {
  mockFetch.mockReset();
});

const mockApiResponse = <T>(data: T, message = 'Operation successful') => ({
  data,
  message,
});

describe('api.startGame', () => {
  it('starts a game and returns unwrapped data', async () => {
    const gameState = { sessionId: 'abc-123', words: [], score: 0, isComplete: false };
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve(mockApiResponse(gameState)),
    });

    const result = await api.startGame();

    expect(result).toEqual(gameState);
    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/game/start'),
      expect.objectContaining({ method: 'POST' })
    );
  });

  it('throws on non-ok response', async () => {
    mockFetch.mockResolvedValueOnce({ ok: false, status: 500 });

    await expect(api.startGame()).rejects.toThrow('Failed to start game');
  });
});

describe('api.getGame', () => {
  it('returns unwrapped game state', async () => {
    const gameState = { sessionId: 'abc-123', words: [], score: 100, isComplete: false };
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve(mockApiResponse(gameState)),
    });

    const result = await api.getGame('abc-123');

    expect(result).toEqual(gameState);
  });
});

describe('api.submitGuess', () => {
  it('sends guess and returns unwrapped response', async () => {
    const guessResponse = { isCorrect: true, isPhraseComplete: false, currentScore: 100, revealedWord: 'quick' };
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve(mockApiResponse(guessResponse)),
    });

    const result = await api.submitGuess('abc-123', { wordIndex: 1, guess: 'quick' });

    expect(result.isCorrect).toBe(true);
    expect(result.currentScore).toBe(100);
    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/game/abc-123/guess'),
      expect.objectContaining({ method: 'POST' })
    );
  });
});

describe('api.getClue', () => {
  it('returns unwrapped clue', async () => {
    const clue = { url: 'https://example.com', searchTerm: 'fast' };
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve(mockApiResponse(clue)),
    });

    const result = await api.getClue('abc-123', 1);

    expect(result.url).toBe('https://example.com');
    expect(result.searchTerm).toBe('fast');
  });

  it('appends excluded URLs as query params', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve(mockApiResponse({ url: '', searchTerm: '' })),
    });

    await api.getClue('abc-123', 1, ['https://excluded.com']);

    const calledUrl = mockFetch.mock.calls[0][0] as string;
    expect(calledUrl).toContain('excludeUrl=');
  });
});

describe('api.giveUp', () => {
  it('returns unwrapped state', async () => {
    const state = { sessionId: 'abc-123', words: [], score: 0, isComplete: true };
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve(mockApiResponse(state)),
    });

    const result = await api.giveUp('abc-123');

    expect(result.isComplete).toBe(true);
  });
});

describe('api.getGameRecord', () => {
  it('returns null on 404', async () => {
    mockFetch.mockResolvedValueOnce({ ok: false, status: 404 });

    const result = await api.getGameRecord('abc-123');

    expect(result).toBeNull();
  });

  it('returns unwrapped record when found', async () => {
    const record = { gameId: 'GAME-1', phraseText: 'test', score: 100 };
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve(mockApiResponse(record)),
    });

    const result = await api.getGameRecord('abc-123');

    expect(result!.gameId).toBe('GAME-1');
  });
});

describe('api.createUser', () => {
  it('creates user and returns unwrapped response', async () => {
    const userResponse = { uniqueId: 'USR-123', name: 'Test', email: 'test@t.com', lifetimePoints: 0, preferredDifficulty: 10, gamesPlayed: 0, createdAt: '2026-01-01' };
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve(mockApiResponse(userResponse)),
    });

    const result = await api.createUser({ name: 'Test', email: 'test@t.com' });

    expect(result.uniqueId).toBe('USR-123');
  });

  it('throws with error message on conflict', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 409,
      json: () => Promise.resolve({ error: { code: 'NAME_TAKEN', message: 'Name taken' } }),
    });

    await expect(api.createUser({ name: 'Taken', email: 'e@t.com' })).rejects.toThrow('Name taken');
  });
});

describe('api.getUser', () => {
  it('returns null on 404', async () => {
    mockFetch.mockResolvedValueOnce({ ok: false, status: 404 });

    const result = await api.getUser('nonexistent');

    expect(result).toBeNull();
  });

  it('returns unwrapped user', async () => {
    const userResponse = { uniqueId: 'USR-123', name: 'Test', email: 'test@t.com', lifetimePoints: 0, preferredDifficulty: 10, gamesPlayed: 0, createdAt: '2026-01-01' };
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve(mockApiResponse(userResponse)),
    });

    const result = await api.getUser('USR-123');

    expect(result!.name).toBe('Test');
  });
});

describe('api.getAllUsers', () => {
  it('returns unwrapped user array', async () => {
    const users = [
      { uniqueId: 'USR-1', name: 'A', email: 'a@t.com', lifetimePoints: 0, preferredDifficulty: 10, gamesPlayed: 0, createdAt: '2026-01-01' },
    ];
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve(mockApiResponse(users)),
    });

    const result = await api.getAllUsers();

    expect(result).toHaveLength(1);
    expect(result[0].name).toBe('A');
  });
});

describe('api.deleteUser', () => {
  it('does not throw on 204', async () => {
    mockFetch.mockResolvedValueOnce({ ok: true, status: 204 });

    await expect(api.deleteUser('USR-123')).resolves.toBeUndefined();
  });
});

describe('api.updateDifficulty', () => {
  it('returns unwrapped difficulty response', async () => {
    const diffResponse = { uniqueId: 'USR-123', preferredDifficulty: 50 };
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve(mockApiResponse(diffResponse)),
    });

    const result = await api.updateDifficulty('USR-123', 50);

    expect(result.preferredDifficulty).toBe(50);
  });
});

describe('api.addPoints', () => {
  it('returns unwrapped points response', async () => {
    const pointsResponse = { uniqueId: 'USR-123', lifetimePoints: 200, pointsAdded: 100 };
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve(mockApiResponse(pointsResponse)),
    });

    const result = await api.addPoints('USR-123', 100);

    expect(result.lifetimePoints).toBe(200);
    expect(result.pointsAdded).toBe(100);
  });
});

describe('api.checkNameAvailability', () => {
  it('returns true when available', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve(mockApiResponse({ available: true })),
    });

    const result = await api.checkNameAvailability('AvailName');

    expect(result).toBe(true);
  });

  it('returns false when taken', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve(mockApiResponse({ available: false })),
    });

    const result = await api.checkNameAvailability('TakenName');

    expect(result).toBe(false);
  });
});

describe('api.checkEmailAvailability', () => {
  it('returns availability status', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve(mockApiResponse({ available: true })),
    });

    const result = await api.checkEmailAvailability('avail@t.com');

    expect(result).toBe(true);
  });
});

describe('api.getUserGames', () => {
  it('returns unwrapped game records', async () => {
    const games = [{ gameId: 'GAME-1', phraseText: 'test', score: 200 }];
    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve(mockApiResponse(games)),
    });

    const result = await api.getUserGames('USR-123');

    expect(result).toHaveLength(1);
    expect(result[0].score).toBe(200);
  });
});
