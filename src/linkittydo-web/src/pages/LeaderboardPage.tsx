import { useState, useEffect } from 'react';
import { api } from '../services/api';
import type { LeaderboardEntry } from '../types';
import './LeaderboardPage.css';

export const LeaderboardPage: React.FC = () => {
  const [entries, setEntries] = useState<LeaderboardEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchLeaderboard = async () => {
      try {
        setLoading(true);
        const data = await api.getLeaderboard(20);
        setEntries(data);
      } catch {
        setError('Failed to load leaderboard');
      } finally {
        setLoading(false);
      }
    };
    fetchLeaderboard();
  }, []);

  if (loading) {
    return (
      <div className="leaderboard-page">
        <h1>Leaderboard</h1>
        <p className="leaderboard-loading">Loading rankings...</p>
      </div>
    );
  }

  if (error) {
    return (
      <div className="leaderboard-page">
        <h1>Leaderboard</h1>
        <p className="leaderboard-error">{error}</p>
      </div>
    );
  }

  return (
    <div className="leaderboard-page" role="main" aria-label="Leaderboard rankings">
      <h1>Leaderboard</h1>
      {entries.length === 0 ? (
        <p className="leaderboard-empty">No players yet. Be the first to play!</p>
      ) : (
        <table className="leaderboard-table" aria-label="Player rankings">
          <thead>
            <tr>
              <th>Rank</th>
              <th>Player</th>
              <th>Points</th>
              <th>Games</th>
              <th>Solved</th>
              <th>Best Score</th>
              <th>Streak</th>
            </tr>
          </thead>
          <tbody>
            {entries.map((entry) => (
              <tr key={entry.rank} className={entry.rank <= 3 ? `rank-${entry.rank}` : ''}>
                <td className="rank-cell">
                  {entry.rank <= 3 ? (
                    <span className="rank-medal">
                      {entry.rank === 1 ? '🥇' : entry.rank === 2 ? '🥈' : '🥉'}
                    </span>
                  ) : (
                    entry.rank
                  )}
                </td>
                <td className="name-cell">{entry.name}</td>
                <td className="points-cell">{entry.lifetimePoints.toLocaleString()}</td>
                <td className="games-cell">{entry.gamesPlayed}</td>
                <td className="games-cell">{entry.gamesSolved}</td>
                <td className="points-cell">{entry.bestScore.toLocaleString()}</td>
                <td className="games-cell">{entry.currentStreak}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
};
