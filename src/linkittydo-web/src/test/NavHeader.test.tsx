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
      <NavHeader userName="Guest" isGuest={true} lifetimePoints={0} onUserClick={() => {}} />
    );
    expect(screen.getByText('LinkittyDo!')).toBeInTheDocument();
  });

  it('renders Home and Play navigation links', () => {
    renderWithRouter(
      <NavHeader userName="Guest" isGuest={true} lifetimePoints={0} onUserClick={() => {}} />
    );
    expect(screen.getByText('Home')).toBeInTheDocument();
    expect(screen.getByText('Play')).toBeInTheDocument();
  });

  it('does not show History link for guest users', () => {
    renderWithRouter(
      <NavHeader userName="Guest" isGuest={true} lifetimePoints={0} onUserClick={() => {}} />
    );
    expect(screen.queryByText('History')).not.toBeInTheDocument();
  });

  it('shows History link for registered users', () => {
    renderWithRouter(
      <NavHeader userName="Alice" isGuest={false} lifetimePoints={500} onUserClick={() => {}} />
    );
    expect(screen.getByText('History')).toBeInTheDocument();
  });

  it('displays user name', () => {
    renderWithRouter(
      <NavHeader userName="Alice" isGuest={false} lifetimePoints={500} onUserClick={() => {}} />
    );
    expect(screen.getByText('Alice')).toBeInTheDocument();
  });

  it('shows guest badge for guest users', () => {
    renderWithRouter(
      <NavHeader userName="Guest" isGuest={true} lifetimePoints={0} onUserClick={() => {}} />
    );
    expect(screen.getByText('(Guest)')).toBeInTheDocument();
  });

  it('shows lifetime points for registered users', () => {
    renderWithRouter(
      <NavHeader userName="Alice" isGuest={false} lifetimePoints={1500} onUserClick={() => {}} />
    );
    expect(screen.getByText('1,500 pts')).toBeInTheDocument();
  });

  it('does not show points for guest users', () => {
    renderWithRouter(
      <NavHeader userName="Guest" isGuest={true} lifetimePoints={0} onUserClick={() => {}} />
    );
    expect(screen.queryByText('0 pts')).not.toBeInTheDocument();
  });

  it('highlights the active route', () => {
    renderWithRouter(
      <NavHeader userName="Guest" isGuest={true} lifetimePoints={0} onUserClick={() => {}} />,
      { route: '/play' }
    );
    const playLink = screen.getByText('Play');
    expect(playLink.className).toContain('active');
  });
});
