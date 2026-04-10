import { Navigate } from 'react-router-dom';
import { getAdminToken, clearAdminTokens } from '../../services/adminApi';

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

function hasAdminRole(token: string): boolean {
  const payload = parseJwtPayload(token);
  if (!payload) return false;

  // Check expiry
  if (typeof payload.exp === 'number' && payload.exp * 1000 < Date.now()) {
    return false;
  }

  // Check role claim (standard .NET ClaimTypes.Role URI or short "role")
  const roleClaim = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? payload['role'];
  if (!roleClaim) return false;

  const roles = Array.isArray(roleClaim) ? roleClaim : [roleClaim];
  return roles.some(r => typeof r === 'string' && r.toLowerCase() === 'admin');
}

export function AdminGuard({ children }: { children: React.ReactNode }) {
  const token = getAdminToken();
  if (!token || !hasAdminRole(token)) {
    clearAdminTokens();
    return <Navigate to="/admin/login" replace />;
  }
  return <>{children}</>;
}
