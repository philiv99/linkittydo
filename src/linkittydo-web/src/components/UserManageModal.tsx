import React, { useState, useEffect } from 'react';
import type { User } from '../types';
import './UserManageModal.css';

interface UserManageModalProps {
  isOpen: boolean;
  onClose: () => void;
  currentUser: User;
  allUsers: User[];
  isGuest: boolean;
  onSignOut: () => void;
  onSwitchUser: (uniqueId: string) => Promise<boolean>;
  onUpdateDifficulty: (difficulty: number) => Promise<boolean>;
  onCreateProfile: () => void;
  loading: boolean;
}

export const UserManageModal: React.FC<UserManageModalProps> = ({
  isOpen,
  onClose,
  currentUser,
  allUsers,
  isGuest,
  onSignOut,
  onSwitchUser,
  onUpdateDifficulty,
  onCreateProfile,
  loading,
}) => {
  const [difficulty, setDifficulty] = useState(currentUser.preferredDifficulty);
  const [selectedUserId, setSelectedUserId] = useState(currentUser.uniqueId);
  const [isSwitching, setIsSwitching] = useState(false);

  // Reset state when modal opens
  useEffect(() => {
    if (isOpen) {
      setDifficulty(currentUser.preferredDifficulty);
      setSelectedUserId(currentUser.uniqueId);
    }
  }, [isOpen, currentUser.preferredDifficulty, currentUser.uniqueId]);

  const handleDifficultyChange = async (newDifficulty: number) => {
    setDifficulty(newDifficulty);
    await onUpdateDifficulty(newDifficulty);
  };

  const handleUserSwitch = async () => {
    if (selectedUserId !== currentUser.uniqueId) {
      setIsSwitching(true);
      const success = await onSwitchUser(selectedUserId);
      setIsSwitching(false);
      if (success) {
        onClose();
      }
    }
  };

  const handleSignOut = () => {
    onSignOut();
    onClose();
  };

  if (!isOpen) return null;

  // Filter out the current user from the dropdown options
  const otherUsers = allUsers.filter(u => u.uniqueId !== currentUser.uniqueId);

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content manage-modal" onClick={(e) => e.stopPropagation()}>
        <button className="modal-close" onClick={onClose} aria-label="Close">
          ×
        </button>

        <h2>Account Settings</h2>

        <div className="user-info-section">
          <div className="user-avatar">
            {currentUser.name.charAt(0).toUpperCase()}
          </div>
          <div className="user-details">
            <span className="user-display-name">{currentUser.name}</span>
            {!isGuest && <span className="user-email">{currentUser.email}</span>}
            {isGuest && <span className="guest-label">Guest Account</span>}
          </div>
        </div>

        {isGuest ? (
          <div className="guest-section">
            <p className="guest-message">Create a profile to save your progress and compete on the leaderboard!</p>
            <button 
              className="btn-primary full-width"
              onClick={() => {
                onClose();
                onCreateProfile();
              }}
            >
              Create Profile
            </button>
          </div>
        ) : (
          <>
            {/* Difficulty Setting */}
            <div className="setting-section">
              <label className="setting-label">Preferred Difficulty</label>
              <div className="difficulty-slider-container">
                <input
                  type="range"
                  min="0"
                  max="100"
                  value={difficulty}
                  onChange={(e) => handleDifficultyChange(parseInt(e.target.value))}
                  className="difficulty-slider"
                  disabled={loading}
                />
                <div className="difficulty-labels">
                  <span>Easy</span>
                  <span className="difficulty-value">{difficulty}</span>
                  <span>Hard</span>
                </div>
              </div>
            </div>

            {/* Switch User */}
            {otherUsers.length > 0 && (
              <div className="setting-section">
                <label className="setting-label">Switch Account</label>
                <div className="switch-user-container">
                  <select
                    value={selectedUserId}
                    onChange={(e) => setSelectedUserId(e.target.value)}
                    className="user-select"
                    disabled={loading || isSwitching}
                  >
                    <option value={currentUser.uniqueId}>
                      {currentUser.name} (current)
                    </option>
                    {otherUsers.map((u) => (
                      <option key={u.uniqueId} value={u.uniqueId}>
                        {u.name}
                      </option>
                    ))}
                  </select>
                  <button
                    className="btn-secondary"
                    onClick={handleUserSwitch}
                    disabled={selectedUserId === currentUser.uniqueId || loading || isSwitching}
                  >
                    {isSwitching ? 'Switching...' : 'Switch'}
                  </button>
                </div>
              </div>
            )}

            {/* Stats */}
            <div className="setting-section stats-section">
              <label className="setting-label">Your Stats</label>
              <div className="stats-grid">
                <div className="stat-item">
                  <span className="stat-value">{currentUser.lifetimePoints.toLocaleString()}</span>
                  <span className="stat-label">Lifetime Points</span>
                </div>
              </div>
            </div>
          </>
        )}

        <div className="modal-actions">
          {!isGuest && (
            <button
              className="btn-danger"
              onClick={handleSignOut}
              disabled={loading}
            >
              Sign Out
            </button>
          )}
          <button
            className="btn-secondary"
            onClick={onClose}
          >
            Close
          </button>
        </div>
      </div>
    </div>
  );
};
