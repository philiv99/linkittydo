# Sprint 36 Plan — Developer Practice Gaps

**Date**: 2026-04-10
**Goal**: Encode learned lessons from retrospectives into agents and instructions so common mistakes (missing tests, wrong build commands, unchecked API signatures) are prevented automatically.
**Branch**: `feature/20260410-sprint-36`

## Problem Statement

Retrospectives repeatedly flag the same issues: "No new tests added" (Sprints 28, 29, 33), "unused import slipped through" (Sprint 33), "`npm run build` not used as definitive check" (Sprint 33), "API signature mismatch" (Sprint 28). These lessons are documented in retros but not encoded into the agent instructions or workflow checklists, so they recur.

## Items

| # | Task | Acceptance Criteria |
|---|------|-------------------|
| 1 | Update build-validator agent to use `npm run build` as definitive frontend check | Agent instructions specify `npm run build` (not `npx tsc --noEmit`) and explain why. Also update full-test skill. |
| 2 | Add "Test Coverage Expectations" section to sprint plan template | Template includes a field for each item: "Expected new tests: [description]". |
| 3 | Create lessons-learned registry in copilot-instructions.md | New "Lessons Learned" section with indexed entries from retros that agents must check. |
| 4 | Update verify-app agent with pre-commit checklist | Agent includes: check unused imports, verify all new code has tests, run `npm run build`, check API signatures match. |
| 5 | Add "Retro Lesson Encoding" step to SPRINT_EXECUTION_WORKFLOW.md Phase 5 | Retro checklist includes: extract lessons, add to copilot-instructions lessons-learned, update affected agents. |

## Risks
- None significant — documentation and agent instruction changes only.
