import { useEffect, useState } from 'react';
import { adminApi } from '../../services/adminApi';
import type { SiteConfigEntry } from '../../types/admin';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import './AdminConfig.css';

export function AdminConfig() {
  const [configs, setConfigs] = useState<SiteConfigEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [editingKey, setEditingKey] = useState<string | null>(null);
  const [editValue, setEditValue] = useState('');
  const [saving, setSaving] = useState(false);
  const [showSaveConfirm, setShowSaveConfirm] = useState(false);

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
              <td className="config-key">{config.key}</td>
              <td>
                {editingKey === config.key ? (
                  config.valueType === 'bool' ? (
                    <select
                      value={editValue}
                      onChange={e => setEditValue(e.target.value)}
                      className="config-select"
                    >
                      <option value="true">true</option>
                      <option value="false">false</option>
                    </select>
                  ) : config.valueType === 'json' ? (
                    <textarea
                      value={editValue}
                      onChange={e => setEditValue(e.target.value)}
                      rows={3}
                      className="config-textarea"
                    />
                  ) : (
                    <input
                      type={config.valueType === 'int' ? 'number' : 'text'}
                      value={editValue}
                      onChange={e => setEditValue(e.target.value)}
                      className="config-input"
                    />
                  )
                ) : (
                  <span className="config-value-display">{config.value}</span>
                )}
              </td>
              <td className="config-type">{config.valueType}</td>
              <td className="config-date">{new Date(config.updatedAt).toLocaleDateString()}</td>
              <td>
                {editingKey === config.key ? (
                  <div className="config-actions">
                    <button
                      onClick={() => setShowSaveConfirm(true)}
                      disabled={saving}
                      className="config-save-btn"
                    >
                      Save
                    </button>
                    <button
                      onClick={() => setEditingKey(null)}
                      className="config-cancel-btn"
                    >
                      Cancel
                    </button>
                  </div>
                ) : (
                  <button
                    onClick={() => handleEdit(config)}
                    className="config-edit-btn"
                  >
                    Edit
                  </button>
                )}
              </td>
            </tr>
          ))}
          {configs.length === 0 && (
            <tr><td colSpan={5} className="config-empty">No configuration entries</td></tr>
          )}
        </tbody>
      </table>

      {showSaveConfirm && (
        <ConfirmDialog
          title="Save Configuration"
          message={`Are you sure you want to update "${editingKey}" to "${editValue}"?`}
          confirmLabel="Save"
          variant="safe"
          onConfirm={async () => {
            setShowSaveConfirm(false);
            await handleSave();
          }}
          onCancel={() => setShowSaveConfirm(false)}
        />
      )}
    </div>
  );
}
