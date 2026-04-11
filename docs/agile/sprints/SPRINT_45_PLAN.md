# Sprint 45 Plan — Leaderboard Real Player Data

**Date**: 2026-04-11
**Branch**: `feature/20260411-sprint-45`
**Backlog Item**: #130

---

## Goal

Fix leaderboard to only show players with real game data. Remove rows without actual player stats. Verify player names display correctly. Clarify the Rank column.

## Root Cause

Backend `GetLeaderboardEntriesAsync` returns ALL active non-simulated users ordered by `LifetimePoints`, including users who registered but never played (0 games, 0 points). No filtering for actual gameplay activity exists.

## Tasks

| # | Task | Description |
|---|------|-------------|
| 1 | Filter backend leaderboard query | Add `WHERE LifetimePoints > 0` filter (or `GamesPlayed > 0` via PlayerStats/GameRecords) to exclude users who have never played. |
| 2 | Verify names display correctly | Trace the Name field from DB through API to frontend. Ensure user names are populated. |
| 3 | Add rank column tooltip/clarity | Add tooltip or header clarification that Rank is based on lifetime points position. |
| 4 | Update leaderboard tests | Add tests for the filtered query — users with 0 points/games excluded, users with real stats included. |
| 5 | Verify builds pass | Run `dotnet build`, `npm run build`, and all tests. |

## Test Expectations

- Test: Leaderboard excludes users with 0 lifetime points
- Test: Leaderboard includes users with positive lifetime points
- Test: Leaderboard preserves correct ranking order after filtering
- Test: Leaderboard handles empty result set gracefully

## Acceptance Criteria

1. Leaderboard only shows players who have actually played (LifetimePoints > 0)
2. Player names display correctly in the Player column
3. No canned or prepopulated data rows appear
4. Rank column is clear (sequential based on points ranking)
5. All existing tests pass, new tests added
6. Both frontend and backend builds pass cleanly
