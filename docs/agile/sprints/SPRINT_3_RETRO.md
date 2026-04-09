# Sprint 3 Retrospective

**Date**: 2026-04-09
**Sprint Goal**: Implement difficulty-aware clue selection and enhanced scoring so that game difficulty actually affects gameplay
**Result**: Complete
**Items**: 3/3 delivered (#4, #5, #6)

## What Went Well
- All 3 backlog items completed in a single session
- 42 new tests added (112 total backend), all passing
- Clean separation: scoring logic is pure static methods, easy to test
- `InternalsVisibleTo` makes unit testing internal classes straightforward
- Existing 70 tests needed only mock signature updates, no logic regressions

## What Could Improve
- Sprint 2 originally mapped items #4-#7 but Sprint 3 plan excluded #7 (scale phrase database) — backlog sprint mapping was stale after Sprint 2 was replanned as the launcher sprint. Should update backlog mappings when sprint scope changes.

## Improvements Applied

| Improvement | Document Updated | Details |
|-------------|-----------------|---------|
| None required | — | Sprint executed smoothly |

## Improvements Deferred

| Improvement | Priority | Target |
|-------------|----------|--------|
| Update backlog sprint mappings after scope changes | Low | Next planning phase |
| Pre-existing lint warnings (GameBoard.tsx, UserManageModal.tsx) | Low | Future sprint |

## Metrics
- Backend tests: 112 passing (+42)
- Frontend tests: 29 passing (unchanged)
- Lint/type errors: 0 new
- Items completed: 3 of 3
- Items carried over: 0
