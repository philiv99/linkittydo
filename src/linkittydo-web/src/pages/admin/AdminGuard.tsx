import { Navigate } from 'react-router-dom';
import { getAdminToken } from '../../services/adminApi';

export function AdminGuard({ children }: { children: React.ReactNode }) {
  if (!getAdminToken()) {
    return <Navigate to="/admin/login" replace />;
  }
  return <>{children}</>;
}
