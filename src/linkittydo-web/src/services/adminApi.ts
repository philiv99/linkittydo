import type {
  DashboardStats,
  AdminUser,
  PaginatedResponse,
  PlayerAnalytics,
  AdminGame,
  GameDetail,
  SiteConfigEntry,
  DataSummary,
  SimulationSummary,
  PlayerDetail,
  AdminPhrase,
  PhraseStats,
  UserRoles,
  AuditLogEntry,
} from '../types/admin';

const getApiBaseUrl = (): string => {
  const baseUrl = (import.meta.env.VITE_API_BASE_URL || 'http://localhost:5157/api').replace(/\/$/, '');
  return baseUrl.endsWith('/api') ? baseUrl : `${baseUrl}/api`;
};

const API_BASE_URL = getApiBaseUrl();

const ADMIN_TOKEN_KEY = 'linkittydo_admin_token';
const ADMIN_REFRESH_TOKEN_KEY = 'linkittydo_admin_refresh_token';

export const getAdminToken = (): string | null => localStorage.getItem(ADMIN_TOKEN_KEY);
export const getAdminRefreshToken = (): string | null => localStorage.getItem(ADMIN_REFRESH_TOKEN_KEY);
export const storeAdminTokens = (accessToken: string, refreshToken: string): void => {
  localStorage.setItem(ADMIN_TOKEN_KEY, accessToken);
  localStorage.setItem(ADMIN_REFRESH_TOKEN_KEY, refreshToken);
};
export const clearAdminTokens = (): void => {
  localStorage.removeItem(ADMIN_TOKEN_KEY);
  localStorage.removeItem(ADMIN_REFRESH_TOKEN_KEY);
};

const adminHeaders = (): Record<string, string> => {
  const token = getAdminToken();
  return {
    'Content-Type': 'application/json',
    ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
  };
};

async function handleResponse<T>(response: Response): Promise<T> {
  if (response.status === 401) {
    clearAdminTokens();
    window.location.href = '/linkittydo/admin/login';
    throw new Error('Unauthorized');
  }
  if (!response.ok) {
    const errorData = await response.json().catch(() => null);
    throw new Error(errorData?.error?.message || `Request failed: ${response.status}`);
  }
  if (response.status === 204) return undefined as T;
  const wrapper = await response.json();
  return wrapper.data;
}

async function handlePaginatedResponse<T>(response: Response): Promise<PaginatedResponse<T>> {
  if (response.status === 401) {
    clearAdminTokens();
    window.location.href = '/linkittydo/admin/login';
    throw new Error('Unauthorized');
  }
  if (!response.ok) {
    const errorData = await response.json().catch(() => null);
    throw new Error(errorData?.error?.message || `Request failed: ${response.status}`);
  }
  return response.json();
}

export const adminApi = {
  async login(email: string, password: string): Promise<{ uniqueId: string; name: string; accessToken: string; refreshToken: string }> {
    const response = await fetch(`${API_BASE_URL}/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
    });
    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw new Error(errorData?.error?.message || 'Login failed');
    }
    const wrapper = await response.json();
    const data = wrapper.data;
    storeAdminTokens(data.accessToken, data.refreshToken);
    return data;
  },

  logout(): void {
    clearAdminTokens();
  },

  isAuthenticated(): boolean {
    return !!getAdminToken();
  },

  // Dashboard
  async getDashboard(): Promise<DashboardStats> {
    const response = await fetch(`${API_BASE_URL}/admin/dashboard`, { headers: adminHeaders() });
    return handleResponse<DashboardStats>(response);
  },

  // Users
  async getUsers(page = 1, pageSize = 20, isSimulated?: boolean, search?: string): Promise<PaginatedResponse<AdminUser>> {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (isSimulated !== undefined) params.set('isSimulated', String(isSimulated));
    if (search) params.set('search', search);
    const response = await fetch(`${API_BASE_URL}/admin/users?${params}`, { headers: adminHeaders() });
    return handlePaginatedResponse<AdminUser>(response);
  },

  async setUserStatus(uniqueId: string, isActive: boolean): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/admin/users/${uniqueId}/status`, {
      method: 'PATCH',
      headers: adminHeaders(),
      body: JSON.stringify({ isActive }),
    });
    await handleResponse<void>(response);
  },

  async getPlayerAnalytics(uniqueId: string): Promise<PlayerAnalytics> {
    const response = await fetch(`${API_BASE_URL}/admin/users/${uniqueId}/analytics`, { headers: adminHeaders() });
    return handleResponse<PlayerAnalytics>(response);
  },

  // Games
  async getGames(page = 1, pageSize = 20, filters?: { userId?: string; result?: string; isSimulated?: boolean }): Promise<PaginatedResponse<AdminGame>> {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (filters?.userId) params.set('userId', filters.userId);
    if (filters?.result) params.set('result', filters.result);
    if (filters?.isSimulated !== undefined) params.set('isSimulated', String(filters.isSimulated));
    const response = await fetch(`${API_BASE_URL}/admin/games?${params}`, { headers: adminHeaders() });
    return handlePaginatedResponse<AdminGame>(response);
  },

  async getGameDetail(gameId: string): Promise<GameDetail> {
    const response = await fetch(`${API_BASE_URL}/admin/games/${gameId}`, { headers: adminHeaders() });
    return handleResponse<GameDetail>(response);
  },

  // Site Config
  async getConfigs(): Promise<SiteConfigEntry[]> {
    const response = await fetch(`${API_BASE_URL}/admin/config`, { headers: adminHeaders() });
    return handleResponse<SiteConfigEntry[]>(response);
  },

  async setConfig(key: string, value: string): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/admin/config/${encodeURIComponent(key)}`, {
      method: 'PUT',
      headers: adminHeaders(),
      body: JSON.stringify({ value }),
    });
    await handleResponse<void>(response);
  },

  // Data Explorer
  async getDataSummary(): Promise<DataSummary> {
    const response = await fetch(`${API_BASE_URL}/admin/data/summary`, { headers: adminHeaders() });
    return handleResponse<DataSummary>(response);
  },

  async getSimulationSummary(): Promise<SimulationSummary> {
    const response = await fetch(`${API_BASE_URL}/admin/data/simulation-summary`, { headers: adminHeaders() });
    return handleResponse<SimulationSummary>(response);
  },

  async getPlayerDetail(userId: string): Promise<PlayerDetail> {
    const response = await fetch(`${API_BASE_URL}/admin/data/player/${userId}`, { headers: adminHeaders() });
    return handleResponse<PlayerDetail>(response);
  },

  // Phrases
  async getPhrases(page = 1, pageSize = 20, isActive?: boolean): Promise<PaginatedResponse<AdminPhrase>> {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (isActive !== undefined) params.set('isActive', String(isActive));
    const response = await fetch(`${API_BASE_URL}/admin/phrases?${params}`, { headers: adminHeaders() });
    return handlePaginatedResponse<AdminPhrase>(response);
  },

  async createPhrase(text: string, difficulty: number): Promise<AdminPhrase> {
    const response = await fetch(`${API_BASE_URL}/admin/phrases`, {
      method: 'POST',
      headers: adminHeaders(),
      body: JSON.stringify({ text, difficulty }),
    });
    return handleResponse<AdminPhrase>(response);
  },

  async updatePhrase(uniqueId: string, text: string, difficulty: number): Promise<AdminPhrase> {
    const response = await fetch(`${API_BASE_URL}/admin/phrases/${uniqueId}`, {
      method: 'PUT',
      headers: adminHeaders(),
      body: JSON.stringify({ text, difficulty }),
    });
    return handleResponse<AdminPhrase>(response);
  },

  async setPhraseStatus(uniqueId: string, isActive: boolean): Promise<void> {
    const response = await fetch(`${API_BASE_URL}/admin/phrases/${uniqueId}/status`, {
      method: 'PATCH',
      headers: adminHeaders(),
      body: JSON.stringify({ isActive }),
    });
    await handleResponse<void>(response);
  },

  async getPhraseStats(uniqueId: string): Promise<PhraseStats> {
    const response = await fetch(`${API_BASE_URL}/admin/phrases/${uniqueId}/stats`, { headers: adminHeaders() });
    return handleResponse<PhraseStats>(response);
  },

  // User Roles
  async getUserRoles(uniqueId: string): Promise<UserRoles> {
    const response = await fetch(`${API_BASE_URL}/admin/users/${uniqueId}/roles`, { headers: adminHeaders() });
    return handleResponse<UserRoles>(response);
  },

  async assignRole(uniqueId: string, roleName: string): Promise<UserRoles> {
    const response = await fetch(`${API_BASE_URL}/admin/users/${uniqueId}/roles`, {
      method: 'POST',
      headers: adminHeaders(),
      body: JSON.stringify({ roleName }),
    });
    return handleResponse<UserRoles>(response);
  },

  async removeRole(uniqueId: string, roleName: string): Promise<UserRoles> {
    const response = await fetch(`${API_BASE_URL}/admin/users/${uniqueId}/roles/${encodeURIComponent(roleName)}`, {
      method: 'DELETE',
      headers: adminHeaders(),
    });
    return handleResponse<UserRoles>(response);
  },

  // Audit Log
  async getAuditLog(page = 1, pageSize = 50, action?: string, userId?: string, from?: string, to?: string): Promise<PaginatedResponse<AuditLogEntry>> {
    const params = new URLSearchParams({ page: page.toString(), pageSize: pageSize.toString() });
    if (action) params.append('action', action);
    if (userId) params.append('userId', userId);
    if (from) params.append('from', from);
    if (to) params.append('to', to);
    const response = await fetch(`${API_BASE_URL}/admin/audit-log?${params}`, {
      headers: adminHeaders(),
    });
    return handleResponse<PaginatedResponse<AuditLogEntry>>(response);
  },

  async getAuditActions(): Promise<string[]> {
    const response = await fetch(`${API_BASE_URL}/admin/audit-log/actions`, {
      headers: adminHeaders(),
    });
    return handleResponse<string[]>(response);
  },
};
