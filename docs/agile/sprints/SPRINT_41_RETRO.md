# Sprint 41 Retrospective — Game Data Completeness & Frontend Integration

**Date**: 2026-04-12
**Branch**: `feature/20260412-sprint-41`
**Items**: #125, #126, #127

## What Was Delivered

### #125 — Persistence Status on Frontend
- Added `PersistenceStatus` enum (Saved, Failed, NotApplicable) to backend models
- Updated `GuessResponse` and `GameState` DTOs with status field
- `SubmitGuessAsync` and `GiveUpAsync` now catch completion errors and return `PersistenceStatus.Failed` instead of throwing
- Frontend shows a 5-second auto-dismissing toast warning when persistence fails

### #126 — Game Detail Frontend with Lazy-Loaded Events
- Added `getGameDetail(gameId)` API method using existing `GET /api/game/detail/{gameId}` endpoint
- GameHistoryPage now lazy-loads events on expand (click to reveal timeline)
- Added loading spinner and empty-state display for event details
- Displays "Abandoned" result status for expired sessions

### #127 — Session Recovery via DatabaseSessionStore
- Full rewrite of `DatabaseSessionStore.cs`:
  - Fixed PhraseUniqueId bug (was storing FullText, now stores UniqueId)
  - Added `GameRecordId` to `GameSessionRecord` for cross-referencing
  - Stores PhraseWords, PhraseFullText, PhraseId in StateJson for self-contained recovery
  - `LoadSessionsAsync(TimeSpan maxAge)` reconstructs sessions from DB on startup
- Added `_sessionStore.Set()` calls after guess and clue events (not just game start)
- Program.cs calls `LoadSessionsAsync` on startup after migrations

## Test Results
- **Backend**: 320 tests passing (9 new: 4 persistence status + 2 game detail controller + 3 session recovery)
- **Frontend**: 61 tests passing (updated mocks for new DTO fields and lazy-load behavior)
- **Build**: Both backend and frontend compile cleanly

## Issues Encountered

### 1. Stale Session Cleanup Ordering (Bug)
- `LoadSessionsAsync` queried active sessions first, then cleaned stale ones
- When only stale sessions existed, the active query returned 0 → early return → stale cleanup was skipped
- **Fix**: Moved stale cleanup before active session query
- **Lesson**: When a method has both cleanup and load responsibilities, cleanup should always run first unconditionally

### 2. EF Core DbContext Disposal in Integration Tests
- Session recovery tests shared a single `LinkittyDoDbContext` between seed code and the `DatabaseSessionStore`
- The store creates a DI scope internally; when that scope disposed, the shared context was also disposed
- Subsequent assertions in the test hit `ObjectDisposedException`
- **Fix**: Changed to `DbContextOptions<>` factory pattern — seed and verify phases each create their own context
- **Lesson**: When testing Singleton services that create DI scopes internally, never share DbContext instances; use options factory pattern

### 3. Frontend Test Mock Missing New Field
- `GameBoard.test.tsx` mock `defaultGameState` was missing the new `persistenceStatus` field
- `GameHistoryPage.test.tsx` expand test wasn't mocking the new `getGameDetail` API call
- **Fix**: Added `persistenceStatus` to mock state; added `getGameDetail` mock with `waitFor` for async load

## Lessons Learned

| ID | Lesson | Priority |
|----|--------|----------|
| L14 | When a method has both cleanup and load responsibilities, always run cleanup unconditionally before the load path to avoid early returns skipping cleanup | High |
| L15 | Integration tests for Singleton services that create DI scopes internally must use DbContextOptions factory pattern, not shared DbContext instances | Medium |

## Process Metrics
- Items planned: 3
- Items completed: 3
- Carry-over: 0
- Backend tests added: 9 (311 → 320)
- Frontend tests: 61 (stable, updated mocks)
- Files modified: 20 (12 backend, 8 frontend)
