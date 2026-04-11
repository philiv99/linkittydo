# Sprint 44 Plan — Admin Nav Role Enforcement

**Date**: 2026-04-11
**Branch**: `feature/20260411-sprint-44`
**Backlog Item**: #129

---

## Goal

Fix admin navigation persisting after switching to a non-admin user. When a player without the admin role is the active user, admin nav links must be hidden and admin pages must redirect away.

## Root Cause

`switchUser()` in `useUser.ts` calls `api.getUser(uniqueId)` to update the displayed user profile but does NOT re-authenticate. The JWT token from the previous admin login persists in AuthContext, so `isAdmin` stays `true` even after switching to a non-admin user like Tom.

## Tasks

| # | Task | Description |
|---|------|-------------|
| 1 | Fix `switchUser` to clear auth state | When switching users, call `auth.logout()` to clear the JWT token. The switched user's profile is loaded but auth state resets to unauthenticated. This forces `isAdmin` to be `false` since no valid JWT exists. |
| 2 | Add redirect from admin pages for non-admin | Ensure `AdminGuard` redirects non-admin users to `/play` (not just shows "Access Denied"). |
| 3 | Verify NavHeader hides admin link | Confirm `isAdmin` prop flows correctly from cleared auth state through `useUser` to `NavHeader`. |
| 4 | Add frontend tests | Test that `switchUser` clears auth, test NavHeader conditional rendering. |
| 5 | Verify builds pass | Run `dotnet build`, `npm run build`, and all tests. |

## Test Expectations

- Test: switchUser clears authentication token
- Test: NavHeader does not render Admin link when isAdmin is false
- Test: NavHeader renders Admin link when isAdmin is true
- Test: AdminGuard redirects unauthenticated users to /play

## Acceptance Criteria

1. Switching from admin user to non-admin user hides admin nav link immediately
2. Non-admin users cannot see admin navigation
3. AdminGuard redirects (not just "Access Denied") for non-admin users
4. All existing tests pass, new tests added for the fix
5. Both frontend and backend builds pass cleanly
