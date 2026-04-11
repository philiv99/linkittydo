import { useUser } from '../hooks/useUser';
import { api } from '../services/api';
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import type { GameRecord, GameEvent, ClueEvent, GuessEvent, GameEndEvent } from '../types';
import './GameHistoryPage.css';

const formatDate = (dateStr: string): string => {
  return new Date(dateStr).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
};

const isClueEvent = (e: GameEvent): e is ClueEvent => e.eventType === 'clue';
const isGuessEvent = (e: GameEvent): e is GuessEvent => e.eventType === 'guess';
const isGameEndEvent = (e: GameEvent): e is GameEndEvent => e.eventType === 'gameend';

export const GameHistoryPage: React.FC = () => {
  const { user, isGuest } = useUser();
  const navigate = useNavigate();
  const [games, setGames] = useState<GameRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [expandedGameId, setExpandedGameId] = useState<string | null>(null);
  const [loadingDetail, setLoadingDetail] = useState<string | null>(null);
  const [detailCache, setDetailCache] = useState<Record<string, GameEvent[]>>({});

  useEffect(() => {
    if (isGuest) {
      setLoading(false);
      return;
    }
    api.getUserGames(user.uniqueId)
      .then(setGames)
      .catch(() => setGames([]))
      .finally(() => setLoading(false));
  }, [isGuest, user.uniqueId]);

  if (isGuest) {
    return (
      <div className="history-page">
        <div className="history-guest-message">
          <h2>Game History</h2>
          <p>Create a profile to track your game history and see your progress over time.</p>
          <button className="history-play-button" onClick={() => navigate('/play')}>
            Go Play
          </button>
        </div>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="history-page">
        <h2>Game History</h2>
        <div className="history-loading">Loading your games...</div>
      </div>
    );
  }

  if (games.length === 0) {
    return (
      <div className="history-page">
        <h2>Game History</h2>
        <div className="history-empty">
          <p>No games played yet. Time to get started!</p>
          <button className="history-play-button" onClick={() => navigate('/play')}>
            Play Now
          </button>
        </div>
      </div>
    );
  }

  const toggleExpand = async (gameId: string) => {
    if (expandedGameId === gameId) {
      setExpandedGameId(null);
      return;
    }
    setExpandedGameId(gameId);

    // Fetch events from the detail endpoint if not cached
    if (!detailCache[gameId]) {
      setLoadingDetail(gameId);
      try {
        const detail = await api.getGameDetail(gameId);
        if (detail) {
          setDetailCache(prev => ({ ...prev, [gameId]: detail.events }));
        }
      } catch {
        // Silent fail — expand shows empty state
      } finally {
        setLoadingDetail(null);
      }
    }
  };

  const renderEvent = (event: GameEvent, idx: number) => {
    if (isClueEvent(event)) {
      return (
        <li key={idx} className="event-item event-clue">
          <span className="event-icon">?</span>
          <span>Clue for word #{event.wordIndex + 1}: <em>{event.searchTerm}</em></span>
        </li>
      );
    }
    if (isGuessEvent(event)) {
      return (
        <li key={idx} className={`event-item event-guess ${event.isCorrect ? 'correct' : 'wrong'}`}>
          <span className="event-icon">{event.isCorrect ? '+' : 'x'}</span>
          <span>
            Guessed &quot;{event.guessText}&quot; for word #{event.wordIndex + 1}
            {event.isCorrect && ` (+${event.pointsAwarded} pts)`}
          </span>
        </li>
      );
    }
    if (isGameEndEvent(event)) {
      return (
        <li key={idx} className={`event-item event-end ${event.reason}`}>
          <span className="event-icon">!</span>
          <span>{event.reason === 'solved' ? 'Phrase solved!' : 'Gave up'}</span>
        </li>
      );
    }
    return null;
  };

  return (
    <div className="history-page">
      <h2>Game History</h2>
      <p className="history-subtitle">{games.length} game{games.length !== 1 ? 's' : ''} played</p>

      <div className="history-list">
        {games.map(game => (
          <div key={game.gameId} className="history-card">
            <div
              className="history-card-header"
              onClick={() => toggleExpand(game.gameId)}
            >
              <div className="history-card-main">
                <span className="history-phrase">{game.phraseText}</span>
                <span className="history-date">{formatDate(game.playedAt)}</span>
              </div>
              <div className="history-card-meta">
                <span className={`history-result ${game.result.toLowerCase()}`}>
                  {game.result === 'Solved' ? 'Solved' : game.result === 'GaveUp' ? 'Gave Up' : game.result === 'Abandoned' ? 'Abandoned' : 'In Progress'}
                </span>
                <span className="history-score">{game.score} pts</span>
                <span className="history-difficulty">Diff: {game.difficulty}</span>
                <span className="history-expand">{expandedGameId === game.gameId ? '\u25B2' : '\u25BC'}</span>
              </div>
            </div>

            {expandedGameId === game.gameId && (
              <div className="history-card-detail">
                {loadingDetail === game.gameId ? (
                  <p className="detail-loading">Loading event timeline...</p>
                ) : (detailCache[game.gameId]?.length ?? 0) > 0 ? (
                  <>
                    <h4>Event Timeline</h4>
                    <ul className="event-timeline">
                      {detailCache[game.gameId].map((event, idx) => renderEvent(event, idx))}
                    </ul>
                  </>
                ) : (
                  <p className="detail-empty">No events recorded for this game.</p>
                )}
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
};
