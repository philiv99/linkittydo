# Sprint 10 Plan — JWT Authentication

**Sprint Goal**: Add JWT token authentication to protect user-specific API endpoints while keeping game and public endpoints accessible without auth.

**Branch**: `feature/20260409-sprint-10`
**Date**: 2026-04-09

## Items

| # | Item | Priority |
|---|------|----------|
| 22 | User authentication (JWT) | P1 |

## Tasks

1. Add `Microsoft.AspNetCore.Authentication.JwtBearer` and `BCrypt.Net-Next` NuGet packages
2. Create `AuthController` with register (with password), login, and refresh token endpoints
3. Add `PasswordHash` field to User model (not exposed in responses)
4. Create `IAuthService` / `AuthService` for token generation, validation, password hashing
5. Configure JWT authentication in `Program.cs` with secure defaults
6. Add `[Authorize]` to user-specific endpoints (update, delete, difficulty, points, games)
7. Keep public endpoints open: game start, leaderboard, check-name, check-email, health
8. Update frontend `api.ts` to include Authorization header from stored token
9. Update `useUser.ts` hook to handle login/register with password, token storage
10. Update `UserModal.tsx` for login/register with password field
11. Add backend auth tests
12. Add frontend auth tests

## Acceptance Criteria

- Users can register with name + email + password
- Users can login with email + password and receive JWT + refresh token
- Protected endpoints return 401 without valid token
- Public endpoints remain accessible without auth
- Frontend stores token in localStorage and sends with API calls
- All existing tests still pass
- New auth tests pass
