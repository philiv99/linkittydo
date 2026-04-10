import { useEffect, useState } from 'react';
import { adminApi } from '../../services/adminApi';
import type { AdminUser, PlayerAnalytics } from '../../types/admin';

export function AdminUsers() {
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [selectedUser, setSelectedUser] = useState<string | null>(null);
  const [analytics, setAnalytics] = useState<PlayerAnalytics | null>(null);
  const [analyticsLoading, setAnalyticsLoading] = useState(false);

  const fetchUsers = async (p: number) => {
    setLoading(true);
    try {
      const result = await adminApi.getUsers(p);
      setUsers(result.data);
      setTotalPages(result.pagination.totalPages);
      setPage(result.pagination.page);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load users');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchUsers(1); }, []);

  const handleToggleStatus = async (uniqueId: string, currentActive: boolean) => {
    try {
      await adminApi.setUserStatus(uniqueId, !currentActive);
      setUsers(prev => prev.map(u => u.uniqueId === uniqueId ? { ...u, isActive: !currentActive } : u));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update status');
    }
  };

  const handleViewAnalytics = async (uniqueId: string) => {
    if (selectedUser === uniqueId) {
      setSelectedUser(null);
      setAnalytics(null);
      return;
    }
    setSelectedUser(uniqueId);
    setAnalyticsLoading(true);
    try {
      const stats = await adminApi.getPlayerAnalytics(uniqueId);
      setAnalytics(stats);
    } catch {
      setAnalytics(null);
    } finally {
      setAnalyticsLoading(false);
    }
  };

  if (loading) return <div className="admin-loading">Loading users...</div>;

  return (
    <div className="admin-page">
      <h1>User Management</h1>
      {error && <div className="admin-error">{error}</div>}
      <table className="admin-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Email</th>
            <th>Points</th>
            <th>Status</th>
            <th>Created</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {users.map(user => (
            <>
              <tr key={user.uniqueId}>
                <td>
                  {user.name}
                  {user.isSimulated && <span className="status-badge simulated" style={{ marginLeft: '0.5rem' }}>SIM</span>}
                </td>
                <td>{user.email}</td>
                <td>{user.lifetimePoints}</td>
                <td>
                  <span className={`status-badge ${user.isActive ? 'active' : 'inactive'}`}>
                    {user.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td>{new Date(user.createdAt).toLocaleDateString()}</td>
                <td>
                  <button
                    onClick={() => handleToggleStatus(user.uniqueId, user.isActive)}
                    style={{ marginRight: '0.5rem', padding: '0.3rem 0.6rem', borderRadius: '4px', border: '1px solid #2a3a5c', background: '#0f3460', color: '#e0e0e0', cursor: 'pointer', fontSize: '0.8rem' }}
                  >
                    {user.isActive ? 'Deactivate' : 'Activate'}
                  </button>
                  <button
                    onClick={() => handleViewAnalytics(user.uniqueId)}
                    style={{ padding: '0.3rem 0.6rem', borderRadius: '4px', border: '1px solid #2a3a5c', background: '#0f3460', color: '#e0e0e0', cursor: 'pointer', fontSize: '0.8rem' }}
                  >
                    {selectedUser === user.uniqueId ? 'Hide Stats' : 'Stats'}
                  </button>
                </td>
              </tr>
              {selectedUser === user.uniqueId && (
                <tr key={`${user.uniqueId}-analytics`}>
                  <td colSpan={6} style={{ background: '#0f3460', padding: '1rem' }}>
                    {analyticsLoading ? (
                      <span>Loading analytics...</span>
                    ) : analytics ? (
                      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '0.75rem' }}>
                        <div><span style={{ color: '#a0aec0', fontSize: '0.8rem' }}>Games Played</span><br /><strong>{analytics.gamesPlayed}</strong></div>
                        <div><span style={{ color: '#a0aec0', fontSize: '0.8rem' }}>Solved</span><br /><strong>{analytics.gamesSolved}</strong></div>
                        <div><span style={{ color: '#a0aec0', fontSize: '0.8rem' }}>Avg Score</span><br /><strong>{analytics.avgScore.toFixed(0)}</strong></div>
                        <div><span style={{ color: '#a0aec0', fontSize: '0.8rem' }}>Best Score</span><br /><strong>{analytics.bestScore}</strong></div>
                        <div><span style={{ color: '#a0aec0', fontSize: '0.8rem' }}>Current Streak</span><br /><strong>{analytics.currentStreak}</strong></div>
                        <div><span style={{ color: '#a0aec0', fontSize: '0.8rem' }}>Best Streak</span><br /><strong>{analytics.bestStreak}</strong></div>
                        <div><span style={{ color: '#a0aec0', fontSize: '0.8rem' }}>Avg Clues/Game</span><br /><strong>{analytics.avgCluesPerGame.toFixed(1)}</strong></div>
                        <div><span style={{ color: '#a0aec0', fontSize: '0.8rem' }}>Avg Guesses/Game</span><br /><strong>{analytics.avgGuessesPerGame.toFixed(1)}</strong></div>
                      </div>
                    ) : (
                      <span style={{ color: '#a0aec0' }}>No analytics available for this user</span>
                    )}
                  </td>
                </tr>
              )}
            </>
          ))}
        </tbody>
      </table>
      <div className="admin-pagination">
        <button disabled={page <= 1} onClick={() => fetchUsers(page - 1)}>Previous</button>
        <span>Page {page} of {totalPages}</span>
        <button disabled={page >= totalPages} onClick={() => fetchUsers(page + 1)}>Next</button>
      </div>
    </div>
  );
}
