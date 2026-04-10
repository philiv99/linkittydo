import { Link, useLocation } from 'react-router-dom';
import './NavHeader.css';

interface NavHeaderProps {
  userName: string;
  isGuest: boolean;
  isAdmin: boolean;
  lifetimePoints: number;
  onUserClick: () => void;
}

export const NavHeader: React.FC<NavHeaderProps> = ({
  userName,
  isGuest,
  isAdmin,
  lifetimePoints,
  onUserClick,
}) => {
  const location = useLocation();

  return (
    <header className="nav-header">
      <div className="nav-brand">
        <Link to="/" className="nav-logo">LinkittyDo!</Link>
      </div>
      <nav className="nav-links">
        <Link
          to="/play"
          className={`nav-link ${location.pathname === '/play' || location.pathname === '/' ? 'active' : ''}`}
        >
          Play
        </Link>
        {!isGuest && (
          <Link
            to="/history"
            className={`nav-link ${location.pathname === '/history' ? 'active' : ''}`}
          >
            History
          </Link>
        )}
        <Link
          to="/leaderboard"
          className={`nav-link ${location.pathname === '/leaderboard' ? 'active' : ''}`}
        >
          Leaderboard
        </Link>
        {isAdmin && (
          <Link
            to="/admin"
            className={`nav-link nav-link-admin ${location.pathname.startsWith('/admin') ? 'active' : ''}`}
          >
            Admin
          </Link>
        )}
      </nav>
      <div className="nav-user">
        <span
          className="nav-user-name"
          onClick={onUserClick}
          title={isGuest ? 'Click to create a profile' : 'Click to manage account'}
        >
          {userName}
          {isGuest && <span className="nav-guest-badge">(Guest)</span>}
        </span>
        {!isGuest && (
          <span className="nav-points" title="Lifetime Points">
            {lifetimePoints.toLocaleString()} pts
          </span>
        )}
      </div>
    </header>
  );
};
