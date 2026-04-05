# Sprint Retrospective Guide

This document defines the structured retrospective process for LinkittyDo sprints. The retrospective is how the team learns and improves sprint-over-sprint.

## Purpose

Retrospectives are not just documentation. The findings from each retro are **applied** to the process documents themselves, creating a learning loop where each sprint improves the system for the next.

## When to Run

After Phase 4 (Review & Testing) is complete and the PR has been created. This is Phase 5 of the sprint workflow.

---

## Step 1: Sprint Review (5 min)

Summarize the sprint outcome:

```markdown
## Sprint [N] Review

**Goal**: [original sprint goal from plan]
**Result**: [Complete / Partial / Blocked]
**Duration**: [start] - [end]

### Tasks
| Task | Status | Notes |
|------|--------|-------|
| A. ... | Done | |
| B. ... | Done | |
| C. ... | Carry-over | [why] |
```

---

## Step 2: Gather Feedback (10-15 min)

Ask the user for input on these categories. Not all categories apply every sprint — skip those that are not relevant.

| Category | Question |
|----------|----------|
| **Effectiveness** | Did the sprint deliver what was planned? |
| **Quality** | Were there bugs, regressions, or quality issues? |
| **Planning** | Was the scope right? Too much? Too little? |
| **Testing** | Was test coverage adequate? Any gaps? |
| **Process** | Did the 5-phase workflow work well? Any friction? |
| **Communication** | Were there misunderstandings or unclear requirements? |

User rates overall sprint: **Poor / Fair / Good / Very Good / Excellent**

---

## Step 3: Identify Improvements (5-10 min)

Based on user feedback and observations during the sprint, propose specific improvements:

```markdown
### Improvements Identified

| # | Improvement | Priority | Apply When |
|---|-------------|----------|------------|
| 1 | [specific, actionable improvement] | High | Now |
| 2 | [specific, actionable improvement] | Medium | Next Sprint |
| 3 | [specific, actionable improvement] | Low | Future |
```

**Priority definitions**:
- **High**: Caused real problems this sprint. These MUST be applied to the feature branch before the PR is merged.
- **Medium**: Would help but did not block work. Add to backlog for next sprint.
- **Low**: Nice to have. Add to backlog for future.

**Enforcement**: High-priority improvements are mandatory and will be verified in the next sprint's Phase 0 prerequisites. If they were not applied, they must be applied before the new sprint begins.

---

## Step 4: Apply Improvements (THE LEARNING LOOP)

This is the critical step. Improvements rated "Apply Now" are implemented immediately by updating the relevant process documents:

| If improvement relates to... | Update this document |
|------------------------------|---------------------|
| Sprint workflow steps | `docs/agile/SPRINT_EXECUTION_WORKFLOW.md` |
| When to stop/continue | `docs/agile/SPRINT_STOPPING_CRITERIA.md` |
| Retrospective process | `docs/agile/SPRINT_RETROSPECTIVE_TEMPLATE.md` |
| Backlog priorities | `docs/agile/BACKLOG.md` |
| Coding patterns | `.github/copilot-instructions.md` |
| Build/test process | `.github/skills/full-test/SKILL.md` |
| Agent behavior | `.github/agents/*.agent.md` |

**Commit the process doc changes** to the feature branch with the sprint retro:
```
docs: apply sprint N retrospective improvements
```

Improvements rated "Next Sprint" or "Future" are added to `BACKLOG.md` as `chore` items.

---

## Step 5: Document the Retrospective

Create `docs/agile/sprints/SPRINT_N_RETRO.md` using this template:

```markdown
# Sprint [N] Retrospective

**Date**: [date]
**Sprint Goal**: [goal]
**Result**: [Complete / Partial]
**User Rating**: [Poor / Fair / Good / Very Good / Excellent]

## What Went Well
- ...

## What Could Improve
- ...

## Improvements Applied This Sprint
| Improvement | Document Updated | Commit |
|-------------|-----------------|--------|
| ... | ... | ... |

## Improvements Deferred
| Improvement | Priority | Target |
|-------------|----------|--------|
| ... | Medium | Sprint N+1 |

## Metrics
- Backend tests: [X passing]
- Frontend tests: [X passing]
- Lint/type errors: [0]
- Items completed: [X of Y]
- Items carried over: [list]
```

---

## Step 6: Update Sprint State

After the retrospective:
1. Update `docs/agile/sprint-status.json` to reflect sprint completion
2. Update `docs/agile/BACKLOG.md`: remove completed items, add new discoveries
3. Save sprint context to Copilot memory for continuity (see Memory section below)

---

## Memory and Context Persistence

### Why This Matters

Conversations have limited context. Sprint knowledge must persist so the next session can resume effectively.

### What to Persist (Copilot Memory)

At the end of each sprint or session, save key context to `/memories/repo/`:

```
Sprint [N] completed on [date].
- Goal: [goal]
- Result: [Complete/Partial]
- Key lessons: [1-2 bullet points from retro]
- Process changes made: [which docs updated]
- Carry-over items: [any unfinished work]
- Next sprint candidates: [top 2-3 from backlog]
```

### Sprint Status File

The file `docs/agile/sprint-status.json` persists sprint state in the repository:

```json
{
  "current_sprint": {
    "number": 1,
    "status": "complete",
    "branch": "feature/YYYYMMDD-sprint-1",
    "plan_approved": true,
    "pr_number": null
  },
  "last_completed": {
    "number": 1,
    "date": "2026-04-10",
    "result": "complete",
    "items_completed": 3,
    "items_carried_over": 0,
    "key_lesson": "Brief lesson from retrospective"
  },
  "test_metrics": {
    "backend_tests_passing": 0,
    "frontend_tests_passing": 0,
    "lint_errors": 0
  }
}
```

This file survives context loss and can be read at the start of any new session to understand the current state.
