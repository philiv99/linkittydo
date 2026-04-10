import { useEffect, useState } from 'react';
import { adminApi } from '../../services/adminApi';
import type { DataSummary, SimulationSummary, PlayerDetail } from '../../types/admin';
import './AdminDataExplorer.css';

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
          <h2 className="data-section-title">Data Summary</h2>
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
          <h2 className="data-section-title spaced">Simulation Summary</h2>
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

      <h2 className="data-section-title spaced">Player Lookup</h2>
      <div className="player-lookup-bar">
        <input
          type="text"
          value={playerIdInput}
          onChange={e => setPlayerIdInput(e.target.value)}
          placeholder="Enter user ID (e.g., USR-...)"
          className="player-lookup-input"
          onKeyDown={e => e.key === 'Enter' && handleLookupPlayer()}
        />
        <button
          onClick={handleLookupPlayer}
          disabled={playerLoading}
          className="player-lookup-btn"
        >
          {playerLoading ? 'Loading...' : 'Lookup'}
        </button>
      </div>

      {playerDetail && (
        <div className="player-detail-card">
          <h3 className="player-detail-name">{playerDetail.user.name}</h3>
          <div className="player-info-grid">
            <div><span className="player-label">Email:</span> {playerDetail.user.email}</div>
            <div><span className="player-label">Points:</span> {playerDetail.user.lifetimePoints}</div>
            <div><span className="player-label">Status:</span> <span className={`status-badge ${playerDetail.user.isActive ? 'active' : 'inactive'}`}>{playerDetail.user.isActive ? 'Active' : 'Inactive'}</span></div>
          </div>
          {playerDetail.stats && (
            <div className="player-stats-grid">
              <div><span className="player-label">Games:</span> {playerDetail.stats.gamesPlayed}</div>
              <div><span className="player-label">Solved:</span> {playerDetail.stats.gamesSolved}</div>
              <div><span className="player-label">Avg Score:</span> {playerDetail.stats.avgScore.toFixed(0)}</div>
              <div><span className="player-label">Best:</span> {playerDetail.stats.bestScore}</div>
            </div>
          )}
          {playerDetail.recentGames.length > 0 && (
            <>
              <h4 className="recent-games-title">Recent Games</h4>
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
                      <td className="player-phrase-cell">{g.phraseText}</td>
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
