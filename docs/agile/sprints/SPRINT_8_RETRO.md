# Sprint 8 Retrospective

## Sprint Summary
- **Sprint**: 8 — Security & Production Readiness
- **Branch**: `feature/20260409-sprint-8`
- **Duration**: Single session (part of 5-sprint pipeline)
- **Status**: Complete

## Delivered vs Planned

| Item | Planned | Delivered |
|------|---------|-----------|
| #23 Input Sanitization | DataAnnotation validation on all request models | Done — GuessRequest (regex, length, range), StartGameRequest (regex, range) |
| #24 Rate Limiting | ASP.NET Core rate limiting middleware | Done — 4 policies: game-start, clue, user, global |
| #22 JWT Auth | Deferred (too large for sprint) | Deferred — remains in backlog |

## What Went Well
- ASP.NET Core 8 built-in rate limiting required no additional packages
- `[ApiController]` attribute provides automatic ModelState validation — no manual checks needed
- DataAnnotation approach is clean and testable in isolation (no HTTP integration tests needed)
- Input validation tests cover XSS injection patterns, SQL injection attempts, and boundary conditions

## Test Metrics
- **Backend**: 157 tests (was 136, +21 new validation tests)
- **Frontend**: 55 tests (unchanged)
- **Total**: 212 tests, all passing

## Key Decisions
- Used fixed-window rate limiting (simplest, appropriate for current scale)
- Applied rate limits per-IP via `RemoteIpAddress` (sufficient without auth)
- Set policy-specific limits on controllers via `[EnableRateLimiting]` attribute
- Global fallback of 100 req/min/IP catches any untagged endpoints

## Process Notes
- Sprint 8 completes the 5-sprint pipeline (Sprints 4-8) requested by user
- All PRs auto-merged without approval gates per user instruction
