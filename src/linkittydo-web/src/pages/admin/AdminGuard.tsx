import { Navigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';

export function AdminGuard({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isAdmin } = useAuth();

  if (!isAuthenticated) {
    // Not logged in at all — redirect to play (login modal will handle auth)
    return <Navigate to="/play" replace />;
  }

  if (!isAdmin) {
    // Logged in but not an admin — show access denied
    return (
      <div style={{ padding: '2rem', textAlign: 'center' }}>
        <h1>Access Denied</h1>
        <p>You do not have admin privileges.</p>
        <a href="/linkittydo/play">Return to game</a>
      </div>
    );
  }

  return <>{children}</>;
}
