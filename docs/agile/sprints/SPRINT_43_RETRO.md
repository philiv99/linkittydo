# Sprint 43 Retrospective — Leaderboard Data Accuracy Fix

**Date**: 2026-04-11
**Sprint Goal**: Fix leaderboard displaying incorrect data — missing names, zero stats
**Result**: Complete — all 5 tasks delivered, PR #50 merged

## What Was Delivered

| # | Task | Status |
|---|------|--------|
| 1 | Rewrite `GetLeaderboardEntriesAsync` to two-query approach | Done |
| 2 | Add fallback stats from GameRecords when PlayerStats empty | Done |
| 3 | Handle empty/null names with "(unknown)" fallback | Done |
| 4 | Add 6 new EF Core integration tests for leaderboard | Done |
| 5 | Verify builds pass (backend + frontend) | Done |

## Metrics

| Metric | Before | After |
|--------|--------|-------|
| Backend tests | 323 | 329 |
| Frontend build | Clean | Clean |
| Lint errors | 0 | 0 |

## Root Cause Analysis

The leaderboard bug had three root causes:

1. **EF Core GroupJoin unreliability**: The `GroupJoin`+`SelectMany`+`Take` LINQ composition is notoriously fragile in EF Core. It can silently produce incorrect SQL, lose ordering, or fall back to client-side evaluation. This was the core technical issue — Sprint 42 introduced the GroupJoin pattern, and it immediately broke in production use.

2. **No data resilience**: The leaderboard relied **solely** on the pre-computed `PlayerStats` table. If analytics recomputation hadn't run (first startup, failed recompute, admin-added users), all stats showed 0 with no fallback.

3. **Missing input validation at display layer**: Empty/null player names rendered as invisible blank cells instead of a fallback label.

## Lessons Learned

### L15 (New): Avoid EF Core GroupJoin for production queries
EF Core's `GroupJoin`+`SelectMany`+`DefaultIfEmpty` pattern (LEFT JOIN) is unreliable when composed with `Take`, `OrderBy`, or other operators. Prefer two separate queries joined in memory — it's more readable, testable, and produces correct results. Reserve complex LINQ compositions for simple cases only.

### L16 (New): Pre-computed tables need fallback paths
When displaying data from pre-computed/cached tables (like `PlayerStats`), always provide a fallback computation from the source data (like `GameRecords`). Users should never see stale zeros because a background process hasn't run yet.

## Process Observations

- Sprint 42 introduced the GroupJoin pattern as an optimization over N+1 queries. Sprint 43 had to rewrite it because the "optimization" produced incorrect results. This reinforces the principle: **correctness first, then optimize**.
- The investigation was fast because the data flow was well-documented in copilot-instructions.md (codebase map).
- The two-query approach is actually simpler code than the GroupJoin it replaced.

## Action Items

- [x] Add L15 to copilot-instructions.md lessons registry
- [x] Add L16 to copilot-instructions.md lessons registry
