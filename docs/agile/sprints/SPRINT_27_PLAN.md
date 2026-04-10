# Sprint 27 Plan

**Goal**: Show admin navigation link for users with the Admin role (roleId 3) in the main NavHeader.

**Duration**: April 10, 2026

**Estimated effort**: ~4 hours

## Selected Items

### 1. Admin role visibility in main navigation

**Source**: New backlog item - Admin functionality access for admin users

**Tasks**:
- A. Backend: Add roles to AuthResponse and JWT claims (~1h)
- B. Frontend: Add roles to types and useUser state (~0.5h)
- C. Frontend: Conditional Admin link in NavHeader (~0.5h)
- D. Tests: Backend + frontend tests for admin nav (~1h)
- E. Fix pre-existing test TS errors in GameHistoryPage.test.tsx (~0.5h)

**Acceptance Criteria**:
- [ ] Login/register response includes user roles
- [ ] JWT token includes role claims
- [ ] Admin users see "Admin" link in NavHeader next to Leaderboard
- [ ] Non-admin users do NOT see the Admin link
- [ ] Guest users do NOT see the Admin link
- [ ] Clicking Admin link navigates to /admin (existing admin dashboard)
- [ ] All tests pass
- [ ] Vite build succeeds

**Risk**: Low

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Role query adds latency to login | Low | Small join table, single query |
| Pre-existing test errors | Low | Fix as part of sprint |
