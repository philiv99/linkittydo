# Sprint 39 Plan

**Goal**: Make game completion persistence reliable — awaited, transactional, and fully tested
**Duration**: 2026-04-11
**Estimated effort**: 8 hours

## Sprint Context

Game data persistence has critical gaps where completed games can silently fail to save. The `PersistGameRecordAsync` method is called fire-and-forget, GameRecord and GameEvents are saved non-atomically, and game records read from DB lack events. This sprint fixes the core persistence reliability before Sprint 40 adds incremental persistence.

## Selected Items

### 1. Make GameService methods async end-to-end (#124)
- **Source**: Backlog #124
- **Tasks**:
  - A. Change `SubmitGuess` signature from `GuessResponse` to `Task<GuessResponse>` (~1h)
  - B. Change `GiveUp` signature from `GameState` to `Task<GameState>` (~0.5h)
  - C. Update `IGameService` interface with async signatures (~0.5h)
  - D. Update `GameController` to await new async methods (~0.5h)
  - E. Update all test mocks and existing tests for new signatures (~1h)
- **Acceptance Criteria**:
  - [ ] `SubmitGuess` and `GiveUp` return Task-wrapped types
  - [ ] Controller properly awaits both methods
  - [ ] All existing tests pass with updated signatures
- **Risk**: Low — signature change is mechanical

### 2. Await game record persistence / fix fire-and-forget (#118)
- **Source**: Backlog #118
- **Tasks**:
  - A. Remove `_ = PersistGameRecordAsync(session)` pattern from `SubmitGuess` (~0.5h)
  - B. Remove `_ = PersistGameRecordAsync(session)` pattern from `GiveUp` (~0.5h)
  - C. Await `PersistGameRecordAsync` directly in both methods (~0.5h)
  - D. Propagate persistence exceptions to callers (log and return error status) (~0.5h)
- **Acceptance Criteria**:
  - [ ] No fire-and-forget calls to `PersistGameRecordAsync` remain
  - [ ] Persistence errors propagate to controller, returning 500 if save fails
  - [ ] Successful persistence still returns normal response
- **Risk**: Low — depends on #124 completing first

### 3. Wrap GameRecord + GameEvents in UnitOfWork transaction (#119)
- **Source**: Backlog #119
- **Tasks**:
  - A. Inject `IUnitOfWork` into `GameService` (~0.5h)
  - B. Wrap GameRecord create + GameEvents add in `BeginTransactionAsync`/`CommitTransactionAsync` (~1h)
  - C. Add `RollbackTransactionAsync` in catch block for partial failure (~0.5h)
  - D. Move analytics recompute outside the transaction (analytics failure should not roll back game data) (~0.5h)
- **Acceptance Criteria**:
  - [ ] GameRecord and GameEvents saved atomically within a transaction
  - [ ] Partial failure rolls back both GameRecord and GameEvents
  - [ ] Analytics recompute failure does not affect game data save
- **Risk**: Medium — need to verify EfUnitOfWork transaction behavior with existing DbContext usage

### 4. Load GameEvents when reading GameRecords from DB (#120)
- **Source**: Backlog #120
- **Tasks**:
  - A. Add `GetByGameIdWithEventsAsync` method to `IGameRecordRepository` (~0.5h)
  - B. Implement in `EfGameRecordRepository` using explicit `_context.GameEvents.Where(e => e.GameId == gameId)` query (~0.5h)
  - C. Add `GetByUserIdWithEventsAsync` overload for paginated history with events (~0.5h)
  - D. Update `GamesManagerService.GetGameEventsAsync` to use the new method (if applicable) (~0.5h)
  - E. Update admin game detail endpoint to return events with game records (~0.5h)
- **Acceptance Criteria**:
  - [ ] `GetByGameIdWithEventsAsync` returns GameRecord with populated Events list
  - [ ] Events are ordered by SequenceNumber
  - [ ] Admin game detail shows actual events from DB
  - [ ] Existing read methods (without events) remain unchanged for performance
- **Risk**: Low — straightforward query addition

### 5. Backend tests for game persistence paths (#128)
- **Source**: Backlog #128
- **Tasks**:
  - A. Test: successful persist saves GameRecord + all Events + updates points (~1h)
  - B. Test: transaction rolls back cleanly on GameEvents failure (~0.5h)
  - C. Test: guest session persistence is correctly skipped (~0.5h)
  - D. Test: analytics recompute failure doesn't prevent game save (~0.5h)
  - E. Test: awaited persistence propagates errors to caller (~0.5h)
  - F. Test: events loaded correctly from DB match original in-memory events (~0.5h)
- **Acceptance Criteria**:
  - [ ] Minimum 6 new tests covering all persistence code paths
  - [ ] All tests pass on both `dotnet test` and `npm run build`
  - [ ] Tests use mock repositories or in-memory DB
- **Risk**: Low

## Test Expectations

| Area | Expected New Tests |
|------|--------------------|
| GameService persistence | 4 tests: persist-with-events, rollback-on-failure, guest-skip, analytics-isolation |
| GameService async methods | 2 tests: awaited-success, awaited-error-propagation |
| EfGameRecordRepository | 2 tests: load-with-events, events-ordered-by-sequence |

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Changing SubmitGuess/GiveUp signatures may break tests | Medium | Update all mocks and test callsites systematically |
| UnitOfWork transaction may double-save via repository | Medium | Verify EfUnitOfWork doesn't auto-save; disable repo-level SaveChanges within transaction scope |
| DbContext lifetime conflicts between repos and UoW | Low | Both use the same scoped DbContext instance |

## Definition of Done

- [ ] All 5 backlog items implemented and working
- [ ] At least 8 new backend tests (all passing)
- [ ] `dotnet build` succeeds with 0 errors
- [ ] `dotnet test` — all tests pass
- [ ] `npm run build` succeeds with 0 errors
- [ ] No fire-and-forget persistence calls remain in codebase
- [ ] Sprint branch created, committed, PR merged to main
