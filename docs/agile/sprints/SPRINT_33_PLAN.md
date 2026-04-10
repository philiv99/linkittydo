# Sprint 33 Plan: Unified Auth — Eliminate Dual Token System

**Sprint Goal**: Unify player and admin authentication into a single token/context system so admin users who are already logged in as players are not forced to re-authenticate.

**Date**: 2026-04-10
**Branch**: `feature/20260410-sprint-33`
**Backlog Items**: #113, #114, #115

---

## Problem Statement

The app maintains two independent auth systems:
- **Player auth** (`api.ts`): stores JWT in `linkittydo_token`, used by `useUser` hook
- **Admin auth** (`adminApi.ts`): stores JWT in `linkittydo_admin_token`, used by `AdminGuard` and `adminHeaders()`

Both call the same `/api/auth/login` endpoint and produce identical JWTs. An admin user who logs in via the player flow has their Admin role in the player token, but `AdminGuard` only checks the admin token — forcing a second login.

## Items

### Item 1: Create AuthContext for Centralized Auth State (#114)
**Priority**: P1
**Acceptance Criteria**:
- [ ] `AuthContext` + `AuthProvider` created, wrapping the app in `main.tsx`
- [ ] Provides: `token`, `user`, `roles`, `isAdmin`, `isAuthenticated`, `login`, `register`, `logout`, `refreshToken`
- [ ] `useAuth()` hook for consumers
- [ ] Single source of truth for auth — no more scattered localStorage reads
- [ ] Token stored under one key (`linkittydo_token`)

**Tasks**:
1. Create `src/contexts/AuthContext.tsx` with `AuthProvider` and `useAuth` hook
2. Move token management (store/clear/refresh) from `api.ts` into the context
3. Move login/register/logout logic from `useUser` into AuthContext
4. Wrap `<App />` in `<AuthProvider>` in `main.tsx`

### Item 2: Migrate AdminGuard and adminApi to Use Shared Token (#113)
**Priority**: P1
**Acceptance Criteria**:
- [ ] `AdminGuard` reads from the shared player token (`linkittydo_token`), not `linkittydo_admin_token`
- [ ] `adminApi` uses `authHeaders()` from `api.ts` (or from AuthContext) instead of its own `adminHeaders()`
- [ ] Admin pages work without a second login when user is already authenticated with Admin role
- [ ] 401 responses in admin API redirect to main login, not a separate admin login

**Tasks**:
1. Update `AdminGuard` to use `useAuth()` context or read from shared token
2. Update `adminApi.ts` to import and use shared `getStoredToken()` from `api.ts`
3. Update 401 handlers to redirect to the main login flow
4. Remove `ADMIN_TOKEN_KEY`, `ADMIN_REFRESH_TOKEN_KEY`, `getAdminToken`, `storeAdminTokens`, `clearAdminTokens`, `adminHeaders`

### Item 3: Remove Redundant AdminLogin Page (#115)
**Priority**: P2
**Acceptance Criteria**:
- [ ] `/admin/login` route redirects to the main app with a login prompt (or is removed)
- [ ] If a non-admin authenticated user navigates to `/admin`, they see an "access denied" message
- [ ] If an unauthenticated user navigates to `/admin`, they are prompted to log in (via the main login modal)
- [ ] `AdminLogin.tsx` and `AdminLogin.css` removed

**Tasks**:
1. Update `AdminGuard` to distinguish unauthenticated (no token) vs unauthorized (has token but not admin)
2. For unauthenticated: redirect to `/play` and trigger login modal (or show inline login)
3. For unauthorized (non-admin): show "Access Denied" message
4. Remove `AdminLogin.tsx`, `AdminLogin.css`
5. Update routes in `App.tsx`

### Item 4: Refactor useUser to Consume AuthContext (#114 cont.)
**Priority**: P1
**Acceptance Criteria**:
- [ ] `useUser` no longer manages tokens, login, or register directly
- [ ] `useUser` delegates auth operations to `useAuth()`
- [ ] `useUser` focuses on user profile state (name, points, difficulty, game history)
- [ ] All existing functionality preserved (guest mode, user sync, etc.)

**Tasks**:
1. Split `useUser` — auth concerns move to `AuthContext`, profile concerns stay
2. `useUser` calls `useAuth()` for token/login/logout
3. Update `App.tsx` and any components that use `useUser` for auth
4. Verify NavHeader admin link still works

### Item 5: Update Tests for Unified Auth
**Priority**: P2
**Acceptance Criteria**:
- [ ] Existing NavHeader tests updated for new auth pattern
- [ ] Admin auth flow tested (authenticated admin goes straight through)
- [ ] Non-admin rejection tested
- [ ] All existing tests pass

**Tasks**:
1. Update NavHeader tests for any prop changes
2. Add/update tests for AdminGuard behavior
3. Run full test suite

---

## Definition of Done

- [ ] `npm run build` passes with 0 errors
- [ ] `npx tsc --noEmit` passes
- [ ] `npx eslint src/` passes (or only pre-existing warnings)
- [ ] All existing + new tests pass
- [ ] Admin user logged in as player navigates to `/admin` without re-login
- [ ] Non-admin user sees "Access Denied" on `/admin`
- [ ] Unauthenticated user redirected to login
- [ ] No duplicate token storage in localStorage
- [ ] CHANGELOG.md updated
