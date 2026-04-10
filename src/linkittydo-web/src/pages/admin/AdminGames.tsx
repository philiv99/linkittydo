import { useEffect, useState } from 'react';
import { adminApi } from '../../services/adminApi';
import type { AdminGame, GameDetail } from '../../types/admin';

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

  if (loading && games.length === 0) return <div className="admin-loading">Loading games...</div>;

  return (
    <div className="admin-page">
      <h1>Games Manager</h1>
      {error && <div className="admin-error">{error}</div>}
      <div style={{ marginBottom: '1rem', display: 'flex', gap: '0.5rem', alignItems: 'center' }}>
        <label style={{ color: '#a0aec0', fontSize: '0.85rem' }}>Filter by result:</label>
        <select
          value={resultFilter}
          onChange={e => setResultFilter(e.target.value)}
          style={{ padding: '0.4rem', borderRadius: '4px', background: '#0f3460', color: '#e0e0e0', border: '1px solid #2a3a5c' }}
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
            <th>Game ID</th>
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
                <td style={{ fontFamily: 'monospace', fontSize: '0.8rem' }}>
                  {game.gameId.substring(0, 20)}...
                  {game.isSimulated && <span className="status-badge simulated" style={{ marginLeft: '0.5rem' }}>SIM</span>}
                </td>
                <td style={{ maxWidth: '250px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                  {game.phraseText}
                </td>
                <td>{game.score}</td>
                <td>{resultBadge(game.result)}</td>
                <td>{new Date(game.playedAt).toLocaleDateString()}</td>
                <td>
                  <button
                    onClick={() => handleViewDetail(game.gameId)}
                    style={{ padding: '0.3rem 0.6rem', borderRadius: '4px', border: '1px solid #2a3a5c', background: '#0f3460', color: '#e0e0e0', cursor: 'pointer', fontSize: '0.8rem' }}
                  >
                    {detail?.gameId === game.gameId ? 'Hide' : 'Detail'}
                  </button>
                </td>
              </tr>
              {detail?.gameId === game.gameId && (
                <tr key={`${game.gameId}-detail`}>
                  <td colSpan={6} style={{ background: '#0f3460', padding: '1rem' }}>
                    {detailLoading ? (
                      <span>Loading detail...</span>
                    ) : (
                      <div>
                        <div style={{ marginBottom: '0.75rem' }}>
                          <strong>Phrase:</strong> {detail.phraseText}<br />
                          <strong>Difficulty:</strong> {detail.difficulty} | <strong>Score:</strong> {detail.score} | <strong>Events:</strong> {detail.eventCount}
                        </div>
                        {detail.events.length > 0 && (
                          <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                            <thead>
                              <tr>
                                <th style={{ padding: '0.4rem', fontSize: '0.75rem', textAlign: 'left', color: '#a0aec0' }}>#</th>
                                <th style={{ padding: '0.4rem', fontSize: '0.75rem', textAlign: 'left', color: '#a0aec0' }}>Type</th>
                                <th style={{ padding: '0.4rem', fontSize: '0.75rem', textAlign: 'left', color: '#a0aec0' }}>Time</th>
                              </tr>
                            </thead>
                            <tbody>
                              {detail.events.map(evt => (
                                <tr key={evt.id}>
                                  <td style={{ padding: '0.3rem 0.4rem', fontSize: '0.8rem' }}>{evt.sequenceNumber}</td>
                                  <td style={{ padding: '0.3rem 0.4rem', fontSize: '0.8rem' }}>{evt.eventType}</td>
                                  <td style={{ padding: '0.3rem 0.4rem', fontSize: '0.8rem' }}>{new Date(evt.timestamp).toLocaleTimeString()}</td>
                                </tr>
                              ))}
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
            <tr><td colSpan={6} style={{ textAlign: 'center', color: '#a0aec0', padding: '2rem' }}>No games found</td></tr>
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
