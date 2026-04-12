import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { api } from '../services/api';
import { useAuth } from '../contexts/AuthContext';
import { useUser } from '../hooks/useUser';
import type { ProfileResponse, GameRecord } from '../types';
import './ProfilePage.css';

const formatDate = (dateStr: string): string => {
  return new Date(dateStr).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
};

const formatDateTime = (dateStr: string): string => {
  return new Date(dateStr).toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
};

export const ProfilePage: React.FC = () => {
  const { authUser, isAuthenticated } = useAuth();
  const { isGuest, refreshUser } = useUser();
  const navigate = useNavigate();
  const [profile, setProfile] = useState<ProfileResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [authError, setAuthError] = useState(false);

  useEffect(() => {
    if (isGuest) {
      navigate('/play');
      return;
    }

    const loadProfile = async () => {
      try {
        setLoading(true);
        setAuthError(false);
        if (!isAuthenticated || !authUser) {
          setAuthError(true);
          setLoading(false);
          return;
        }
        const data = await api.getUserProfile(authUser.uniqueId);
        setProfile(data);
      } catch (err) {
        if (err instanceof Error && err.message.includes('401')) {
          setAuthError(true);
        } else {
          setError('Failed to load profile');
        }
        console.error(err);
      } finally {
        setLoading(false);
      }
    };

    refreshUser();
    loadProfile();
  }, [authUser, isAuthenticated, isGuest, navigate, refreshUser]);

  if (loading) {
    return (
      <div className="profile-page">
        <div className="profile-loading">Loading profile...</div>
      </div>
    );
  }

  if (authError) {
    return (
      <div className="profile-page">
        <div className="profile-auth-error">
          <h2>Session Expired</h2>
          <p>Your session has expired. Please log in again to view your profile.</p>
          <button className="profile-login-button" onClick={() => navigate('/play')}>
            Go to Play
          </button>
        </div>
      </div>
    );
  }

  if (error || !profile) {
    return (
      <div className="profile-page">
        <div className="profile-error">{error || 'Profile not available'}</div>
      </div>
    );
  }

  const resultLabel = (result: string): string => {
    switch (result) {
      case 'Solved': return 'Solved';
      case 'GaveUp': return 'Gave Up';
      case 'Abandoned': return 'Abandoned';
      default: return 'In Progress';
    }
  };

  const resultClass = (result: string): string => {
    switch (result) {
      case 'Solved': return 'result-solved';
      case 'GaveUp': return 'result-gaveup';
      default: return 'result-other';
    }
  };

  return (
    <div className="profile-page">
      <div className="profile-container">
        <div className="profile-header">
          <div className="profile-avatar">
            {profile.name.charAt(0).toUpperCase()}
          </div>
          <div className="profile-info">
            <h1 className="profile-name">{profile.name}</h1>
            <p className="profile-email">{profile.email}</p>
            <p className="profile-joined">Joined {formatDate(profile.createdAt)}</p>
          </div>
          <div className="profile-points">
            <span className="profile-points-value">{profile.lifetimePoints.toLocaleString()}</span>
            <span className="profile-points-label">Lifetime Points</span>
          </div>
        </div>

        <div className="profile-stats-grid">
          <StatCard label="Games Played" value={profile.gamesPlayed} />
          <StatCard label="Games Solved" value={profile.gamesSolved} />
          <StatCard label="Solve Rate" value={`${profile.solveRate}%`} />
          <StatCard label="Avg Score" value={Math.round(profile.avgScore)} />
          <StatCard label="Best Score" value={profile.bestScore} />
          <StatCard label="Current Streak" value={profile.currentStreak} highlight={profile.currentStreak > 0} />
          <StatCard label="Best Streak" value={profile.bestStreak} />
          <StatCard label="Difficulty" value={profile.preferredDifficulty} />
        </div>

        {profile.lastPlayedAt && (
          <p className="profile-last-played">
            Last played: {formatDateTime(profile.lastPlayedAt)}
          </p>
        )}

        <div className="profile-recent-games">
          <h2>Recent Games</h2>
          {profile.recentGames.length === 0 ? (
            <p className="profile-no-games">No games played yet. Start your first game!</p>
          ) : (
            <div className="profile-games-list">
              {profile.recentGames.map((game: GameRecord) => (
                <div key={game.gameId} className="profile-game-card">
                  <div className="profile-game-phrase">{game.phraseText}</div>
                  <div className="profile-game-details">
                    <span className={`profile-game-result ${resultClass(game.result)}`}>
                      {resultLabel(game.result)}
                    </span>
                    <span className="profile-game-score">{game.score} pts</span>
                    <span className="profile-game-difficulty">Diff: {game.difficulty}</span>
                    <span className="profile-game-date">{formatDate(game.playedAt)}</span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

interface StatCardProps {
  label: string;
  value: string | number;
  highlight?: boolean;
}

const StatCard: React.FC<StatCardProps> = ({ label, value, highlight }) => (
  <div className={`profile-stat-card ${highlight ? 'highlight' : ''}`}>
    <div className="profile-stat-value">{value}</div>
    <div className="profile-stat-label">{label}</div>
  </div>
);
