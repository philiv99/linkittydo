# Sprint 40 Retrospective

**Sprint**: 40  
**Date**: 2026-04-12  
**Goal**: In-progress game persistence and abandoned game tracking  
**Result**: Complete — all 3 items delivered + async migration  
**PR**: #47 (merged to main)

## Delivered vs Planned

| Task | Backlog | Status |
|------|---------|--------|
| Persist GameRecord to DB at game start | #121 | Done |
| Persist events incrementally | #122 | Done |
| Track abandoned/expired games | #123 | Done |

## Metrics

- **Tests**: 306 → 311 (+5 new, restructured persistence tests)
- **Files changed**: 12
- **Lines added/removed**: +255 / -101

## What Went Well

1. **Clean lifecycle change**: Moving from create-on-end to create-on-start/update-on-end was cleaner than expected
2. **Incremental persistence natural**: Each event type already had a clear insertion point
3. **Abandoned tracking elegant**: Leveraging the existing cleanup service was straightforward

## What Could Be Improved

1. **Flaky simulation test**: `SimulateGame_ScoredCorrectlyForSolvedGame` uses randomness and intermittently fails — should be backlogged
2. **Interface churn**: Two sprints of progressive async migration caused double-touch on test files

## Lessons Applied

- **L3** (grep before changing): Verified all callers of RecordClueEvent and RemoveExpiredSessions before making async
- **L7** (tests with code): Updated all affected tests in same commit
