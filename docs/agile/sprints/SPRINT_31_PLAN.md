# Sprint 31 Plan: Admin Quality & Completeness

**Sprint Goal**: Improve admin robustness with consistent error handling, proper route guards, graceful empty states, and clue effectiveness analytics.

**Date**: 2026-04-10
**Branch**: `feature/20260410-sprint-31`
**Backlog Items**: #108, #109, #110, #111

---

## Items

### Item 1: Consistent adminApi Error Handling (#108)
**Priority**: P2
**Acceptance Criteria**:
- [ ] All adminApi methods use the shared `handleResponse<T>()` helper
- [ ] No duplicated 401 handling logic
- [ ] All endpoints handle errors consistently (401 → redirect, other errors → throw)

**Tasks**:
1. Refactor `getUsers()`, `getGames()`, `getPhrases()` to use `handleResponse<T>`
2. Remove inline 401 checks
3. Verify all admin pages still work correctly after refactor

### Item 2: AdminGuard Verifies Admin Role (#109)
**Priority**: P2
**Acceptance Criteria**:
- [ ] AdminGuard checks that the stored JWT contains an admin role (or verifies with server)
- [ ] Non-admin users with a token are redirected to login
- [ ] Admin users pass through normally

**Tasks**:
1. Decode JWT payload in AdminGuard to check for admin role claim
2. OR add a lightweight `/api/auth/verify` endpoint and call it in AdminGuard
3. Handle expired tokens gracefully (redirect to login)
4. Add frontend test for AdminGuard behavior

### Item 3: Graceful Empty PlayerStats (#110)
**Priority**: P2
**Acceptance Criteria**:
- [ ] When PlayerStats row doesn't exist, admin shows "No games played yet" instead of empty/error
- [ ] Backend returns a zero-initialized PlayerStats object instead of null (or frontend handles null)

**Tasks**:
1. Option A: Update `AdminService.GetPlayerAnalyticsAsync()` to return a default PlayerStats when null
2. Option B: Update `AdminUsers.tsx` to show a message when analytics is null
3. Choose the approach that requires fewer changes (likely Option B for now)

### Item 4: ClueEffectiveness Recomputation (#111)
**Priority**: P3
**Acceptance Criteria**:
- [ ] `RecomputeClueEffectivenessAsync()` method is implemented in AnalyticsService
- [ ] Aggregates ClueEvents with subsequent GuessEvents to compute effectiveness metrics
- [ ] Called after game completion (alongside PlayerStats and PhrasePlayStats recomputation)
- [ ] ClueEffectiveness table is populated with data

**Tasks**:
1. Implement `RecomputeClueEffectivenessAsync(string gameId)` in AnalyticsService
2. For each ClueEvent, check if the next GuessEvent for the same word was correct
3. Upsert into ClueEffectiveness table: increment TimesShown, TimesLedToCorrectGuess
4. Call from PersistGameRecordAsync() in GameService
5. Add unit test

---

## Risks

1. **JWT decoding in frontend**: Need a dependency or base64 decode to read JWT claims. Must handle malformed tokens.
2. **ClueEffectiveness logic**: Linking clues to subsequent guesses requires careful event sequence analysis.

---

## Definition of Done

- All acceptance criteria met
- Backend and frontend build cleanly
- All existing and new tests pass
- CHANGELOG.md updated
