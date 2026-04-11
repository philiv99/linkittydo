import { useEffect, useState } from 'react';
import { adminApi } from '../../services/adminApi';
import type { AdminGame, GameDetail, GameEventSummary } from '../../types/admin';
import './AdminGames.css';

export function AdminGames() {
  const [games, setGames] = useState<AdminGame[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [resultFilter, setResultFilter] = useState('');
  const [detail, setDetail] = useState<GameDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);

  const fetchGames = async (p: number, result?: string) => {
    setLoading(true);
    try {
      const filters = result ? { result } : undefined;
      const res = await adminApi.getGames(p, 20, filters);
      setGames(res.data);
      setTotalPages(res.pagination.totalPages);
      setPage(res.pagination.page);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load games');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchGames(1, resultFilter || undefined); }, [resultFilter]);

  const handleViewDetail = async (gameId: string) => {
    if (detail?.gameId === gameId) {
      setDetail(null);
      return;
    }
    setDetailLoading(true);
    try {
      const d = await adminApi.getGameDetail(gameId);
      setDetail(d);
    } catch {
      setDetail(null);
    } finally {
      setDetailLoading(false);
    }
  };

  const resultBadge = (result: string) => {
    const cls = result === 'Solved' ? 'solved' : result === 'GaveUp' ? 'gaveup' : 'inprogress';
    return <span className={`status-badge ${cls}`}>{result}</span>;
  };

  const formatDateTime = (dateStr: string) => {
    const d = new Date(dateStr);
    return d.toLocaleDateString() + ' ' + d.toLocaleTimeString();
  };

  const renderEventRow = (evt: GameEventSummary) => {
    if (evt.eventType === 'clue') {
      return (
        <tr key={evt.id}>
          <td>{evt.sequenceNumber}</td>
          <td><span className="event-badge event-clue">clue</span></td>
          <td className="event-detail-cell">
            <strong>{evt.phraseWord ?? `word[${evt.wordIndex}]`}</strong>
            {' → '}
            <span className="event-search-term">{evt.searchTerm}</span>
            {evt.relationshipType && (
              <span className={`relationship-badge rel-${evt.relationshipType}`}>
                {evt.relationshipType}
              </span>
            )}
            {evt.url && (
              <>
                {' — '}
                <a href={evt.url} target="_blank" rel="noopener noreferrer" className="event-clue-link">
                  {evt.url.length > 60 ? evt.url.substring(0, 60) + '...' : evt.url}
                </a>
              </>
            )}
          </td>
          <td>{formatDateTime(evt.timestamp)}</td>
        </tr>
      );
    }

    if (evt.eventType === 'guess') {
      return (
        <tr key={evt.id}>
          <td>{evt.sequenceNumber}</td>
          <td><span className="event-badge event-guess">guess</span></td>
          <td className="event-detail-cell">
            <strong>{evt.phraseWord ?? `word[${evt.wordIndex}]`}</strong>
            {' → guessed '}
            <span className="event-guess-text">"{evt.guessText}"</span>
            {' '}
            {evt.isCorrect
              ? <span className="status-badge solved">Correct +{evt.pointsAwarded}pts</span>
              : <span className="status-badge gaveup">Incorrect</span>
            }
          </td>
          <td>{formatDateTime(evt.timestamp)}</td>
        </tr>
      );
    }

    if (evt.eventType === 'gameend') {
      return (
        <tr key={evt.id}>
          <td>{evt.sequenceNumber}</td>
          <td><span className="event-badge event-end">end</span></td>
          <td className="event-detail-cell">
            Game ended: <strong>{evt.reason === 'solved' ? 'Solved!' : 'Gave Up'}</strong>
          </td>
          <td>{formatDateTime(evt.timestamp)}</td>
        </tr>
      );
    }

    return (
      <tr key={evt.id}>
        <td>{evt.sequenceNumber}</td>
        <td>{evt.eventType}</td>
        <td></td>
        <td>{formatDateTime(evt.timestamp)}</td>
      </tr>
    );
  };

  if (loading && games.length === 0) return <div className="admin-loading">Loading games...</div>;

  return (
    <div className="admin-page">
      <h1>Games Manager</h1>
      {error && <div className="admin-error">{error}</div>}
      <div className="games-filter-bar">
        <label className="games-filter-label">Filter by result:</label>
        <select
          value={resultFilter}
          onChange={e => setResultFilter(e.target.value)}
        >
          <option value="">All</option>
          <option value="Solved">Solved</option>
          <option value="GaveUp">Gave Up</option>
          <option value="InProgress">In Progress</option>
        </select>
      </div>
      <table className="admin-table">
        <thead>
          <tr>
            <th>Player</th>
            <th>Phrase</th>
            <th>Score</th>
            <th>Result</th>
            <th>Played</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {games.map(game => (
            <>
              <tr key={game.gameId}>
                <td className="game-player-cell">
                  {game.playerName}
                  {game.isSimulated && <span className="status-badge simulated game-sim-badge">SIM</span>}
                </td>
                <td className="game-phrase-cell">
                  {game.phraseText}
                </td>
                <td>{game.score}</td>
                <td>{resultBadge(game.result)}</td>
                <td>{formatDateTime(game.playedAt)}</td>
                <td>
                  <button
                    onClick={() => handleViewDetail(game.gameId)}
                    className="game-detail-btn"
                  >
                    {detail?.gameId === game.gameId ? 'Hide' : 'Detail'}
                  </button>
                </td>
              </tr>
              {detail?.gameId === game.gameId && (
                <tr key={`${game.gameId}-detail`}>
                  <td colSpan={6} className="game-detail-row">
                    {detailLoading ? (
                      <span>Loading detail...</span>
                    ) : (
                      <div>
                        <div className="game-detail-meta">
                          <strong>Phrase:</strong> {detail.phraseText}<br />
                          <strong>Difficulty:</strong> {detail.difficulty} | <strong>Score:</strong> {detail.score} | <strong>Events:</strong> {detail.eventCount}
                        </div>
                        {detail.events.length > 0 && (
                          <table className="game-events-table">
                            <thead>
                              <tr>
                                <th>#</th>
                                <th>Type</th>
                                <th>Detail</th>
                                <th>Time</th>
                              </tr>
                            </thead>
                            <tbody>
                              {detail.events.map(evt => renderEventRow(evt))}
                            </tbody>
                          </table>
                        )}
                      </div>
                    )}
                  </td>
                </tr>
              )}
            </>
          ))}
          {games.length === 0 && (
            <tr><td colSpan={6} className="games-empty">No games found</td></tr>
          )}
        </tbody>
      </table>
      <div className="admin-pagination">
        <button disabled={page <= 1} onClick={() => fetchGames(page - 1, resultFilter || undefined)}>Previous</button>
        <span>Page {page} of {totalPages}</span>
        <button disabled={page >= totalPages} onClick={() => fetchGames(page + 1, resultFilter || undefined)}>Next</button>
      </div>
    </div>
  );
}
