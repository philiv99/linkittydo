# Sprint 40 Plan

**Goal**: Persist game data from the moment a game starts, with incremental event saving and abandoned game tracking
**Duration**: 2026-04-11
**Estimated effort**: 8 hours
**Depends on**: Sprint 39 (reliable persistence)

## Sprint Context

Sprint 39 made game completion persistence reliable. This sprint moves persistence earlier in the game lifecycle: save GameRecord to DB at game start, persist each event as it happens, and track abandoned games that expire without completion.

## Selected Items

### 1. Persist GameRecord to DB at game start (#121)
- **Source**: Backlog #121
- **Tasks**:
  - A. In `StartNewGameAsync`, save GameRecord to DB immediately after creation (for registered users) (~1h)
  - B. Set `Result = InProgress` in the DB record (~0.5h)
  - C. Update `PersistGameRecordAsync` to use `UpdateAsync` instead of `CreateAsync` (record already exists) (~1h)
  - D. Handle edge case: what if DB save fails at start — return error to frontend, don't create session (~0.5h)
  - E. Update existing tests for the new flow (~1h)
- **Acceptance Criteria**:
  - [ ] GameRecord row appears in DB immediately when game starts
  - [ ] `Result` column shows `InProgress` for active games
  - [ ] Game completion updates (not creates) the existing record
  - [ ] If initial DB save fails, game start returns error
- **Risk**: Medium — changes the GameRecord lifecycle from create-on-end to create-on-start/update-on-end

### 2. Persist game events incrementally (#122)
- **Source**: Backlog #122
- **Tasks**:
  - A. In `RecordClueEvent`, persist the ClueEvent to DB immediately after adding to in-memory list (~1h)
  - B. In `SubmitGuess`, persist the GuessEvent to DB immediately after adding to in-memory list (~1h)
  - C. In `SubmitGuess`/`GiveUp`, persist GameEndEvent to DB (already happens via batch — change to targeted insert) (~0.5h)
  - D. Remove batch event insertion from `PersistGameRecordAsync` (events already saved individually) (~0.5h)
  - E. Handle event persistence failure gracefully — log error but don't break the game session (~0.5h)
  - F. Update persistence tests for incremental save pattern (~1h)
- **Acceptance Criteria**:
  - [ ] Each clue request creates a GameEvents DB row immediately
  - [ ] Each guess creates a GameEvents DB row immediately
  - [ ] GameEndEvent persisted at game end
  - [ ] Events in DB match events in in-memory session
  - [ ] Event persistence failure logged but doesn't crash the game
- **Risk**: Medium — more DB writes per game; need to ensure performance is acceptable

### 3. Track abandoned/expired games in DB (#123)
- **Source**: Backlog #123
- **Tasks**:
  - A. Add `Abandoned` value to `GameResult` enum (~0.5h)
  - B. Create EF Core migration for enum expansion (if stored as string, no migration needed) (~0.5h)
  - C. Update `SessionCleanupService.RemoveExpiredSessions` to persist abandoned game records before removing from memory (~1.5h)
  - D. For sessions with a GameRecord (registered users): set `Result = Abandoned`, `CompletedAt = now`, update in DB (~0.5h)
  - E. For sessions without an InProgress DB record (edge case): create the abandoned record (~0.5h)
  - F. Add tests for abandoned game tracking (~1h)
- **Acceptance Criteria**:
  - [ ] `GameResult` enum includes `Abandoned`
  - [ ] Expired sessions with game records get `Result = Abandoned` in DB
  - [ ] Abandoned games appear in user's game history
  - [ ] Session cleanup log includes count of abandoned games
  - [ ] Guest sessions still cleaned up without persistence
- **Risk**: Low — Session cleanup service is already well-structured

## Test Expectations

| Area | Expected New Tests |
|------|--------------------|
| GameService start | 2 tests: record-persisted-at-start, start-fails-on-db-error |
| GameService events | 3 tests: clue-event-persisted-immediately, guess-event-persisted-immediately, end-event-persisted |
| SessionCleanupService | 2 tests: abandoned-game-persisted, guest-session-no-persistence |
| GameResult enum | 1 test: abandoned-value-serialization |

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| More DB writes per game impacts performance | Medium | Individual event inserts are small; index on (GameId, SequenceNumber) already exists |
| RecordClueEvent is synchronous — adding DB write needs async | Medium | Change to async; update IGameService interface and callers |
| Abandoned tracking could persist incomplete data | Low | Only persist if GameRecord exists; validate required fields |

## Definition of Done

- [ ] All 3 backlog items implemented and working
- [ ] At least 8 new backend tests (all passing)
- [ ] `dotnet build` succeeds with 0 errors
- [ ] `dotnet test` — all tests pass
- [ ] `npm run build` succeeds with 0 errors
- [ ] Playing a complete game creates DB records visible at each step
- [ ] Abandoned games tracked in DB with correct status
- [ ] Sprint branch created, committed, PR merged to main
