import React, { useState, useEffect, useCallback } from 'react';
import { api } from '../services/api';
import { useAuth } from '../contexts/AuthContext';
import { useUser } from '../hooks/useUser';
import type { DailyChallengeResponse, DailyChallengeLeaderboardEntry } from '../types';
import './DailyChallengePage.css';

export const DailyChallengePage: React.FC = () => {
  const { authUser, isAuthenticated } = useAuth();
  const { user } = useUser();
  const [challengeStatus, setChallengeStatus] = useState<DailyChallengeResponse | null>(null);
  const [leaderboard, setLeaderboard] = useState<DailyChallengeLeaderboardEntry[]>([]);
  const [statusLoading, setStatusLoading] = useState(true);
  const [starting, setStarting] = useState(false);
  const [startError, setStartError] = useState<string | null>(null);

  const loadStatus = useCallback(async () => {
    try {
      setStatusLoading(true);
      const userId = isAuthenticated ? authUser?.uniqueId : undefined;
      const status = await api.getDailyChallenge(userId);
      setChallengeStatus(status);
      const lb = await api.getDailyChallengeLeaderboard(undefined, 10);
      setLeaderboard(lb);
    } catch (err) {
      console.error('Failed to load daily challenge status:', err);
    } finally {
      setStatusLoading(false);
    }
  }, [isAuthenticated, authUser]);

  useEffect(() => {
    loadStatus();
  }, [loadStatus]);

  const handleStartDaily = async () => {
    try {
      setStarting(true);
      setStartError(null);
      const state = await api.startDailyChallenge({
        userId: isAuthenticated ? authUser?.uniqueId : undefined,
        difficulty: user.preferredDifficulty,
      });
      // Navigate to play page with daily challenge session
      window.location.href = `/play?session=${state.sessionId}`;
    } catch (err) {
      setStartError(err instanceof Error ? err.message : 'Failed to start daily challenge');
    } finally {
      setStarting(false);
    }
  };

  const todayFormatted = new Date().toLocaleDateString(undefined, {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });

  if (statusLoading) {
    return (
      <div className="daily-page">
        <div className="daily-loading">Loading today's challenge...</div>
      </div>
    );
  }

  return (
    <div className="daily-page">
      <div className="daily-container">
        <div className="daily-header">
          <h1>Daily Challenge</h1>
          <p className="daily-date">{todayFormatted}</p>
        </div>

        {challengeStatus?.alreadyPlayed && challengeStatus.previousResult ? (
          <div className="daily-completed">
            <div className="daily-completed-badge">Challenge Complete!</div>
            <div className="daily-completed-stats">
              <div className="daily-stat">
                <span className="daily-stat-value">{challengeStatus.previousResult.score}</span>
                <span className="daily-stat-label">Score</span>
              </div>
              <div className="daily-stat">
                <span className="daily-stat-value">{challengeStatus.previousResult.result}</span>
                <span className="daily-stat-label">Result</span>
              </div>
            </div>
            <p className="daily-completed-msg">Come back tomorrow for a new challenge!</p>
          </div>
        ) : (
          <div className="daily-start-section">
            <p className="daily-description">
              Everyone gets the same phrase today. How will you score?
            </p>
            {!isAuthenticated && (
              <p className="daily-login-hint">
                Log in to track your daily challenge results and compete on the leaderboard.
              </p>
            )}
            <button
              className="daily-start-btn"
              onClick={handleStartDaily}
              disabled={starting}
            >
              {starting ? 'Starting...' : 'Play Today\'s Challenge'}
            </button>
            {startError && <p className="daily-error">{startError}</p>}
          </div>
        )}

        {leaderboard.length > 0 && (
          <div className="daily-leaderboard">
            <h2>Today's Leaderboard</h2>
            <div className="daily-lb-list">
              {leaderboard.map((entry) => (
                <div key={entry.rank} className="daily-lb-row">
                  <span className="daily-lb-rank">#{entry.rank}</span>
                  <span className="daily-lb-name">{entry.playerName}</span>
                  <span className="daily-lb-score">{entry.score} pts</span>
                  <span className={`daily-lb-result ${entry.result === 'Solved' ? 'solved' : 'gaveup'}`}>
                    {entry.result}
                  </span>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
