# Sprint 8 Plan — Security & Production Readiness

**Sprint**: 8
**Date**: 2026-04-09
**Branch**: feature/20260409-sprint-8
**Goal**: Harden API with input sanitization and rate limiting

## Items

| # | Item | Issue | Priority |
|---|------|-------|----------|
| 23 | Input sanitization audit | #23 | P2 |
| 24 | Rate limiting | #24 | P2 |

**Note**: Item #22 (JWT Auth) deferred — requires full auth flow design (login/register with passwords, refresh tokens), which is a larger scope item best handled in a dedicated sprint.

## Acceptance Criteria

### Input Sanitization (#23)
- Add FluentValidation or manual validation middleware for all API inputs
- Validate guess text (max length, alphanumeric + spaces only)
- Validate user names (regex, length 2-50, allowed characters)
- Validate email format at API boundary
- Strip/reject HTML/script injection attempts

### Rate Limiting (#24)
- Add ASP.NET Core rate limiting middleware
- Rate limit clue requests: 30 per minute per session
- Rate limit game start: 10 per minute per IP
- Rate limit user endpoints: 60 per minute per IP
- Return 429 Too Many Requests with retry-after header
