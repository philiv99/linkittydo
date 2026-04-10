import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { NavHeader } from '../components/NavHeader';

const renderWithRouter = (ui: React.ReactElement, { route = '/' } = {}) => {
  return render(
    <MemoryRouter initialEntries={[route]}>
      {ui}
    </MemoryRouter>
  );
};

describe('NavHeader', () => {
  it('renders the brand logo link', () => {
    renderWithRouter(
      <NavHeader userName="Guest" isGuest={true} isAdmin={false} lifetimePoints={0} onUserClick={() => {}} />
    );
    expect(screen.getByText('LinkittyDo!')).toBeInTheDocument();
  });

  it('renders Play and Leaderboard navigation links', () => {
    renderWithRouter(
      <NavHeader userName="Guest" isGuest={true} isAdmin={false} lifetimePoints={0} onUserClick={() => {}} />
    );
    expect(screen.getByText('Play')).toBeInTheDocument();
    expect(screen.getByText('Leaderboard')).toBeInTheDocument();
  });

  it('does not show History link for guest users', () => {
    renderWithRouter(
      <NavHeader userName="Guest" isGuest={true} isAdmin={false} lifetimePoints={0} onUserClick={() => {}} />
    );
    expect(screen.queryByText('History')).not.toBeInTheDocument();
  });

  it('shows History link for registered users', () => {
    renderWithRouter(
      <NavHeader userName="Alice" isGuest={false} isAdmin={false} lifetimePoints={500} onUserClick={() => {}} />
    );
    expect(screen.getByText('History')).toBeInTheDocument();
  });

  it('displays user name', () => {
    renderWithRouter(
      <NavHeader userName="Alice" isGuest={false} isAdmin={false} lifetimePoints={500} onUserClick={() => {}} />
    );
    expect(screen.getByText('Alice')).toBeInTheDocument();
  });

  it('shows guest badge for guest users', () => {
    renderWithRouter(
      <NavHeader userName="Guest" isGuest={true} isAdmin={false} lifetimePoints={0} onUserClick={() => {}} />
    );
    expect(screen.getByText('(Guest)')).toBeInTheDocument();
  });

  it('shows lifetime points for registered users', () => {
    renderWithRouter(
      <NavHeader userName="Alice" isGuest={false} isAdmin={false} lifetimePoints={1500} onUserClick={() => {}} />
    );
    expect(screen.getByText('1,500 pts')).toBeInTheDocument();
  });

  it('does not show points for guest users', () => {
    renderWithRouter(
      <NavHeader userName="Guest" isGuest={true} isAdmin={false} lifetimePoints={0} onUserClick={() => {}} />
    );
    expect(screen.queryByText('0 pts')).not.toBeInTheDocument();
  });

  it('highlights the active route', () => {
    renderWithRouter(
      <NavHeader userName="Guest" isGuest={true} isAdmin={false} lifetimePoints={0} onUserClick={() => {}} />,
      { route: '/play' }
    );
    const playLink = screen.getByText('Play');
    expect(playLink.className).toContain('active');
  });

  it('shows Admin link for admin users', () => {
    renderWithRouter(
      <NavHeader userName="AdminUser" isGuest={false} isAdmin={true} lifetimePoints={0} onUserClick={() => {}} />
    );
    expect(screen.getByRole('link', { name: 'Admin' })).toBeInTheDocument();
  });

  it('does not show Admin link for non-admin users', () => {
    renderWithRouter(
      <NavHeader userName="Alice" isGuest={false} isAdmin={false} lifetimePoints={500} onUserClick={() => {}} />
    );
    expect(screen.queryByRole('link', { name: 'Admin' })).not.toBeInTheDocument();
  });

  it('does not show Admin link for guest users', () => {
    renderWithRouter(
      <NavHeader userName="Guest" isGuest={true} isAdmin={false} lifetimePoints={0} onUserClick={() => {}} />
    );
    expect(screen.queryByRole('link', { name: 'Admin' })).not.toBeInTheDocument();
  });

  it('Admin link points to /admin', () => {
    renderWithRouter(
      <NavHeader userName="AdminUser" isGuest={false} isAdmin={true} lifetimePoints={0} onUserClick={() => {}} />
    );
    const adminLink = screen.getByRole('link', { name: 'Admin' });
    expect(adminLink).toHaveAttribute('href', '/admin');
  });
});
