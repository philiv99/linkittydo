# Sprint 4 Retrospective

**Sprint**: 4 - Frontend Architecture & History
**Date**: 2026-04-09
**Branch**: `feature/20260409-sprint-4`

## Results

| Item | Status |
|------|--------|
| React Router + page structure (#8) | Complete |
| Home page (#9) | Complete |
| Game history UI (#10) | Complete |

**Items completed**: 3/3
**Tests**: 49 frontend (20 new), 112 backend (unchanged)
**Build**: Clean

## What Went Well
- Clean separation of pages from existing GameBoard monolith
- useUser hook works well across pages via localStorage sharing
- react-router-dom integrated cleanly with existing base URL config
- All existing tests continued to pass without modification

## What Could Improve
- App.tsx now has duplicate user modal handling (also exists in GameBoard) — could be consolidated in a future sprint
- Some act() warnings in tests for async state updates — not blocking but should be addressed

## Key Decisions
- Kept GameBoard's internal user management intact rather than lifting state — avoids breaking changes
- Used MemoryRouter in tests for easy route simulation
- Added basename to BrowserRouter to match vite base config
