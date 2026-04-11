# Sprint 42 Retrospective

**Date**: 2026-04-11
**Goal**: Leaderboard shows only real player data from the backend database
**Result**: Complete — all acceptance criteria met
**PR**: #49 (merged to main)

## Delivery Summary

| Planned | Delivered | Status |
|---------|-----------|--------|
| Filter simulated users from leaderboard | Implemented `IsSimulated` filter in `GetLeaderboardAsync` | Done |
| Join Users + PlayerStats for enriched data | New `GetLeaderboardEntriesAsync` with EF Core GroupJoin | Done |
| Expand LeaderboardEntry model | Added GamesSolved, BestScore, CurrentStreak | Done |
| Update frontend display | Added Solved, Best Score, Streak columns | Done |
| Add backend tests | 3 new tests + 1 updated (simulated exclusion, fallback stats) | Done |
| Update frontend tests | Mock data updated for new fields | Done |

## Test Metrics

| Metric | Before | After |
|--------|--------|-------|
| Backend tests | 320 | 323 (+3) |
| Frontend tests | 61 | 61 (updated) |
| Build errors | 0 | 0 |

## What Went Well

1. **Clean separation**: The `GetLeaderboardEntriesAsync` method cleanly handles both MySQL (EF Core join) and JSON fallback paths
2. **No regressions**: All 323 backend + 61 frontend tests pass
3. **Efficient query**: Replaced N+1 `GetGameCountAsync` calls with a single GroupJoin query

## What Could Be Improved

1. **DbContext as optional parameter**: Injecting `LinkittyDoDbContext?` as an optional constructor parameter works but is slightly unusual — could be formalized with a dedicated leaderboard repository if more query complexity is added later

## Lessons Learned

- **L16** (Sprint 42): When service methods do N+1 queries to compose API responses, move the join to the data layer (EF Core GroupJoin or SQL) early — controller-level loops over async calls are both slow and harder to test.

## Sprint Rating

**4/5 — Good**. Small, focused sprint with clean delivery. Single item completed on first pass with no blockers.
