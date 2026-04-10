import { useEffect, useState } from 'react';
import { adminApi } from '../../services/adminApi';
import type { SiteConfigEntry } from '../../types/admin';

export function AdminConfig() {
  const [configs, setConfigs] = useState<SiteConfigEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [editingKey, setEditingKey] = useState<string | null>(null);
  const [editValue, setEditValue] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    adminApi.getConfigs()
      .then(setConfigs)
      .catch(err => setError(err.message))
      .finally(() => setLoading(false));
  }, []);

  const handleEdit = (config: SiteConfigEntry) => {
    setEditingKey(config.key);
    setEditValue(config.value);
  };

  const handleSave = async () => {
    if (!editingKey) return;
    setSaving(true);
    try {
      await adminApi.setConfig(editingKey, editValue);
      setConfigs(prev => prev.map(c => c.key === editingKey ? { ...c, value: editValue, updatedAt: new Date().toISOString() } : c));
      setEditingKey(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save');
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <div className="admin-loading">Loading configuration...</div>;

  return (
    <div className="admin-page">
      <h1>Site Configuration</h1>
      {error && <div className="admin-error">{error}</div>}
      <table className="admin-table">
        <thead>
          <tr>
            <th>Key</th>
            <th>Value</th>
            <th>Type</th>
            <th>Updated</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {configs.map(config => (
            <tr key={config.key}>
              <td style={{ fontFamily: 'monospace', fontSize: '0.85rem' }}>{config.key}</td>
              <td>
                {editingKey === config.key ? (
                  config.valueType === 'bool' ? (
                    <select
                      value={editValue}
                      onChange={e => setEditValue(e.target.value)}
                      style={{ padding: '0.3rem', background: '#0f3460', color: '#e0e0e0', border: '1px solid #2a3a5c', borderRadius: '4px' }}
                    >
                      <option value="true">true</option>
                      <option value="false">false</option>
                    </select>
                  ) : config.valueType === 'json' ? (
                    <textarea
                      value={editValue}
                      onChange={e => setEditValue(e.target.value)}
                      rows={3}
                      style={{ width: '100%', padding: '0.3rem', background: '#0f3460', color: '#e0e0e0', border: '1px solid #2a3a5c', borderRadius: '4px', fontFamily: 'monospace', fontSize: '0.8rem' }}
                    />
                  ) : (
                    <input
                      type={config.valueType === 'int' ? 'number' : 'text'}
                      value={editValue}
                      onChange={e => setEditValue(e.target.value)}
                      style={{ padding: '0.3rem', background: '#0f3460', color: '#e0e0e0', border: '1px solid #2a3a5c', borderRadius: '4px', width: '200px' }}
                    />
                  )
                ) : (
                  <span style={{ fontFamily: 'monospace', fontSize: '0.85rem' }}>{config.value}</span>
                )}
              </td>
              <td style={{ color: '#a0aec0', fontSize: '0.8rem' }}>{config.valueType}</td>
              <td style={{ fontSize: '0.8rem' }}>{new Date(config.updatedAt).toLocaleDateString()}</td>
              <td>
                {editingKey === config.key ? (
                  <div style={{ display: 'flex', gap: '0.3rem' }}>
                    <button
                      onClick={handleSave}
                      disabled={saving}
                      style={{ padding: '0.3rem 0.6rem', borderRadius: '4px', border: 'none', background: '#48bb78', color: 'white', cursor: 'pointer', fontSize: '0.8rem' }}
                    >
                      Save
                    </button>
                    <button
                      onClick={() => setEditingKey(null)}
                      style={{ padding: '0.3rem 0.6rem', borderRadius: '4px', border: '1px solid #2a3a5c', background: 'transparent', color: '#a0aec0', cursor: 'pointer', fontSize: '0.8rem' }}
                    >
                      Cancel
                    </button>
                  </div>
                ) : (
                  <button
                    onClick={() => handleEdit(config)}
                    style={{ padding: '0.3rem 0.6rem', borderRadius: '4px', border: '1px solid #2a3a5c', background: '#0f3460', color: '#e0e0e0', cursor: 'pointer', fontSize: '0.8rem' }}
                  >
                    Edit
                  </button>
                )}
              </td>
            </tr>
          ))}
          {configs.length === 0 && (
            <tr><td colSpan={5} style={{ textAlign: 'center', color: '#a0aec0', padding: '2rem' }}>No configuration entries</td></tr>
          )}
        </tbody>
      </table>
    </div>
  );
}
