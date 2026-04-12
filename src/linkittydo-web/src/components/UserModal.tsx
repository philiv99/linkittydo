import React, { useState, useEffect } from 'react';
import './UserModal.css';

interface UserModalProps {
  isOpen: boolean;
  onClose: () => void;
  onRegister: (name: string, email: string, password: string) => Promise<boolean>;
  onLogin: (email: string, password: string) => Promise<boolean>;
  onCheckName: (name: string) => Promise<boolean>;
  onCheckEmail: (email: string) => Promise<boolean>;
  loading: boolean;
  error: string | null;
}

export const UserModal: React.FC<UserModalProps> = ({
  isOpen,
  onClose,
  onRegister,
  onLogin,
  onCheckName,
  onCheckEmail,
  loading,
  error,
}) => {
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [nameError, setNameError] = useState<string | null>(null);
  const [emailError, setEmailError] = useState<string | null>(null);
  const [passwordError, setPasswordError] = useState<string | null>(null);
  const [isCheckingName, setIsCheckingName] = useState(false);
  const [isCheckingEmail, setIsCheckingEmail] = useState(false);
  const [prevIsOpen, setPrevIsOpen] = useState(false);

  // Reset state when modal opens (state adjustment during render)
  if (isOpen && !prevIsOpen) {
    setName('');
    setEmail('');
    setPassword('');
    setNameError(null);
    setEmailError(null);
    setPasswordError(null);
    setMode('login');
  }
  if (isOpen !== prevIsOpen) {
    setPrevIsOpen(isOpen);
  }

  const handleNameChange = (value: string) => {
    setName(value);
    if (mode !== 'register' || !value || value.length < 2) {
      setNameError(null);
      return;
    }
    const nameRegex = /^[a-zA-Z0-9\s_-]+$/;
    if (!nameRegex.test(value)) {
      setNameError('Name can only contain letters, numbers, spaces, underscores, and hyphens');
      return;
    }
    if (value.length > 50) {
      setNameError('Name must be 50 characters or less');
      return;
    }
    setNameError(null);
  };

  const handleEmailChange = (value: string) => {
    setEmail(value);
    if (mode !== 'register' || !value) {
      setEmailError(null);
      return;
    }
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(value)) {
      setEmailError('Please enter a valid email address');
      return;
    }
    setEmailError(null);
  };

  const handlePasswordChange = (value: string) => {
    setPassword(value);
    if (!value) {
      setPasswordError(null);
    } else if (value.length < 8) {
      setPasswordError('Password must be at least 8 characters');
    } else {
      setPasswordError(null);
    }
  };

  // Debounced name availability check (register only, async only)
  useEffect(() => {
    if (mode !== 'register' || !name || name.length < 2) return;
    const nameRegex = /^[a-zA-Z0-9\s_-]+$/;
    if (!nameRegex.test(name) || name.length > 50) return;

    let cancelled = false;
    const timeoutId = setTimeout(async () => {
      if (cancelled) return;
      setIsCheckingName(true);
      const available = await onCheckName(name);
      if (cancelled) return;
      setIsCheckingName(false);
      if (!available) {
        setNameError('This name is already taken');
      } else {
        setNameError(null);
      }
    }, 500);

    return () => {
      cancelled = true;
      clearTimeout(timeoutId);
    };
  }, [name, onCheckName, mode]);

  // Debounced email availability check (register only, async only)
  useEffect(() => {
    if (mode !== 'register' || !email) return;
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) return;

    let cancelled = false;
    const timeoutId = setTimeout(async () => {
      if (cancelled) return;
      setIsCheckingEmail(true);
      const available = await onCheckEmail(email);
      if (cancelled) return;
      setIsCheckingEmail(false);
      if (!available) {
        setEmailError('This email is already registered');
      } else {
        setEmailError(null);
      }
    }, 500);

    return () => {
      cancelled = true;
      clearTimeout(timeoutId);
    };
  }, [email, onCheckEmail, mode]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (mode === 'login') {
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRegex.test(email)) {
        setEmailError('Please enter a valid email address');
        return;
      }
      if (!password) {
        setPasswordError('Password is required');
        return;
      }
      const success = await onLogin(email.trim(), password);
      if (success) onClose();
    } else {
      if (nameError || emailError || passwordError || isCheckingName || isCheckingEmail) return;
      if (name.length < 2) { setNameError('Name must be at least 2 characters'); return; }
      if (password.length < 8) { setPasswordError('Password must be at least 8 characters'); return; }
      const success = await onRegister(name.trim(), email.trim(), password);
      if (success) onClose();
    }
  };

  const isFormValid = mode === 'login'
    ? email.length > 0 && password.length > 0
    : (name.length >= 2 && email.length > 0 && password.length >= 8 &&
       !nameError && !emailError && !passwordError && !isCheckingName && !isCheckingEmail);

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <button className="modal-close" onClick={onClose} aria-label="Close">
          ×
        </button>
        
        <div className="auth-tabs">
          <button
            className={`auth-tab ${mode === 'login' ? 'active' : ''}`}
            onClick={() => setMode('login')}
            type="button"
          >
            Sign In
          </button>
          <button
            className={`auth-tab ${mode === 'register' ? 'active' : ''}`}
            onClick={() => setMode('register')}
            type="button"
          >
            Register
          </button>
        </div>
        
        <form onSubmit={handleSubmit}>
          {mode === 'register' && (
            <div className="form-group">
              <label htmlFor="name">Name</label>
              <input
                id="name"
                type="text"
                value={name}
              onChange={(e) => handleNameChange(e.target.value)}
                placeholder="Choose a display name"
                maxLength={50}
                disabled={loading}
                className={nameError ? 'error' : ''}
                autoComplete="off"
              />
              {isCheckingName && <span className="checking">Checking availability...</span>}
              {nameError && <span className="error-message">{nameError}</span>}
            </div>
          )}

          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(e) => handleEmailChange(e.target.value)}
              placeholder="Enter your email"
              disabled={loading}
              className={emailError ? 'error' : ''}
            />
            {isCheckingEmail && <span className="checking">Checking availability...</span>}
            {emailError && <span className="error-message">{emailError}</span>}
          </div>

          <div className="form-group">
            <label htmlFor="password">Password</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => handlePasswordChange(e.target.value)}
              placeholder={mode === 'register' ? 'At least 8 characters' : 'Enter your password'}
              disabled={loading}
              className={passwordError ? 'error' : ''}
              autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
            />
            {passwordError && <span className="error-message">{passwordError}</span>}
          </div>

          {error && <div className="form-error">{error}</div>}

          <div className="form-actions">
            <button
              type="button"
              className="btn-secondary"
              onClick={onClose}
              disabled={loading}
            >
              Cancel
            </button>
            <button
              type="submit"
              className="btn-primary"
              disabled={!isFormValid || loading}
            >
              {loading
                ? (mode === 'login' ? 'Signing In...' : 'Creating...')
                : (mode === 'login' ? 'Sign In' : 'Create Account')}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
