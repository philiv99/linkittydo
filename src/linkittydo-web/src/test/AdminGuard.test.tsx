import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { AdminGuard } from '../pages/admin/AdminGuard';

// Mock useAuth hook
const mockUseAuth = vi.fn();
vi.mock('../contexts/AuthContext', () => ({
  useAuth: () => mockUseAuth(),
}));

const renderWithRouter = (initialRoute: string) => {
  return render(
    <MemoryRouter initialEntries={[initialRoute]}>
      <Routes>
        <Route path="/admin" element={<AdminGuard><div>Admin Content</div></AdminGuard>} />
        <Route path="/play" element={<div>Play Page</div>} />
      </Routes>
    </MemoryRouter>
  );
};

describe('AdminGuard', () => {
  it('renders children when user is authenticated and admin', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isAdmin: true });
    renderWithRouter('/admin');
    expect(screen.getByText('Admin Content')).toBeInTheDocument();
  });

  it('redirects to /play when user is not authenticated', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: false, isAdmin: false });
    renderWithRouter('/admin');
    expect(screen.queryByText('Admin Content')).not.toBeInTheDocument();
    expect(screen.getByText('Play Page')).toBeInTheDocument();
  });

  it('redirects to /play when user is authenticated but not admin', () => {
    mockUseAuth.mockReturnValue({ isAuthenticated: true, isAdmin: false });
    renderWithRouter('/admin');
    expect(screen.queryByText('Admin Content')).not.toBeInTheDocument();
    expect(screen.getByText('Play Page')).toBeInTheDocument();
  });
});
