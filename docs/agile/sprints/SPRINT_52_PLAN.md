# Sprint 52 Plan

**Date**: 2026-04-12
**Sprint Goal**: Fix navigation and auth-state bugs — Profile redirect, History empty state, stale header points

## Selected Items

| # | Backlog Item | Priority |
|---|-------------|----------|
| B1 | Profile nav redirects to Play (starts new game) | P1 |
| B2 | History page shows no games for logged-in user | P1 |
| B3 | Header points not up-to-date | P1 |
| B4 | ProfilePage.test.tsx build error (GameResult type) | P1 |

## Root Cause Analysis

All three user-facing bugs share a common root cause: **the disconnect between `useUser` (localStorage-based user state) and `useAuth` (JWT-based authentication)**.

- `useUser` stores user data in localStorage and reads it on every mount. As long as localStorage has a non-guest user, the app shows Profile/History links.
- `useAuth` manages JWT tokens. When a JWT expires, `isAuthenticated` becomes `false`, but `useUser` still shows the user as logged in.
- Protected pages (Profile, History) and protected API endpoints (`[Authorize]`) both fail silently when the JWT is expired.

Additionally, `useUser` only syncs user data (including `lifetimePoints`) from the server once on mount, so the header points drift from the actual server-side total.

## Tasks

| # | Task | Estimate | Tests Expected |
|---|------|----------|----------------|
| 1 | Fix ProfilePage.test.tsx build error (cast `result` to `GameResult`) | S | Existing tests pass |
| 2 | Add `useAuth` awareness to GameHistoryPage — check `isAuthenticated` before API calls, show re-login prompt on auth failure | M | 2 new tests (auth redirect, auth error handling) |
| 3 | Fix ProfilePage to attempt token refresh before redirecting to /play; show re-login prompt instead of silent redirect | M | 2 new tests (refresh attempt, re-login prompt) |
| 4 | Add user data re-sync to `useUser` hook — refresh `lifetimePoints` from server after game completion and when user navigates to a new page | M | 2 new tests (sync after game, sync on navigation) |
| 5 | Add `refreshUser` function to `useUser` hook, call it from ProfilePage and GameHistoryPage on mount | S | 1 new test (refreshUser updates state) |
| 6 | Verify full flow: login → play game → navigate to Profile → verify points match → navigate to History → verify games show | M | No new tests (manual integration verification via build) |
| 7 | Update BACKLOG.md (mark B1-B4 complete), CHANGELOG.md, sprint docs | S | No new tests |

## Acceptance Criteria

- [ ] `npm run build` passes (fixes B4 pre-existing error)
- [ ] Clicking "Profile" while logged in shows the Profile page (not Play page)
- [ ] Clicking "History" while logged in shows actual game history
- [ ] Header points match server-side total after completing a game
- [ ] When JWT expires, auth-protected pages show a re-login prompt instead of silently redirecting
- [ ] All new code has corresponding tests (7+ new frontend tests)
- [ ] Both backend and frontend builds pass
- [ ] All existing tests continue to pass

## Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Token refresh logic is complex | Medium | Leverage existing `attemptTokenRefresh` in AuthContext |
| useUser re-sync could cause infinite re-render loops | Medium | Use stable dependencies and guard with refs |
| GameBoard auto-start interacts with auth redirects | Low | Only fix the redirect source; GameBoard auto-start is correct behavior for /play |
