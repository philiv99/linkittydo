---
name: phase-check
description: 'Sprint phase transition checkpoint. Use when: finishing a sprint phase, verifying phase completion, checking readiness for next phase, reviewing sprint progress.'
argument-hint: 'Current phase number (1-5) to verify completion'
user-invocable: true
---

# Phase Transition Checkpoint

Verify the current sprint phase is complete and review next phase requirements before proceeding.

## When to Use

- Before moving from one sprint phase to the next
- When uncertain if all phase requirements are met
- To get a status overview of sprint progress

## Procedure

### 1. Determine Current Phase

Based on conversation context or the argument provided, identify which phase was just completed. Reference the [Sprint Execution Workflow](../../docs/agile/SPRINT_EXECUTION_WORKFLOW.md). Phases are 0 (Prerequisites), 1 (Planning), 2 (Kickoff), 3 (Execution), 4 (Review & Testing), 5 (Retrospective).

### 2. Verify Phase Completion

Check all items for the current phase and mark each:
- `[OK]` Complete
- `[MISSING]` Not yet done
- `[N/A]` Not applicable

### 3. Check Sprint Documents

Verify required documents exist:
- `docs/agile/sprint-status.json` updated (required at Phase 2 and Phase 5)
- `docs/agile/sprints/SPRINT_N_PLAN.md` (required after Phase 1)
- `CHANGELOG.md` updated (required during Phase 3)
- `docs/agile/BACKLOG.md` updated (required after Phase 5)
- `docs/agile/sprints/SPRINT_N_RETRO.md` (required after Phase 5)

### 4. Preview Next Phase

List all requirements for the upcoming phase.

## Output Format

```
Phase Transition Checkpoint
===========================
Current Phase: [N] - [Name]
Next Phase: [N+1] - [Name]

Current Phase Status:
- [OK/MISSING] Item 1
- [OK/MISSING] Item 2

Sprint Documents:
- [OK/MISSING] sprint-status.json updated
- [OK/MISSING] SPRINT_N_PLAN.md
- [OK/MISSING] CHANGELOG.md updated
- [OK/MISSING/N/A] SPRINT_N_RETRO.md

Next Phase Requirements:
- [ ] Item 1
- [ ] Item 2

Ready to proceed: [Yes/No - fix MISSING items first]
```
