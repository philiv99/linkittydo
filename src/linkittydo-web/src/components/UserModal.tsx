import React, { useState, useEffect } from 'react';
import type { User } from '../types';
import './UserModal.css';

interface UserModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (name: string, email: string) => Promise<boolean>;
  onCheckName: (name: string) => Promise<boolean>;
  onCheckEmail: (email: string) => Promise<boolean>;
  onSelectExistingUser: (uniqueId: string) => Promise<boolean>;
  allUsers: User[];
  loading: boolean;
  error: string | null;
}

export const UserModal: React.FC<UserModalProps> = ({
  isOpen,
  onClose,
  onSubmit,
  onCheckName,
  onCheckEmail,
  onSelectExistingUser,
  allUsers,
  loading,
  error,
}) => {
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [nameError, setNameError] = useState<string | null>(null);
  const [emailError, setEmailError] = useState<string | null>(null);
  const [isCheckingName, setIsCheckingName] = useState(false);
  const [isCheckingEmail, setIsCheckingEmail] = useState(false);
  const [selectedExistingUser, setSelectedExistingUser] = useState<User | null>(null);
  const [isSigningIn, setIsSigningIn] = useState(false);

  // Reset form when modal opens
  useEffect(() => {
    if (isOpen) {
      setName('');
      setEmail('');
      setNameError(null);
      setEmailError(null);
      setSelectedExistingUser(null);
      setIsSigningIn(false);
    }
  }, [isOpen]);

  // Check if name matches an existing user
  useEffect(() => {
    if (!name) {
      setSelectedExistingUser(null);
      return;
    }

    const matchedUser = allUsers.find(
      u => u.name.toLowerCase() === name.toLowerCase()
    );

    if (matchedUser) {
      setSelectedExistingUser(matchedUser);
      setEmail(matchedUser.email);
      setNameError(null);
      setEmailError(null);
    } else {
      setSelectedExistingUser(null);
    }
  }, [name, allUsers]);

  // Debounced name availability check (only for new users)
  useEffect(() => {
    if (!name || name.length < 2 || selectedExistingUser) {
      if (!selectedExistingUser) setNameError(null);
      return;
    }

    // Validate format first
    const nameRegex = /^[a-zA-Z0-9\s_-]+$/;
    if (!nameRegex.test(name)) {
      setNameError('Name can only contain letters, numbers, spaces, underscores, and hyphens');
      return;
    }

    if (name.length > 50) {
      setNameError('Name must be 50 characters or less');
      return;
    }

    const timeoutId = setTimeout(async () => {
      setIsCheckingName(true);
      const available = await onCheckName(name);
      setIsCheckingName(false);
      if (!available) {
        setNameError('This name is already taken');
      } else {
        setNameError(null);
      }
    }, 500);

    return () => clearTimeout(timeoutId);
  }, [name, onCheckName, selectedExistingUser]);

  // Debounced email availability check (only for new users)
  useEffect(() => {
    if (!email || selectedExistingUser) {
      if (!selectedExistingUser) setEmailError(null);
      return;
    }

    // Validate email format
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
      setEmailError('Please enter a valid email address');
      return;
    }

    const timeoutId = setTimeout(async () => {
      setIsCheckingEmail(true);
      const available = await onCheckEmail(email);
      setIsCheckingEmail(false);
      if (!available) {
        setEmailError('This email is already registered');
      } else {
        setEmailError(null);
      }
    }, 500);

    return () => clearTimeout(timeoutId);
  }, [email, onCheckEmail, selectedExistingUser]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    // If an existing user is selected, sign in as that user
    if (selectedExistingUser) {
      setIsSigningIn(true);
      const success = await onSelectExistingUser(selectedExistingUser.uniqueId);
      setIsSigningIn(false);
      if (success) {
        onClose();
      }
      return;
    }

    // Otherwise, create new user
    if (nameError || emailError || isCheckingName || isCheckingEmail) {
      return;
    }

    if (name.length < 2) {
      setNameError('Name must be at least 2 characters');
      return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
      setEmailError('Please enter a valid email address');
      return;
    }

    const success = await onSubmit(name.trim(), email.trim());
    if (success) {
      onClose();
    }
  };

  const isFormValid = selectedExistingUser 
    ? true 
    : (name.length >= 2 && 
       email.length > 0 && 
       !nameError && 
       !emailError && 
       !isCheckingName && 
       !isCheckingEmail);

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <button className="modal-close" onClick={onClose} aria-label="Close">
          ×
        </button>
        
        <h2>{selectedExistingUser ? 'Welcome Back!' : 'Create Your Profile'}</h2>
        <p className="modal-subtitle">
          {selectedExistingUser 
            ? `Sign in as ${selectedExistingUser.name}` 
            : 'Enter your details or select an existing user'}
        </p>
        
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="name">Name</label>
            <input
              id="name"
              type="text"
              list="existing-users"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Enter name or select existing user"
              maxLength={50}
              disabled={loading || isSigningIn}
              className={nameError ? 'error' : selectedExistingUser ? 'matched' : ''}
              autoComplete="off"
            />
            <datalist id="existing-users">
              {allUsers.map((user) => (
                <option key={user.uniqueId} value={user.name}>
                  {user.email}
                </option>
              ))}
            </datalist>
            {isCheckingName && <span className="checking">Checking availability...</span>}
            {nameError && <span className="error-message">{nameError}</span>}
            {selectedExistingUser && (
              <span className="matched-message">✓ Existing user found</span>
            )}
          </div>

          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(e) => {
                if (!selectedExistingUser) {
                  setEmail(e.target.value);
                }
              }}
              placeholder="Enter your email"
              disabled={loading || isSigningIn || !!selectedExistingUser}
              className={emailError ? 'error' : selectedExistingUser ? 'matched' : ''}
            />
            {isCheckingEmail && <span className="checking">Checking availability...</span>}
            {emailError && <span className="error-message">{emailError}</span>}
          </div>

          {error && <div className="form-error">{error}</div>}

          <div className="form-actions">
            <button
              type="button"
              className="btn-secondary"
              onClick={onClose}
              disabled={loading || isSigningIn}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="btn-primary"
              disabled={!isFormValid || loading || isSigningIn}
            >
              {isSigningIn 
                ? 'Signing In...' 
                : loading 
                  ? 'Creating...' 
                  : selectedExistingUser 
                    ? 'Sign In' 
                    : 'Create Profile'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
