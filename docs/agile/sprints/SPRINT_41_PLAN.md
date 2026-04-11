# Sprint 41 Plan

**Goal**: Complete game data pipeline with frontend integration, game detail API, and session recovery
**Duration**: 2026-04-11
**Estimated effort**: 7 hours
**Depends on**: Sprint 40 (incremental persistence)

## Sprint Context

Sprints 39-40 established reliable, transactional, incremental game persistence. This sprint closes the loop: frontend gets save-status feedback, a new API endpoint serves complete game history with events, and the unused GameSessions table becomes a session recovery mechanism.

## Selected Items

### 1. Add persistence failure response to frontend (#125)
- **Source**: Backlog #125
- **Tasks**:
  - A. Add `PersistenceStatus` enum to models: `Saved`, `Failed`, `NotApplicable` (~0.5h)
  - B. Add `persistenceStatus` field to `GuessResponse` and game completion response DTOs (~0.5h)
  - C. Set `persistenceStatus` in `SubmitGuess` and `GiveUp` based on persistence result (~0.5h)
  - D. Update frontend `GuessResponse` TypeScript type to include `persistenceStatus` (~0.5h)
  - E. Show warning toast in GameBoard when `persistenceStatus === 'Failed'` (~1h)
  - F. Add tests for persistence status propagation (~0.5h)
- **Acceptance Criteria**:
  - [ ] Completed game response includes `persistenceStatus` field
  - [ ] Frontend displays warning when game save fails
  - [ ] Guest sessions return `NotApplicable`
  - [ ] Successful saves return `Saved`
- **Risk**: Low — additive change to existing DTOs

### 2. Game history API endpoint with events (#126)
- **Source**: Backlog #126
- **Tasks**:
  - A. Add `GET /api/game/{gameId}/detail` endpoint to GameController (~1h)
  - B. Use `GetByGameIdWithEventsAsync` from Sprint 39 (#120) to load full record (~0.5h)
  - C. Return response with GameRecord + ordered Events list (~0.5h)
  - D. Update frontend `api.ts` with `getGameDetail(gameId)` method (~0.5h)
  - E. Update game history UI to link to game detail and show event timeline (~1h)
  - F. Add controller test for the new endpoint (~0.5h)
- **Acceptance Criteria**:
  - [ ] `GET /api/game/{gameId}/detail` returns record with events
  - [ ] Events ordered by SequenceNumber
  - [ ] 404 if gameId not found
  - [ ] Frontend game history page can drill into individual games
- **Risk**: Low — builds on Sprint 39 repository method

### 3. Populate GameSessions table for session recovery (#127)
- **Source**: Backlog #127
- **Tasks**:
  - A. Create `IGameSessionRepository` interface with `SaveAsync`, `GetAsync`, `DeleteAsync`, `GetActiveAsync` (~0.5h)
  - B. Implement `EfGameSessionRepository` using existing `GameSessions` DbSet (~1h)
  - C. Serialize session state to `StateJson` column (RevealedWords, ClueCountPerWord, GuessCountPerWord, UsedClueTerms) (~0.5h)
  - D. Write session state on each game state change (start, clue, guess) — debounce or throttle to avoid excessive writes (~1h)
  - E. On application startup, reload active sessions from `GameSessions` table into `InMemorySessionStore` (~1h)
  - F. On session removal (completion/expiry), delete from `GameSessions` table (~0.5h)
  - G. Add tests for session save/restore round-trip (~1h)
- **Acceptance Criteria**:
  - [ ] Active game sessions written to `GameSessions` table
  - [ ] On server restart, active sessions recovered from DB
  - [ ] Session state (revealed words, scores, counters) fully restored
  - [ ] Completed/expired sessions cleaned from GameSessions table
  - [ ] Guest sessions also persisted (for session recovery, not game history)
- **Risk**: High — serialization/deserialization of complex state; must handle schema mismatches gracefully

## Test Expectations

| Area | Expected New Tests |
|------|--------------------|
| Persistence status | 2 tests: status-saved, status-failed |
| Game detail endpoint | 2 tests: returns-with-events, not-found |
| GameSessionRepository | 3 tests: save-restore-roundtrip, delete-on-completion, startup-recovery |

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Session JSON serialization breaks on model changes | High | Use versioned schema; handle deserialization errors gracefully (discard corrupt sessions) |
| DB writes on every game action impacts latency | Medium | Use fire-and-forget for session table writes (non-critical); only GameRecord/Events are transactional |
| Startup recovery with stale sessions | Low | Apply session TTL filter when loading; skip sessions older than max age |

## Definition of Done

- [ ] All 3 backlog items implemented and working
- [ ] At least 7 new tests (all passing)
- [ ] `dotnet build` succeeds with 0 errors
- [ ] `dotnet test` — all tests pass
- [ ] `npm run build` succeeds with 0 errors
- [ ] Full game lifecycle persisted: start → events → completion → history viewable
- [ ] Server restart recovers active sessions
- [ ] Sprint branch created, committed, PR merged to main
