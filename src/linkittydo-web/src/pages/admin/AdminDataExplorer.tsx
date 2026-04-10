import { useEffect, useState } from 'react';
import { adminApi } from '../../services/adminApi';
import type { DataSummary, SimulationSummary, PlayerDetail } from '../../types/admin';

export function AdminDataExplorer() {
  const [dataSummary, setDataSummary] = useState<DataSummary | null>(null);
  const [simSummary, setSimSummary] = useState<SimulationSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [playerIdInput, setPlayerIdInput] = useState('');
  const [playerDetail, setPlayerDetail] = useState<PlayerDetail | null>(null);
  const [playerLoading, setPlayerLoading] = useState(false);

  useEffect(() => {
    Promise.all([
      adminApi.getDataSummary(),
      adminApi.getSimulationSummary(),
    ])
      .then(([data, sim]) => {
        setDataSummary(data);
        setSimSummary(sim);
      })
      .catch(err => setError(err.message))
      .finally(() => setLoading(false));
  }, []);

  const handleLookupPlayer = async () => {
    if (!playerIdInput.trim()) return;
    setPlayerLoading(true);
    setPlayerDetail(null);
    try {
      const detail = await adminApi.getPlayerDetail(playerIdInput.trim());
      setPlayerDetail(detail);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Player not found');
    } finally {
      setPlayerLoading(false);
    }
  };

  if (loading) return <div className="admin-loading">Loading data summary...</div>;

  return (
    <div className="admin-page">
      <h1>Data Explorer</h1>
      {error && <div className="admin-error">{error}</div>}

      {dataSummary && (
        <>
          <h2 style={{ fontSize: '1.1rem', color: '#a0aec0', marginBottom: '0.75rem' }}>Data Summary</h2>
          <div className="admin-cards">
            <div className="admin-card">
              <div className="card-label">Total Users</div>
              <div className="card-value">{dataSummary.totalUsers}</div>
            </div>
            <div className="admin-card">
              <div className="card-label">Total Phrases</div>
              <div className="card-value">{dataSummary.totalPhrases}</div>
            </div>
            <div className="admin-card">
              <div className="card-label">Total Games</div>
              <div className="card-value">{dataSummary.totalGames}</div>
            </div>
            <div className="admin-card">
              <div className="card-label">Total Events</div>
              <div className="card-value">{dataSummary.totalEvents}</div>
            </div>
            <div className="admin-card">
              <div className="card-label">Simulated Users</div>
              <div className="card-value">{dataSummary.simulatedUsers}</div>
            </div>
            <div className="admin-card">
              <div className="card-label">Simulated Games</div>
              <div className="card-value">{dataSummary.simulatedGames}</div>
            </div>
          </div>
        </>
      )}

      {simSummary && (
        <>
          <h2 style={{ fontSize: '1.1rem', color: '#a0aec0', marginBottom: '0.75rem', marginTop: '1.5rem' }}>Simulation Summary</h2>
          <div className="admin-cards">
            <div className="admin-card">
              <div className="card-label">Simulated Users</div>
              <div className="card-value">{simSummary.totalSimulatedUsers}</div>
            </div>
            <div className="admin-card">
              <div className="card-label">Simulated Games</div>
              <div className="card-value">{simSummary.totalSimulatedGames}</div>
            </div>
            <div className="admin-card">
              <div className="card-label">Solve Rate</div>
              <div className="card-value">{(simSummary.simulatedSolveRate * 100).toFixed(1)}%</div>
            </div>
            <div className="admin-card">
              <div className="card-label">Avg Score</div>
              <div className="card-value">{simSummary.avgSimulatedScore.toFixed(0)}</div>
            </div>
          </div>
        </>
      )}

      <h2 style={{ fontSize: '1.1rem', color: '#a0aec0', marginBottom: '0.75rem', marginTop: '1.5rem' }}>Player Lookup</h2>
      <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '1rem' }}>
        <input
          type="text"
          value={playerIdInput}
          onChange={e => setPlayerIdInput(e.target.value)}
          placeholder="Enter user ID (e.g., USR-...)"
          style={{ padding: '0.5rem', background: '#0f3460', color: '#e0e0e0', border: '1px solid #2a3a5c', borderRadius: '6px', width: '320px' }}
          onKeyDown={e => e.key === 'Enter' && handleLookupPlayer()}
        />
        <button
          onClick={handleLookupPlayer}
          disabled={playerLoading}
          style={{ padding: '0.5rem 1rem', background: '#e94560', color: 'white', border: 'none', borderRadius: '6px', cursor: 'pointer' }}
        >
          {playerLoading ? 'Loading...' : 'Lookup'}
        </button>
      </div>

      {playerDetail && (
        <div style={{ background: '#16213e', borderRadius: '10px', padding: '1.25rem', border: '1px solid #2a3a5c' }}>
          <h3 style={{ margin: '0 0 0.75rem', fontSize: '1rem' }}>{playerDetail.user.name}</h3>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '0.5rem', marginBottom: '1rem', fontSize: '0.85rem' }}>
            <div><span style={{ color: '#a0aec0' }}>Email:</span> {playerDetail.user.email}</div>
            <div><span style={{ color: '#a0aec0' }}>Points:</span> {playerDetail.user.lifetimePoints}</div>
            <div><span style={{ color: '#a0aec0' }}>Status:</span> <span className={`status-badge ${playerDetail.user.isActive ? 'active' : 'inactive'}`}>{playerDetail.user.isActive ? 'Active' : 'Inactive'}</span></div>
          </div>
          {playerDetail.stats && (
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '0.5rem', marginBottom: '1rem', fontSize: '0.85rem' }}>
              <div><span style={{ color: '#a0aec0' }}>Games:</span> {playerDetail.stats.gamesPlayed}</div>
              <div><span style={{ color: '#a0aec0' }}>Solved:</span> {playerDetail.stats.gamesSolved}</div>
              <div><span style={{ color: '#a0aec0' }}>Avg Score:</span> {playerDetail.stats.avgScore.toFixed(0)}</div>
              <div><span style={{ color: '#a0aec0' }}>Best:</span> {playerDetail.stats.bestScore}</div>
            </div>
          )}
          {playerDetail.recentGames.length > 0 && (
            <>
              <h4 style={{ fontSize: '0.9rem', color: '#a0aec0', margin: '0.75rem 0 0.5rem' }}>Recent Games</h4>
              <table className="admin-table">
                <thead>
                  <tr>
                    <th>Phrase</th>
                    <th>Score</th>
                    <th>Result</th>
                    <th>Played</th>
                  </tr>
                </thead>
                <tbody>
                  {playerDetail.recentGames.map(g => (
                    <tr key={g.gameId}>
                      <td style={{ maxWidth: '250px', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{g.phraseText}</td>
                      <td>{g.score}</td>
                      <td><span className={`status-badge ${g.result.toLowerCase()}`}>{g.result}</span></td>
                      <td>{new Date(g.playedAt).toLocaleDateString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </>
          )}
        </div>
      )}
    </div>
  );
}
