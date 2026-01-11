import { useState, useEffect, useCallback } from 'react';
import { api } from '../services/api';
import type { User, CreateUserRequest, UserResponse } from '../types';

const STORAGE_KEY = 'linkittydo_user';

const generateGuestUniqueId = (): string => {
  const timestamp = Date.now();
  const random = Math.random().toString(36).substring(2, 8).toUpperCase();
  return `USR-${timestamp}-${random}`;
};

const getStoredUser = (): User | null => {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored) {
      return JSON.parse(stored);
    }
  } catch (e) {
    console.warn('Failed to read user from localStorage:', e);
  }
  return null;
};

const saveUserToStorage = (user: User): void => {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
  } catch (e) {
    console.warn('Failed to save user to localStorage:', e);
  }
};

const createGuestUser = (): User => {
  return {
    uniqueId: generateGuestUniqueId(),
    name: 'Guest',
    email: '',
    lifetimePoints: 0,
    preferredDifficulty: 10,
  };
};

const mapResponseToUser = (response: UserResponse): User => ({
  uniqueId: response.uniqueId,
  name: response.name,
  email: response.email,
  lifetimePoints: response.lifetimePoints,
  preferredDifficulty: response.preferredDifficulty,
  createdAt: response.createdAt,
});

export const useUser = () => {
  const [user, setUser] = useState<User>(() => {
    const stored = getStoredUser();
    if (stored) {
      // Ensure stored user has new fields with defaults
      return {
        ...stored,
        lifetimePoints: stored.lifetimePoints ?? 0,
        preferredDifficulty: stored.preferredDifficulty ?? 10,
      };
    }
    const guest = createGuestUser();
    saveUserToStorage(guest);
    return guest;
  });

  const [isGuest, setIsGuest] = useState<boolean>(() => {
    const stored = getStoredUser();
    return !stored || stored.name === 'Guest' || !stored.email;
  });

  const [allUsers, setAllUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Save to localStorage whenever user changes
  useEffect(() => {
    saveUserToStorage(user);
    setIsGuest(user.name === 'Guest' || !user.email);
  }, [user]);

  // Fetch all users on mount
  const fetchAllUsers = useCallback(async () => {
    try {
      const users = await api.getAllUsers();
      setAllUsers(users.map(mapResponseToUser));
    } catch (e) {
      console.warn('Failed to fetch users:', e);
    }
  }, []);

  useEffect(() => {
    fetchAllUsers();
  }, [fetchAllUsers]);

  // Sync user data from server if logged in
  useEffect(() => {
    const syncUser = async () => {
      if (!isGuest && user.uniqueId) {
        try {
          const serverUser = await api.getUser(user.uniqueId);
          if (serverUser) {
            const syncedUser = mapResponseToUser(serverUser);
            setUser(syncedUser);
          }
        } catch (e) {
          console.warn('Failed to sync user from server:', e);
        }
      }
    };
    syncUser();
    // Only run on mount
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const registerUser = useCallback(async (request: CreateUserRequest): Promise<boolean> => {
    setLoading(true);
    setError(null);
    
    try {
      const response = await api.createUser(request);
      const newUser = mapResponseToUser(response);
      setUser(newUser);
      await fetchAllUsers(); // Refresh user list
      return true;
    } catch (e) {
      const message = e instanceof Error ? e.message : 'Failed to register user';
      setError(message);
      return false;
    } finally {
      setLoading(false);
    }
  }, [fetchAllUsers]);

  const updateUser = useCallback(async (request: CreateUserRequest): Promise<boolean> => {
    setLoading(true);
    setError(null);
    
    try {
      const response = await api.updateUser(user.uniqueId, request);
      const updatedUser = mapResponseToUser(response);
      setUser(updatedUser);
      await fetchAllUsers(); // Refresh user list
      return true;
    } catch (e) {
      const message = e instanceof Error ? e.message : 'Failed to update user';
      setError(message);
      return false;
    } finally {
      setLoading(false);
    }
  }, [user.uniqueId, fetchAllUsers]);

  const switchUser = useCallback(async (uniqueId: string): Promise<boolean> => {
    setLoading(true);
    setError(null);
    
    try {
      const response = await api.getUser(uniqueId);
      if (response) {
        const switchedUser = mapResponseToUser(response);
        setUser(switchedUser);
        return true;
      }
      setError('User not found');
      return false;
    } catch (e) {
      const message = e instanceof Error ? e.message : 'Failed to switch user';
      setError(message);
      return false;
    } finally {
      setLoading(false);
    }
  }, []);

  const updateDifficulty = useCallback(async (difficulty: number): Promise<boolean> => {
    if (isGuest) {
      // For guest, just update locally
      setUser(prev => ({ ...prev, preferredDifficulty: difficulty }));
      return true;
    }

    try {
      const response = await api.updateDifficulty(user.uniqueId, difficulty);
      setUser(prev => ({ ...prev, preferredDifficulty: response.preferredDifficulty }));
      return true;
    } catch (e) {
      const message = e instanceof Error ? e.message : 'Failed to update difficulty';
      setError(message);
      return false;
    }
  }, [user.uniqueId, isGuest]);

  const addPoints = useCallback(async (points: number): Promise<boolean> => {
    if (isGuest) {
      // For guest, just update locally
      setUser(prev => ({ ...prev, lifetimePoints: prev.lifetimePoints + points }));
      return true;
    }

    try {
      const response = await api.addPoints(user.uniqueId, points);
      setUser(prev => ({ ...prev, lifetimePoints: response.lifetimePoints }));
      return true;
    } catch (e) {
      console.warn('Failed to add points:', e);
      // Still update locally even if server fails
      setUser(prev => ({ ...prev, lifetimePoints: prev.lifetimePoints + points }));
      return false;
    }
  }, [user.uniqueId, isGuest]);

  const checkNameAvailability = useCallback(async (name: string): Promise<boolean> => {
    try {
      return await api.checkNameAvailability(name);
    } catch {
      return false;
    }
  }, []);

  const checkEmailAvailability = useCallback(async (email: string): Promise<boolean> => {
    try {
      return await api.checkEmailAvailability(email);
    } catch {
      return false;
    }
  }, []);

  const resetToGuest = useCallback(() => {
    const guest = createGuestUser();
    setUser(guest);
    setError(null);
  }, []);

  const signOut = useCallback(() => {
    resetToGuest();
  }, [resetToGuest]);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  return {
    user,
    isGuest,
    allUsers,
    loading,
    error,
    registerUser,
    updateUser,
    switchUser,
    updateDifficulty,
    addPoints,
    checkNameAvailability,
    checkEmailAvailability,
    resetToGuest,
    signOut,
    clearError,
    fetchAllUsers,
  };
};
