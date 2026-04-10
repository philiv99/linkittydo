import { useState, useEffect, useCallback } from 'react';
import { adminApi } from '../../services/adminApi';
import type { AuditLogEntry } from '../../types/admin';
import './AdminAuditLog.css';

export function AdminAuditLog() {
  const [entries, setEntries] = useState<AuditLogEntry[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [actions, setActions] = useState<string[]>([]);
  const [filterAction, setFilterAction] = useState('');
  const [filterUserId, setFilterUserId] = useState('');
  const [filterFrom, setFilterFrom] = useState('');
  const [filterTo, setFilterTo] = useState('');

  const fetchEntries = useCallback(async (p: number) => {
    setLoading(true);
    try {
      const result = await adminApi.getAuditLog(
        p, 50,
        filterAction || undefined,
        filterUserId || undefined,
        filterFrom || undefined,
        filterTo || undefined
      );
      setEntries(result.data);
      setTotalPages(result.pagination.totalPages);
      setPage(result.pagination.page);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load audit log');
    } finally {
      setLoading(false);
    }
  }, [filterAction, filterUserId, filterFrom, filterTo]);

  useEffect(() => { fetchEntries(1); }, [fetchEntries]);

  useEffect(() => {
    adminApi.getAuditActions().then(setActions).catch(() => {});
  }, []);

  const handleFilter = () => fetchEntries(1);

  const handleClear = () => {
    setFilterAction('');
    setFilterUserId('');
    setFilterFrom('');
    setFilterTo('');
  };

  if (loading && entries.length === 0) return <div className="admin-loading">Loading audit log...</div>;

  return (
    <div className="admin-page">
      <div className="audit-header">
        <h1>Audit Log</h1>
        <div className="audit-filters">
          <select value={filterAction} onChange={e => setFilterAction(e.target.value)}>
            <option value="">All Actions</option>
            {actions.map(a => <option key={a} value={a}>{a}</option>)}
          </select>
          <input
            type="text"
            placeholder="User ID..."
            value={filterUserId}
            onChange={e => setFilterUserId(e.target.value)}
          />
          <input
            type="date"
            value={filterFrom}
            onChange={e => setFilterFrom(e.target.value)}
          />
          <input
            type="date"
            value={filterTo}
            onChange={e => setFilterTo(e.target.value)}
          />
          <button onClick={handleFilter}>Filter</button>
          {(filterAction || filterUserId || filterFrom || filterTo) && (
            <button onClick={handleClear}>Clear</button>
          )}
        </div>
      </div>
      {error && <div className="admin-error">{error}</div>}
      <table className="admin-table">
        <thead>
          <tr>
            <th>Timestamp</th>
            <th>Action</th>
            <th>User ID</th>
            <th>Entity</th>
            <th>Details</th>
            <th>IP</th>
          </tr>
        </thead>
        <tbody>
          {entries.map(entry => (
            <tr key={entry.id}>
              <td className="audit-timestamp">{new Date(entry.timestamp).toLocaleString()}</td>
              <td><span className="audit-action-badge">{entry.action}</span></td>
              <td style={{ fontSize: '0.8rem', fontFamily: 'monospace' }}>{entry.userId || '-'}</td>
              <td style={{ fontSize: '0.8rem' }}>
                {entry.entityType ? `${entry.entityType}${entry.entityId ? ` #${entry.entityId}` : ''}` : '-'}
              </td>
              <td className="audit-details" title={entry.details || ''}>{entry.details || '-'}</td>
              <td style={{ fontSize: '0.8rem', fontFamily: 'monospace' }}>{entry.ipAddress || '-'}</td>
            </tr>
          ))}
          {entries.length === 0 && (
            <tr><td colSpan={6} className="audit-empty">No audit log entries found</td></tr>
          )}
        </tbody>
      </table>
      {totalPages > 1 && (
        <div className="admin-pagination">
          <button disabled={page <= 1} onClick={() => fetchEntries(page - 1)}>Previous</button>
          <span>Page {page} of {totalPages}</span>
          <button disabled={page >= totalPages} onClick={() => fetchEntries(page + 1)}>Next</button>
        </div>
      )}
    </div>
  );
}
