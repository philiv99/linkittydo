# Sprint 44 Retrospective — Admin Nav Role Enforcement

**Date**: 2026-04-11
**Branch**: `feature/20260411-sprint-44`
**PR**: #51

---

## Summary

**Goal**: Fix admin navigation persisting after switching to a non-admin user.
**Result**: Complete — all 5 tasks delivered.

## What Was Delivered

| # | Task | Status |
|---|------|--------|
| 1 | Fix switchUser to clear auth state | Done |
| 2 | AdminGuard redirects non-admin to /play | Done |
| 3 | Verify NavHeader hides admin link | Done (verified via existing tests) |
| 4 | Add frontend tests (3 AdminGuard tests) | Done |
| 5 | Verify builds pass | Done — 329 backend, 64 frontend |

## Metrics

- Backend tests: 329 (unchanged)
- Frontend tests: 64 (was 61, +3 new)
- Build: Clean on both frontend and backend

## Root Cause Analysis

The `switchUser` function in `useUser.ts` only called `api.getUser()` to fetch user profile data but did not clear the JWT token from AuthContext. Since `isAdmin` is derived from JWT role claims, not from the user profile, switching users without re-authenticating left the previous admin JWT active.

## Lesson Learned

- **L14** (Sprint 44): When a function changes the "current user" identity, it must also clear/reset authentication state. Profile data and auth tokens are separate concerns — changing one without the other creates privilege persistence bugs.

## Improvements Applied

- Updated AdminGuard to redirect instead of showing static "Access Denied" — better UX
- Added copilot-instructions lesson L14
