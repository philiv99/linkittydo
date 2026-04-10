import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { adminApi } from '../../services/adminApi';
import './AdminLayout.css';

export function AdminLayout() {
  const navigate = useNavigate();

  const handleLogout = () => {
    adminApi.logout();
    navigate('/admin/login');
  };

  return (
    <div className="admin-layout">
      <aside className="admin-sidebar">
        <div className="admin-sidebar-header">
          <h2>LinkittyDo</h2>
          <span className="admin-badge">Admin</span>
        </div>
        <nav className="admin-nav">
          <NavLink to="/admin/dashboard" className={({ isActive }) => isActive ? 'active' : ''}>
            Dashboard
          </NavLink>
          <NavLink to="/admin/users" className={({ isActive }) => isActive ? 'active' : ''}>
            Users
          </NavLink>
          <NavLink to="/admin/games" className={({ isActive }) => isActive ? 'active' : ''}>
            Games
          </NavLink>
          <NavLink to="/admin/phrases" className={({ isActive }) => isActive ? 'active' : ''}>
            Phrases
          </NavLink>
          <NavLink to="/admin/config" className={({ isActive }) => isActive ? 'active' : ''}>
            Site Config
          </NavLink>
          <NavLink to="/admin/data" className={({ isActive }) => isActive ? 'active' : ''}>
            Data Explorer
          </NavLink>
        </nav>
        <div className="admin-sidebar-footer">
          <NavLink to="/play" className="back-to-game">Back to Game</NavLink>
          <button onClick={handleLogout} className="logout-button">Sign Out</button>
        </div>
      </aside>
      <main className="admin-main">
        <Outlet />
      </main>
    </div>
  );
}
