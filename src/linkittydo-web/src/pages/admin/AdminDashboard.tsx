import { useEffect, useState, useCallback, useRef } from 'react';
import { adminApi } from '../../services/adminApi';
import type { DashboardStats } from '../../types/admin';
import './AdminDashboard.css';

export function AdminDashboard() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [autoRefresh, setAutoRefresh] = useState(false);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const fetchDashboard = useCallback(async () => {
    try {
      const data = await adminApi.getDashboard();
      setStats(data);
      setError('');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchDashboard();
  }, [fetchDashboard]);

  useEffect(() => {
    if (autoRefresh) {
      intervalRef.current = setInterval(fetchDashboard, 30000);
    } else if (intervalRef.current) {
      clearInterval(intervalRef.current);
      intervalRef.current = null;
    }
    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current);
    };
  }, [autoRefresh, fetchDashboard]);

  if (loading) return <div className="admin-loading">Loading dashboard...</div>;
  if (error) return <div className="admin-error">{error}</div>;
  if (!stats) return null;

  return (
    <div className="admin-page">
      <div className="dashboard-header">
        <h1>Dashboard</h1>
        <div className="dashboard-controls">
          <button className="btn btn-secondary" onClick={fetchDashboard}>Refresh</button>
          <label className="auto-refresh-toggle">
            <input type="checkbox" checked={autoRefresh} onChange={e => setAutoRefresh(e.target.checked)} />
            Auto-refresh (30s)
          </label>
        </div>
      </div>
      <div className="admin-cards">
        <div className="admin-card">
          <div className="card-label">Total Users</div>
          <div className="card-value">{stats.totalUsers}</div>
        </div>
        <div className="admin-card">
          <div className="card-label">Active Phrases</div>
          <div className="card-value">{stats.activePhrases}</div>
        </div>
        <div className="admin-card">
          <div className="card-label">Games Played</div>
          <div className="card-value">{stats.totalGamesPlayed}</div>
        </div>
        <div className="admin-card">
          <div className="card-label">Solve Rate</div>
          <div className="card-value">{(stats.overallSolveRate * 100).toFixed(1)}%</div>
        </div>
        <div className="admin-card">
          <div className="card-label">Avg Score</div>
          <div className="card-value">{stats.avgScore.toFixed(0)}</div>
        </div>
        <div className="admin-card">
          <div className="card-label">Games Today</div>
          <div className="card-value">{stats.gamesPlayedToday}</div>
        </div>
        <div className="admin-card">
          <div className="card-label">New Users Today</div>
          <div className="card-value">{stats.newUsersToday}</div>
        </div>
        <div className="admin-card">
          <div className="card-label">Simulated Users</div>
          <div className="card-value">{stats.simulatedUsers}</div>
        </div>
      </div>
      <p className="dashboard-computed">
        Last computed: {new Date(stats.computedAt).toLocaleString()}
      </p>
    </div>
  );
}
