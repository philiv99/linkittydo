import { useNavigate } from 'react-router-dom';
import { useUser } from '../hooks/useUser';
import { api } from '../services/api';
import { useEffect, useState } from 'react';
import type { GameRecord } from '../types';
import './HomePage.css';

const assetUrl = (path: string) => `${import.meta.env.BASE_URL}${path.replace(/^\//, '')}`;

export const HomePage: React.FC = () => {
  const { user, isGuest } = useUser();
  const navigate = useNavigate();
  const [recentGames, setRecentGames] = useState<GameRecord[]>([]);
  const [gamesPlayed, setGamesPlayed] = useState(0);

  useEffect(() => {
    if (!isGuest && user.uniqueId) {
      api.getUserGames(user.uniqueId).then(games => {
        setRecentGames(games.slice(0, 3));
        setGamesPlayed(games.length);
      }).catch(() => {
        // Silently fail - stats are optional
      });
    }
  }, [isGuest, user.uniqueId]);

  return (
    <div className="home-page">
      <section className="hero-section">
        <img src={assetUrl('lounge.jpg')} alt="LinkittyDo!" className="hero-image" />
        <div className="hero-overlay">
          <h1 className="hero-title">LinkittyDo!</h1>
          <p className="hero-subtitle">
            Guess the hidden words using clever clues from across the web
          </p>
          <button className="hero-play-button" onClick={() => navigate('/play')}>
            Play Now
          </button>
        </div>
      </section>

      {!isGuest && (
        <section className="stats-section">
          <h2>Your Stats</h2>
          <div className="stats-grid">
            <div className="stat-card">
              <span className="stat-value">{user.lifetimePoints.toLocaleString()}</span>
              <span className="stat-label">Lifetime Points</span>
            </div>
            <div className="stat-card">
              <span className="stat-value">{gamesPlayed}</span>
              <span className="stat-label">Games Played</span>
            </div>
            <div className="stat-card">
              <span className="stat-value">{user.preferredDifficulty}</span>
              <span className="stat-label">Difficulty</span>
            </div>
          </div>

          {recentGames.length > 0 && (
            <div className="recent-games-preview">
              <h3>Recent Games</h3>
              <ul className="recent-list">
                {recentGames.map(game => (
                  <li key={game.gameId} className="recent-item">
                    <span className="recent-phrase">{game.phraseText}</span>
                    <span className={`recent-result ${game.result.toLowerCase()}`}>
                      {game.result === 'Solved' ? 'Solved' : game.result === 'GaveUp' ? 'Gave Up' : 'In Progress'}
                    </span>
                    <span className="recent-score">{game.score} pts</span>
                  </li>
                ))}
              </ul>
              <button className="view-history-button" onClick={() => navigate('/history')}>
                View All History
              </button>
            </div>
          )}
        </section>
      )}

      <section className="how-to-play-section">
        <h2>How to Play</h2>
        <div className="steps-grid">
          <div className="step-card">
            <span className="step-number">1</span>
            <h3>See the Phrase</h3>
            <p>A phrase appears with some words hidden. Your job is to figure them out.</p>
          </div>
          <div className="step-card">
            <span className="step-number">2</span>
            <h3>Get Clues</h3>
            <p>Click a hidden word for a web clue — a link that hints at the answer.</p>
          </div>
          <div className="step-card">
            <span className="step-number">3</span>
            <h3>Guess & Score</h3>
            <p>Type your guess. Fewer clues and guesses mean higher scores!</p>
          </div>
        </div>
      </section>
    </div>
  );
};
