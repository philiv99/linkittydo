import { createContext, useContext, useState, useCallback, useEffect, type ReactNode } from 'react';
import { api, getStoredToken, clearTokens } from '../services/api';
import type { RegisterRequest, LoginRequest, AuthResponse } from '../types';

function parseJwtPayload(token: string): Record<string, unknown> | null {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;
    const payload = atob(parts[1].replace(/-/g, '+').replace(/_/g, '/'));
    return JSON.parse(payload);
  } catch {
    return null;
  }
}

function extractRoles(token: string): string[] {
  const payload = parseJwtPayload(token);
  if (!payload) return [];
  const roleClaim = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? payload['role'];
  if (!roleClaim) return [];
  const roles = Array.isArray(roleClaim) ? roleClaim : [roleClaim];
  return roles.filter((r): r is string => typeof r === 'string');
}

function isTokenExpired(token: string): boolean {
  const payload = parseJwtPayload(token);
  if (!payload || typeof payload.exp !== 'number') return true;
  return payload.exp * 1000 < Date.now();
}

interface AuthUser {
  uniqueId: string;
  name: string;
  email: string;
  roles: string[];
}

interface AuthContextValue {
  token: string | null;
  authUser: AuthUser | null;
  roles: string[];
  isAuthenticated: boolean;
  isAdmin: boolean;
  loading: boolean;
  error: string | null;
  login: (request: LoginRequest) => Promise<AuthResponse>;
  register: (request: RegisterRequest) => Promise<AuthResponse>;
  logout: () => void;
  clearError: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => {
    const stored = getStoredToken();
    if (stored && !isTokenExpired(stored)) return stored;
    if (stored) clearTokens();
    return null;
  });

  const [authUser, setAuthUser] = useState<AuthUser | null>(() => {
    const stored = getStoredToken();
    if (!stored || isTokenExpired(stored)) return null;
    const payload = parseJwtPayload(stored);
    if (!payload) return null;
    return {
      uniqueId: (payload.sub ?? payload.nameid ?? '') as string,
      name: (payload.unique_name ?? payload.name ?? '') as string,
      email: (payload.email ?? '') as string,
      roles: extractRoles(stored),
    };
  });

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const roles = token ? extractRoles(token) : [];
  const isAuthenticated = !!token && !isTokenExpired(token);
  const isAdmin = isAuthenticated && roles.some(r => r.toLowerCase() === 'admin');

  // Clear expired tokens on interval
  useEffect(() => {
    const interval = setInterval(() => {
      const current = getStoredToken();
      if (current && isTokenExpired(current)) {
        clearTokens();
        setToken(null);
        setAuthUser(null);
      }
    }, 60_000);
    return () => clearInterval(interval);
  }, []);

  const login = useCallback(async (request: LoginRequest): Promise<AuthResponse> => {
    setLoading(true);
    setError(null);
    try {
      const response = await api.login(request);
      setToken(response.accessToken);
      setAuthUser({
        uniqueId: response.uniqueId,
        name: response.name,
        email: response.email,
        roles: response.roles ?? [],
      });
      return response;
    } catch (e) {
      const message = e instanceof Error ? e.message : 'Login failed';
      setError(message);
      throw e;
    } finally {
      setLoading(false);
    }
  }, []);

  const register = useCallback(async (request: RegisterRequest): Promise<AuthResponse> => {
    setLoading(true);
    setError(null);
    try {
      const response = await api.register(request);
      setToken(response.accessToken);
      setAuthUser({
        uniqueId: response.uniqueId,
        name: response.name,
        email: response.email,
        roles: response.roles ?? [],
      });
      return response;
    } catch (e) {
      const message = e instanceof Error ? e.message : 'Registration failed';
      setError(message);
      throw e;
    } finally {
      setLoading(false);
    }
  }, []);

  const logout = useCallback(() => {
    clearTokens();
    // Also clear any legacy admin tokens
    localStorage.removeItem('linkittydo_admin_token');
    localStorage.removeItem('linkittydo_admin_refresh_token');
    setToken(null);
    setAuthUser(null);
    setError(null);
  }, []);

  const clearAuthError = useCallback(() => setError(null), []);

  // Sync token state when token changes in another tab or via api.refreshToken
  useEffect(() => {
    const handleStorage = (e: StorageEvent) => {
      if (e.key === 'linkittydo_token') {
        const newToken = e.newValue;
        if (newToken && !isTokenExpired(newToken)) {
          setToken(newToken);
          const payload = parseJwtPayload(newToken);
          if (payload) {
            setAuthUser({
              uniqueId: (payload.sub ?? payload.nameid ?? '') as string,
              name: (payload.unique_name ?? payload.name ?? '') as string,
              email: (payload.email ?? '') as string,
              roles: extractRoles(newToken),
            });
          }
        } else {
          setToken(null);
          setAuthUser(null);
        }
      }
    };
    window.addEventListener('storage', handleStorage);
    return () => window.removeEventListener('storage', handleStorage);
  }, []);

  return (
    <AuthContext.Provider value={{
      token,
      authUser,
      roles,
      isAuthenticated,
      isAdmin,
      loading,
      error,
      login,
      register,
      logout,
      clearError: clearAuthError,
    }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
