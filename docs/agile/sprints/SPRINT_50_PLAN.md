# Sprint 50 Plan: Scale Phrase Database & Backlog Cleanup

**Date**: 2026-04-12
**Branch**: `feature/20260412-sprint-50`
**Goal**: Scale the phrase database from 5 seed phrases to 100+ curated phrases across difficulty bands, fix admin duplicate handling, and clean up severe backlog drift.

---

## Pre-Sprint Audit Findings

All 8 Game Persistence Reliability items (#118-124, #128) and 11 other P1 items were already completed in Sprints 38-49 but never moved to the Completed section. This sprint corrects the backlog drift per lesson L10.

---

## Tasks

| ID | Task | Estimate |
|----|------|----------|
| T1 | Clean up backlog drift — move 19+ completed items to Completed section | Small |
| T2 | Expand phrase seed data to 100+ curated phrases across difficulty bands (0-100) | Large |
| T3 | Fix admin phrase duplicate handling — return 409 with friendly error instead of raw DB exception | Small |
| T4 | Pre-compute difficulty at seed time (use ComputePhraseDifficulty formula) | Small |
| T5 | Add backend tests (duplicate handling, seed data validation) | Medium |
| T6 | Build and test validation (backend 321+, frontend 64) | Small |
| T7 | Sprint retrospective | Small |

## Test Expectations

- T3: 2 tests — duplicate phrase returns 409, unique phrase returns 201
- T5: 1 test — seed phrases have non-zero difficulty values
- All existing tests continue to pass

## Acceptance Criteria

1. Backlog Completed section is current through Sprint 49
2. DatabaseSeedService seeds 100+ phrases with computed difficulties
3. Admin phrase creation returns 409 on duplicate (not 500)
4. All backend tests pass (target: 325+)
5. Frontend build passes with 0 lint errors
