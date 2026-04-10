import { useEffect, useState } from 'react';
import { adminApi } from '../../services/adminApi';
import type { DashboardStats } from '../../types/admin';
import './AdminDashboard.css';

export function AdminDashboard() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    adminApi.getDashboard()
      .then(setStats)
      .catch(err => setError(err.message))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div className="admin-loading">Loading dashboard...</div>;
  if (error) return <div className="admin-error">{error}</div>;
  if (!stats) return null;

  return (
    <div className="admin-page">
      <h1>Dashboard</h1>
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
