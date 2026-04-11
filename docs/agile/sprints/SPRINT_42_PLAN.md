# Sprint 42 Plan

**Goal**: Leaderboard shows only real player data from the backend database — no simulated users, no N+1 queries
**Duration**: 2026-04-11
**Estimated effort**: 4 hours
**Depends on**: Sprint 41 (game persistence pipeline complete)

## Sprint Context

The leaderboard page exists (Sprint 7) but has data quality issues: it includes simulated users (SIM-prefix), uses inefficient N+1 queries for game counts, and shows only basic columns (rank, name, points, games). The `PlayerStats` computed table (Sprint 18) has richer data that should power the leaderboard. This sprint fixes the backend to serve only real player data and enriches the frontend display.

## Selected Items

### 1. Fix leaderboard to exclude simulated users and use PlayerStats (#129)
- **Source**: Backlog #129
- **Tasks**:
  - A. Update `UserService.GetLeaderboardAsync()` to filter out `IsSimulated == true` users (~0.5h)
  - B. Add `GetLeaderboardWithStatsAsync()` method that joins Users with PlayerStats to return enriched data in a single query (~1h)
  - C. Update `LeaderboardEntry` model to include GamesSolved, BestScore, CurrentStreak from PlayerStats (~0.5h)
  - D. Update `UserController.GetLeaderboard()` to use the new method, eliminating N+1 `GetGameCountAsync` calls (~0.5h)
  - E. Update frontend `LeaderboardEntry` type and `LeaderboardPage` to display the new columns (~1h)
  - F. Add/update backend tests for leaderboard filtering and stats join (~1h)
- **Acceptance Criteria**:
  - [ ] Leaderboard endpoint returns ZERO simulated users (no SIM- prefix IDs)
  - [ ] Leaderboard data comes from Users + PlayerStats join (single query, no N+1)
  - [ ] LeaderboardEntry includes: Rank, Name, LifetimePoints, GamesPlayed, GamesSolved, BestScore, CurrentStreak
  - [ ] Frontend displays all columns with proper formatting
  - [ ] Users with no PlayerStats row show 0 for all stats fields
  - [ ] Existing tests updated, new tests added for simulated user exclusion
- **Risk**: Low — additive changes to existing endpoint, no breaking API changes

## Test Expectations

| Area | Expected New/Updated Tests |
|------|----------------------------|
| Leaderboard filtering | 1 test: simulated users excluded from results |
| Leaderboard stats join | 1 test: stats populated from PlayerStats table |
| Leaderboard no stats | 1 test: users without PlayerStats show zero defaults |
| Controller endpoint | Update existing test to verify new fields |

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| PlayerStats rows missing for some users | Low | Default to 0 for all stats fields when no PlayerStats row exists |
| Frontend type changes break existing tests | Low | Update test mocks to include new fields |

## Definition of Done

- [ ] All acceptance criteria met
- [ ] Backend builds with 0 errors
- [ ] Frontend builds with 0 errors (`npm run build`)
- [ ] All backend tests pass (320+ existing + new)
- [ ] All frontend tests pass (61+ existing + updated)
- [ ] CHANGELOG.md updated
- [ ] PR created and merged to main
