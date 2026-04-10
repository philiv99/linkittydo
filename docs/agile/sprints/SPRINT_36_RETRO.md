# Sprint 36 Retrospective

**Date**: 2026-04-10
**Sprint Goal**: Encode learned lessons from retrospectives into agents and instructions to prevent recurring practice gaps
**Result**: Complete
**PR**: #43 (merged to main)

## What Was Delivered

| # | Task | Status |
|---|------|--------|
| 1 | Update build-validator and full-test skill to enforce `npm run build` | Done |
| 2 | Add test coverage expectations to sprint plan template | Done |
| 3 | Create Lessons Learned registry (L1-L11) in copilot-instructions.md | Done |
| 4 | Add Pre-Commit Checklist to verify-app agent | Done |
| 5 | Add Retro Lesson Encoding step to Phase 5 workflow | Done |

## What Went Well
- All 5 items completed in one pass
- Lessons from 13 retrospectives (Sprints 1-33) are now indexed and searchable (L1-L11)
- Pre-commit checklist directly references lesson IDs for traceability
- The learning loop is now closed: retro → lesson → copilot-instructions → agent behavior

## What Could Improve
- Should periodically review the Lessons Learned registry to prune obsolete entries (e.g., if JSON storage is fully deprecated, L4 about EF Core discriminators may need updating)

## Metrics
- Backend tests: 294 (unchanged — docs-only sprint)
- Frontend tests: 61 (unchanged)
- Items completed: 5 of 5
- PR: #43
