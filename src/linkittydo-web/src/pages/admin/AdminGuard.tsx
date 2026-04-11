import { Navigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';

export function AdminGuard({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isAdmin } = useAuth();

  if (!isAuthenticated) {
    // Not logged in at all — redirect to play (login modal will handle auth)
    return <Navigate to="/play" replace />;
  }

  if (!isAdmin) {
    // Logged in but not an admin — redirect to play
    return <Navigate to="/play" replace />;
  }

  return <>{children}</>;
}
