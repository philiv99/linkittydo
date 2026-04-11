# Sprint 45 Retrospective — Leaderboard Real Player Data

**Date**: 2026-04-11
**Branch**: `feature/20260411-sprint-45`
**PR**: #52

---

## Summary

**Goal**: Fix leaderboard to only show players with real game data.
**Result**: Complete — all 5 tasks delivered.

## What Was Delivered

| # | Task | Status |
|---|------|--------|
| 1 | Filter backend leaderboard query (LifetimePoints > 0) | Done |
| 2 | Verify names display correctly | Done — names come from User.Name, "(unknown)" fallback for empty |
| 3 | Add rank column tooltips | Done — all 7 columns have descriptive tooltips |
| 4 | Update leaderboard tests (+3 new) | Done |
| 5 | Verify builds pass | Done — 332 backend, 64 frontend |

## Metrics

- Backend tests: 332 (was 329, +3 new)
- Frontend tests: 64 (unchanged from Sprint 44)
- Build: Clean on both frontend and backend

## Root Cause Analysis

The leaderboard query filtered on `IsActive && !IsSimulated` but did not filter on `LifetimePoints > 0`. Users who registered but never played (or scored 0) appeared in the leaderboard with blank stats.

## Rank Column Clarification

Rank is a sequential 1-based position from sorted query results. Order: LifetimePoints DESC, then Name ASC. Top 3 get medal emojis. Not a stored DB value — recomputed on each query.

## Lessons Learned

- **L16** (Sprint 45): Leaderboard and ranking queries should filter for meaningful data (e.g., points > 0 or games > 0) — showing empty rows degrades trust in the display.
