import { useState, useEffect, useCallback } from 'react';
import { adminApi } from '../../services/adminApi';
import type { AdminPhrase, PhraseStats } from '../../types/admin';
import { ConfirmDialog } from '../../components/ConfirmDialog';
import './AdminPhrases.css';

export const AdminPhrases = () => {
  const [phrases, setPhrases] = useState<AdminPhrase[]>([]);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [activeFilter, setActiveFilter] = useState<string>('');
  const [searchTerm, setSearchTerm] = useState('');
  const [confirmAction, setConfirmAction] = useState<{ phraseId: string; isActive: boolean } | null>(null);

  // Form state
  const [showForm, setShowForm] = useState(false);
  const [editingPhrase, setEditingPhrase] = useState<AdminPhrase | null>(null);
  const [formText, setFormText] = useState('');
  const [formDifficulty, setFormDifficulty] = useState(0);
  const [formError, setFormError] = useState<string | null>(null);
  const [formLoading, setFormLoading] = useState(false);

  // Stats state
  const [expandedStats, setExpandedStats] = useState<string | null>(null);
  const [statsData, setStatsData] = useState<Record<string, PhraseStats>>({});
  const [statsLoading, setStatsLoading] = useState<string | null>(null);

  const fetchPhrases = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const isActive = activeFilter === '' ? undefined : activeFilter === 'true';
      const result = await adminApi.getPhrases(page, 20, isActive);
      setPhrases(result.data);
      setTotalPages(result.pagination.totalPages);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load phrases');
    } finally {
      setLoading(false);
    }
  }, [page, activeFilter]);

  useEffect(() => {
    fetchPhrases();
  }, [fetchPhrases]);

  const handleStatusToggle = async (uniqueId: string, isActive: boolean) => {
    try {
      await adminApi.setPhraseStatus(uniqueId, !isActive);
      await fetchPhrases();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to update status');
    }
  };

  const handleShowStats = async (uniqueId: string) => {
    if (expandedStats === uniqueId) {
      setExpandedStats(null);
      return;
    }
    setExpandedStats(uniqueId);
    if (!statsData[uniqueId]) {
      setStatsLoading(uniqueId);
      try {
        const stats = await adminApi.getPhraseStats(uniqueId);
        setStatsData(prev => ({ ...prev, [uniqueId]: stats }));
      } catch {
        // Stats not available
      } finally {
        setStatsLoading(null);
      }
    }
  };

  const openCreateForm = () => {
    setEditingPhrase(null);
    setFormText('');
    setFormDifficulty(0);
    setFormError(null);
    setShowForm(true);
  };

  const openEditForm = (phrase: AdminPhrase) => {
    setEditingPhrase(phrase);
    setFormText(phrase.text);
    setFormDifficulty(phrase.difficulty);
    setFormError(null);
    setShowForm(true);
  };

  const handleFormSubmit = async () => {
    if (!formText.trim()) {
      setFormError('Phrase text is required');
      return;
    }
    if (formText.trim().length < 3) {
      setFormError('Phrase must be at least 3 characters');
      return;
    }

    setFormLoading(true);
    setFormError(null);
    try {
      if (editingPhrase) {
        await adminApi.updatePhrase(editingPhrase.uniqueId, formText, formDifficulty);
      } else {
        await adminApi.createPhrase(formText, formDifficulty);
      }
      setShowForm(false);
      await fetchPhrases();
    } catch (e) {
      setFormError(e instanceof Error ? e.message : 'Failed to save phrase');
    } finally {
      setFormLoading(false);
    }
  };

  const filteredPhrases = searchTerm
    ? phrases.filter(p => p.text.toLowerCase().includes(searchTerm.toLowerCase()))
    : phrases;

  if (loading) return <div className="admin-loading">Loading phrases...</div>;
  if (error) return <div className="admin-error">{error}</div>;

  return (
    <div className="admin-page">
      <div className="admin-phrases-header">
        <h1>Phrases</h1>
        <button className="btn-create-phrase" onClick={openCreateForm}>+ New Phrase</button>
      </div>

      <div className="admin-phrases-filters">
        <input
          type="text"
          placeholder="Search phrases..."
          value={searchTerm}
          onChange={e => setSearchTerm(e.target.value)}
        />
        <select value={activeFilter} onChange={e => { setActiveFilter(e.target.value); setPage(1); }}>
          <option value="">All Phrases</option>
          <option value="true">Active Only</option>
          <option value="false">Inactive Only</option>
        </select>
      </div>

      <table className="admin-table">
        <thead>
          <tr>
            <th>Text</th>
            <th>Words</th>
            <th>Difficulty</th>
            <th>Status</th>
            <th>Source</th>
            <th>Created</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {filteredPhrases.map(phrase => (
            <>
              <tr key={phrase.uniqueId}>
                <td className="phrase-text-cell" title={phrase.text}>{phrase.text}</td>
                <td>{phrase.wordCount}</td>
                <td>{phrase.difficulty}</td>
                <td>
                  <span className={`status-badge ${phrase.isActive ? 'active' : 'inactive'}`}>
                    {phrase.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td>{phrase.generatedByLlm ? 'LLM' : 'Manual'}</td>
                <td>{new Date(phrase.createdAt).toLocaleDateString()}</td>
                <td>
                  <button className="btn-action" onClick={() => openEditForm(phrase)}>Edit</button>
                  <button className="btn-action" onClick={() => handleShowStats(phrase.uniqueId)}>
                    {expandedStats === phrase.uniqueId ? 'Hide Stats' : 'Stats'}
                  </button>
                  <button
                    className={`btn-action ${phrase.isActive ? 'deactivate' : 'activate'}`}
                    onClick={() => setConfirmAction({ phraseId: phrase.uniqueId, isActive: phrase.isActive })}
                  >
                    {phrase.isActive ? 'Deactivate' : 'Activate'}
                  </button>
                </td>
              </tr>
              {expandedStats === phrase.uniqueId && (
                <tr key={`${phrase.uniqueId}-stats`}>
                  <td colSpan={7} className="phrase-stats-row">
                    {statsLoading === phrase.uniqueId ? (
                      <span>Loading stats...</span>
                    ) : statsData[phrase.uniqueId] ? (
                      <div className="phrase-stats-grid">
                        <div className="phrase-stat-item">
                          <div className="stat-value">{statsData[phrase.uniqueId].timesPlayed}</div>
                          <div className="stat-label">Times Played</div>
                        </div>
                        <div className="phrase-stat-item">
                          <div className="stat-value">{(statsData[phrase.uniqueId].solveRate * 100).toFixed(1)}%</div>
                          <div className="stat-label">Solve Rate</div>
                        </div>
                        <div className="phrase-stat-item">
                          <div className="stat-value">{statsData[phrase.uniqueId].avgCluesToSolve?.toFixed(1) ?? 'N/A'}</div>
                          <div className="stat-label">Avg Clues to Solve</div>
                        </div>
                        <div className="phrase-stat-item">
                          <div className="stat-value">{statsData[phrase.uniqueId].calibratedDifficulty ?? 'N/A'}</div>
                          <div className="stat-label">Calibrated Difficulty</div>
                        </div>
                      </div>
                    ) : (
                      <span>No stats available for this phrase</span>
                    )}
                  </td>
                </tr>
              )}
            </>
          ))}
        </tbody>
      </table>

      {totalPages > 1 && (
        <div className="admin-pagination">
          <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}>Previous</button>
          <span>Page {page} of {totalPages}</span>
          <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages}>Next</button>
        </div>
      )}

      {showForm && (
        <div className="phrase-form-overlay" onClick={() => setShowForm(false)}>
          <div className="phrase-form-card" onClick={e => e.stopPropagation()}>
            <h3>{editingPhrase ? 'Edit Phrase' : 'New Phrase'}</h3>
            <div className="phrase-form-group">
              <label>Phrase Text</label>
              <textarea
                value={formText}
                onChange={e => setFormText(e.target.value)}
                placeholder="Enter the phrase text..."
              />
            </div>
            <div className="phrase-form-group">
              <label>Difficulty (0-100)</label>
              <input
                type="number"
                min={0}
                max={100}
                value={formDifficulty}
                onChange={e => setFormDifficulty(Number(e.target.value))}
              />
            </div>
            {formError && <div className="form-error">{formError}</div>}
            <div className="phrase-form-actions">
              <button className="btn-cancel" onClick={() => setShowForm(false)}>Cancel</button>
              <button className="btn-save" onClick={handleFormSubmit} disabled={formLoading}>
                {formLoading ? 'Saving...' : 'Save'}
              </button>
            </div>
          </div>
        </div>
      )}

      {confirmAction && (
        <ConfirmDialog
          title={confirmAction.isActive ? 'Deactivate Phrase' : 'Activate Phrase'}
          message={confirmAction.isActive
            ? 'Are you sure you want to deactivate this phrase? It will no longer appear in games.'
            : 'Are you sure you want to activate this phrase? It will start appearing in games.'}
          confirmLabel={confirmAction.isActive ? 'Deactivate' : 'Activate'}
          variant={confirmAction.isActive ? 'danger' : 'safe'}
          onConfirm={async () => {
            await handleStatusToggle(confirmAction.phraseId, confirmAction.isActive);
            setConfirmAction(null);
          }}
          onCancel={() => setConfirmAction(null)}
        />
      )}
    </div>
  );
};
