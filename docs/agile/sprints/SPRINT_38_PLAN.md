# Sprint 38 Plan — Admin Auth & Session Management

**Date**: 2026-04-11
**Branch**: `feature/20260411-sprint-38`
**Theme**: Fix admin menu visibility and auth session resilience

---

## Goal

Fix the bug where admin users do not see the Admin menu link after JWT token expiry. Implement automatic token refresh, ensure all User API responses include roles consistently, and update backlog status markers for completed items.

---

## Selected Items

| # | Item | Priority | Type |
|---|------|----------|------|
| 116 | Admin menu not visible — implement token auto-refresh in AuthContext | P1 | Bug Fix |
| 117 | Roles stripped from user state after profile operations | P1 | Bug Fix |
| — | Mark backlog items #93-#115 as Done per sprint history | P2 | Housekeeping |

---

## Tasks

### Task 1: Implement token auto-refresh in AuthContext (#116)

**Files**: `src/linkittydo-web/src/contexts/AuthContext.tsx`

1. On initialization: when stored token is expired AND refresh token exists, call `api.refreshToken()`
2. On interval check: when token is near/at expiry, attempt refresh before clearing
3. On refresh failure: force complete sign-out (clear tokens + reset user to guest via callback)
4. Update `authUser` and `roles` from the refreshed token
5. Add `onAuthLost` callback prop to AuthProvider for useUser to hook into

**Test Expectations**: Add test for AuthContext token refresh behavior

### Task 2: Fix roles in all User API responses (#117)

**Files**: `src/LinkittyDo.Api/Controllers/UserController.cs`

1. Change `UpdateUser` to use `MapToResponseWithRolesAsync()`
2. Change `GetAllUsers` to include roles (or use batch role query)
3. Verify `AddPoints` and `UpdateDifficulty` responses — these return `PointsResponse`/`DifficultyResponse` (not `UserResponse`), so roles are N/A for those
4. Verify frontend `updateUser` callback correctly preserves roles from server response

**Test Expectations**: Add/update unit test for UpdateUser verifying roles are returned

### Task 3: Mark completed backlog items

**Files**: `docs/agile/BACKLOG.md`

1. Mark items #93-#102 as Done (Sprint 28)
2. Mark items #103-#107, #112, #113 as Done (Sprint 30)
3. Mark items #108-#111 as Done (Sprint 31)

---

## Acceptance Criteria

- [ ] Admin user sees Admin menu link after login AND after page refresh
- [ ] Admin menu persists across JWT token expiry (auto-refresh works)
- [ ] When both access + refresh tokens are invalid, user is fully signed out
- [ ] `UpdateUser` API response includes roles
- [ ] Frontend build passes (`npm run build`)
- [ ] Backend build passes (`dotnet build`)
- [ ] All existing tests pass
- [ ] New tests for token refresh behavior
