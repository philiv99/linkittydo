import { useEffect, useState, useCallback } from 'react';
import { adminApi } from '../../services/adminApi';
import type { AdminUser, PlayerAnalytics, UserRoles } from '../../types/admin';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import './AdminUsers.css';

const ALL_ROLES = ['Admin', 'Moderator', 'Player'] as const;

export function AdminUsers() {
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedUser, setSelectedUser] = useState<string | null>(null);
  const [analytics, setAnalytics] = useState<PlayerAnalytics | null>(null);
  const [analyticsLoading, setAnalyticsLoading] = useState(false);
  const [userRoles, setUserRoles] = useState<Record<string, string[]>>({});
  const [roleToAssign, setRoleToAssign] = useState<Record<string, string>>({});
  const [confirmAction, setConfirmAction] = useState<{ userId: string; isActive: boolean; name: string } | null>(null);

  const fetchUsers = useCallback(async (p: number, search?: string) => {
    setLoading(true);
    try {
      const result = await adminApi.getUsers(p, 20, undefined, search);
      setUsers(result.data);
      setTotalPages(result.pagination.totalPages);
      setPage(result.pagination.page);
      // Fetch roles for all loaded users
      const rolesMap: Record<string, string[]> = {};
      await Promise.all(result.data.map(async (u: AdminUser) => {
        try {
          const r: UserRoles = await adminApi.getUserRoles(u.uniqueId);
          rolesMap[u.uniqueId] = r.roles;
        } catch {
          rolesMap[u.uniqueId] = [];
        }
      }));
      setUserRoles(rolesMap);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load users');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { fetchUsers(1); }, [fetchUsers]);

  const handleSearch = () => {
    setSearchTerm(searchInput);
    fetchUsers(1, searchInput || undefined);
  };

  const handleClearSearch = () => {
    setSearchInput('');
    setSearchTerm('');
    fetchUsers(1);
  };

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

  const handleAssignRole = async (uniqueId: string) => {
    const role = roleToAssign[uniqueId];
    if (!role) return;
    try {
      await adminApi.assignRole(uniqueId, role);
      setUserRoles(prev => ({
        ...prev,
        [uniqueId]: [...(prev[uniqueId] || []), role]
      }));
      setRoleToAssign(prev => ({ ...prev, [uniqueId]: '' }));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to assign role');
    }
  };

  const handleRemoveRole = async (uniqueId: string, role: string) => {
    try {
      await adminApi.removeRole(uniqueId, role);
      setUserRoles(prev => ({
        ...prev,
        [uniqueId]: (prev[uniqueId] || []).filter(r => r !== role)
      }));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to remove role');
    }
  };

  if (loading) return <div className="admin-loading">Loading users...</div>;

  return (
    <div className="admin-page">
      <div className="users-header">
        <h1>User Management</h1>
        <div className="users-search">
          <input
            type="text"
            placeholder="Search by name or email..."
            value={searchInput}
            onChange={e => setSearchInput(e.target.value)}
            onKeyDown={e => e.key === 'Enter' && handleSearch()}
          />
          <button onClick={handleSearch}>Search</button>
          {searchTerm && <button onClick={handleClearSearch}>Clear</button>}
        </div>
      </div>
      {error && <div className="admin-error">{error}</div>}
      <table className="admin-table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Email</th>
            <th>Roles</th>
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
                  {user.isSimulated && <span className="status-badge simulated sim-badge">SIM</span>}
                </td>
                <td>{user.email}</td>
                <td>
                  <div className="role-badges">
                    {(userRoles[user.uniqueId] || []).map(role => (
                      <span key={role} className={`role-badge ${role.toLowerCase()}`}>
                        {role}
                        <button className="remove-role" onClick={() => handleRemoveRole(user.uniqueId, role)} title={`Remove ${role} role`}>&times;</button>
                      </span>
                    ))}
                  </div>
                  <div className="role-assign">
                    <select
                      value={roleToAssign[user.uniqueId] || ''}
                      onChange={e => setRoleToAssign(prev => ({ ...prev, [user.uniqueId]: e.target.value }))}
                    >
                      <option value="">Add role...</option>
                      {ALL_ROLES.filter(r => !(userRoles[user.uniqueId] || []).includes(r)).map(r => (
                        <option key={r} value={r}>{r}</option>
                      ))}
                    </select>
                    <button onClick={() => handleAssignRole(user.uniqueId)} disabled={!roleToAssign[user.uniqueId]}>Add</button>
                  </div>
                </td>
                <td>{user.lifetimePoints}</td>
                <td>
                  <span className={`status-badge ${user.isActive ? 'active' : 'inactive'}`}>
                    {user.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td>{new Date(user.createdAt).toLocaleDateString()}</td>
                <td>
                  <button
                    className="admin-action-btn"
                    onClick={() => setConfirmAction({ userId: user.uniqueId, isActive: user.isActive, name: user.name })}
                  >
                    {user.isActive ? 'Deactivate' : 'Activate'}
                  </button>
                  <button
                    className="admin-action-btn"
                    onClick={() => handleViewAnalytics(user.uniqueId)}
                  >
                    {selectedUser === user.uniqueId ? 'Hide Stats' : 'Stats'}
                  </button>
                </td>
              </tr>
              {selectedUser === user.uniqueId && (
                <tr key={`${user.uniqueId}-analytics`}>
                  <td colSpan={7} className="analytics-row">
                    {analyticsLoading ? (
                      <span>Loading analytics...</span>
                    ) : analytics ? (
                      <div className="analytics-grid">
                        <div><span className="analytics-label">Games Played</span><br /><strong>{analytics.gamesPlayed}</strong></div>
                        <div><span className="analytics-label">Solved</span><br /><strong>{analytics.gamesSolved}</strong></div>
                        <div><span className="analytics-label">Avg Score</span><br /><strong>{analytics.avgScore.toFixed(0)}</strong></div>
                        <div><span className="analytics-label">Best Score</span><br /><strong>{analytics.bestScore}</strong></div>
                        <div><span className="analytics-label">Current Streak</span><br /><strong>{analytics.currentStreak}</strong></div>
                        <div><span className="analytics-label">Best Streak</span><br /><strong>{analytics.bestStreak}</strong></div>
                        <div><span className="analytics-label">Avg Clues/Game</span><br /><strong>{analytics.avgCluesPerGame.toFixed(1)}</strong></div>
                        <div><span className="analytics-label">Avg Guesses/Game</span><br /><strong>{analytics.avgGuessesPerGame.toFixed(1)}</strong></div>
                      </div>
                    ) : (
                      <span className="analytics-empty">No analytics available for this user</span>
                    )}
                  </td>
                </tr>
              )}
            </>
          ))}
        </tbody>
      </table>
      <div className="admin-pagination">
        <button disabled={page <= 1} onClick={() => fetchUsers(page - 1, searchTerm || undefined)}>Previous</button>
        <span>Page {page} of {totalPages}</span>
        <button disabled={page >= totalPages} onClick={() => fetchUsers(page + 1, searchTerm || undefined)}>Next</button>
      </div>

      {confirmAction && (
        <ConfirmDialog
          title={confirmAction.isActive ? 'Deactivate User' : 'Activate User'}
          message={confirmAction.isActive
            ? `Are you sure you want to deactivate "${confirmAction.name}"? They will no longer be able to log in.`
            : `Are you sure you want to activate "${confirmAction.name}"?`}
          confirmLabel={confirmAction.isActive ? 'Deactivate' : 'Activate'}
          variant={confirmAction.isActive ? 'danger' : 'safe'}
          onConfirm={async () => {
            await handleToggleStatus(confirmAction.userId, confirmAction.isActive);
            setConfirmAction(null);
          }}
          onCancel={() => setConfirmAction(null)}
        />
      )}
    </div>
  );
}
