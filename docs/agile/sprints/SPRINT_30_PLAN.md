# Sprint 30 Plan: Admin Critical Data Fixes

**Sprint Goal**: Fix critical data persistence defects that cause admin UI to display incorrect/empty data, and fix the admin navigation link disappearing for admin users.

**Date**: 2026-04-10
**Branch**: `feature/20260410-sprint-30`
**Backlog Items**: #103, #104, #105, #106, #107, #112, #113

---

## Items

### Item 1: Fix GameEvent Persistence (#103)
**Priority**: P1 — Critical Defect
**Acceptance Criteria**:
- [ ] After a game completes (solved or gave up), all GameEvents (ClueEvent, GuessEvent, GameEndEvent) are persisted to the `GameEvents` table
- [ ] Events have correct Discriminator values (`clue`, `guess`, `gameend`)
- [ ] Events preserve SequenceNumber ordering
- [ ] Existing backend tests continue to pass
- [ ] New test verifies events are saved to the database

**Tasks**:
1. Modify `EfGameRecordRepository.CreateAsync()` to also persist events from `record.Events` using `_context.GameEvents.AddRange() `
2. Alternatively, create a dedicated `PersistGameEventsAsync()` method called from `PersistGameRecordAsync()` in GameService
3. Add unit test to verify event persistence
4. Remove `entity.Ignore(e => e.Events)` from DbContext or keep it but handle events explicitly

### Item 2: Recompute PlayerStats After Game Completion (#104)
**Priority**: P1 — Defect
**Acceptance Criteria**:
- [ ] After a game completes, `PlayerStats` row for the user is created/updated
- [ ] GamesPlayed, GamesSolved, AvgScore, CurrentStreak, BestScore, BestStreak are all correct
- [ ] Admin Users > View Analytics shows accurate data
- [ ] New test verifies stats are recomputed

**Tasks**:
1. Inject `IAnalyticsService` into `GameService`
2. Call `RecomputePlayerStatsAsync(userId)` in `PersistGameRecordAsync()` after successful game record save
3. Add unit test

### Item 3: Recompute PhrasePlayStats After Game Completion (#105)
**Priority**: P1 — Defect
**Acceptance Criteria**:
- [ ] After a game completes, `PhrasePlayStats` for the played phrase is created/updated
- [ ] Stats reflect correct solve rate, avg time, and calibrated difficulty
- [ ] Admin Phrases > Stats shows accurate data

**Tasks**:
1. Call `RecomputePhrasePlayStatsAsync()` in `PersistGameRecordAsync()`
2. Fix the query parameter issue (#113) — must pass the correct phrase identifier

### Item 4: Fix PhrasePlayStats Query Using Wrong Field (#113)
**Priority**: P2 — Defect
**Acceptance Criteria**:
- [ ] `RecomputePhrasePlayStatsAsync()` correctly matches GameRecords to the phrase
- [ ] GameRecord stores a PhraseUniqueId that can be queried against
- [ ] PhrasePlayStats are actually populated after game completion

**Tasks**:
1. Check if GameRecord has a PhraseUniqueId field or only PhraseText
2. If missing, add PhraseUniqueId to GameRecord model and EF config, and populate in GameService.StartNewGameAsync()
3. Fix the query in `RecomputePhrasePlayStatsAsync()` to use the correct field
4. Add EF migration if schema change needed

### Item 5: Fix Admin Nav Link Disappearing (#106, #107)
**Priority**: P1 — Defect
**Acceptance Criteria**:
- [ ] After page refresh, admin users still see the "Admin" link in NavHeader
- [ ] `mapResponseToUser()` preserves roles
- [ ] Backend `UserResponse` includes roles
- [ ] `GET /api/user/{id}` returns roles in the response

**Tasks**:
1. Add `List<string> Roles` property to `UserResponse` model
2. Update `UserService` methods that build UserResponse to query and populate roles
3. Update frontend `mapResponseToUser()` to include `roles: response.roles ?? []`
4. Update syncUser() effect to preserve roles from server data
5. Verify admin nav link persists through page refresh

### Item 6: Verify Admin Games Event Display (#112)
**Priority**: P2 — Downstream of #103
**Acceptance Criteria**:
- [ ] After #103 is fixed, admin Games > Detail shows correct event timeline
- [ ] Clue, Guess, and GameEnd events render with correct fields
- [ ] Event count matches actual events

**Tasks**:
1. After fixing #103, play a game end-to-end and verify events appear in admin
2. Check AdminGames.tsx event rendering for correctness
3. Fix any frontend rendering issues found

---

## Risks

1. **EF Migration needed**: Adding PhraseUniqueId to GameRecord requires a migration. Must be backward-compatible with existing data.
2. **Performance**: Calling 3 recompute methods after each game completion adds latency to the game-end response. Consider running them fire-and-forget or on a background thread.
3. **Existing tests**: Changes to GameService and repositories may break existing test mocks. Must update accordingly.

---

## Definition of Done

- All acceptance criteria met
- Backend builds with 0 errors
- Frontend builds with 0 errors (`npm run build`)
- All existing tests pass
- New tests added for event persistence, stats recomputation, and roles in UserResponse
- CHANGELOG.md updated
