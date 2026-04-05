# Sprint 2 Retrospective

**Date**: 2026-04-05
**Sprint Goal**: Create a single-command launcher that starts backend API, frontend dev server, and opens the app in a browser
**Result**: Complete
**User Rating**: Good

## What Went Well
- Backlog item #36 delivered quickly with both start and stop scripts
- Scripts tested end-to-end: servers start, ports listen, browser opens, clean shutdown
- Named windows (`LinkittyDo-API`, `LinkittyDo-Web`) enable clean process management in stop script
- Auto-installs npm dependencies if `node_modules` is missing
- Zero test regressions (70 backend, 29 frontend)

## What Could Improve
- **Missed port-in-use check**: The initial `start-app.bat` did not check whether ports 5157 or 5173 were already in use before launching servers. This was caught during review and fixed post-merge. Scripts that start services should always validate port availability first.

## Improvements Applied This Sprint

| Improvement | Document Updated | Details |
|-------------|-----------------|---------|
| Port-in-use checks added to start-app.bat | start-app.bat | Script now checks both ports before launching and exits with a clear error if either is occupied |

## Improvements Deferred

| Improvement | Priority | Target |
|-------------|----------|--------|
| Pre-existing lint warnings (GameBoard.tsx, UserManageModal.tsx) | Low | Future sprint |

## Process Takeaway
When building scripts that start services on specific ports, always include a pre-flight check for port availability. This avoids confusing failures from duplicate processes.

## Metrics
- Backend tests: 70 passing
- Frontend tests: 29 passing
- Lint/type errors: 0 new
- Items completed: 1 of 1
- Items carried over: none
