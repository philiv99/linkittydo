# Sprint 43 Plan — Leaderboard Data Accuracy Fix

**Date**: 2026-04-11
**Branch**: `feature/20260411-sprint-43`
**Goal**: Fix leaderboard displaying incorrect data — missing player names, zero stats for all columns

## Problem Statement

The Leaderboard page shows:
- Player names missing or invisible for some rows
- Games, Solved, Best Score, Streak all showing 0 despite users having played games
- Points display from User.LifetimePoints but all PlayerStats-derived fields are empty

## Root Cause Analysis

1. **EF Core GroupJoin query unreliable**: The `GroupJoin`+`SelectMany`+`Take` composition generates SQL where ordering and LEFT JOIN correctness aren't guaranteed
2. **No fallback when PlayerStats is empty**: Leaderboard relies solely on the pre-computed `PlayerStats` table. If analytics recomputation hasn't run or failed, all stats show 0
3. **Missing player names**: Some users have empty/null names in the database; UI renders nothing
4. **No graceful handling in UI**: Frontend doesn't handle empty names

## Tasks

| # | Task | Type |
|---|------|------|
| 1 | Rewrite `GetLeaderboardEntriesAsync` to use two-query approach instead of GroupJoin | fix |
| 2 | Add fallback stats from GameRecords when PlayerStats has no data | fix |
| 3 | Handle empty/null player names with "(unknown)" fallback in UI | fix |
| 4 | Update existing leaderboard tests and add EF Core path tests | test |
| 5 | Verify builds pass (backend + frontend) | verify |

## Acceptance Criteria

- Leaderboard shows player names for all rows
- Stats (Games, Solved, Best Score, Streak) reflect actual GameRecord data
- Empty names display a fallback label
- All existing + new tests pass
- Both backend and frontend build cleanly
