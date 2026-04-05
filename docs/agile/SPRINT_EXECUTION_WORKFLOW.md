# Sprint Execution Workflow

This document defines the phase-by-phase workflow for executing sprints on LinkittyDo. It is the operational checklist that turns a sprint plan into delivered software.

Each sprint creates a learning loop: plan, build, review, reflect, and apply improvements to the process itself.

## Overview

| Phase | Name | User Action Required | Purpose |
|-------|------|---------------------|---------|
| 0 | Prerequisites | None | Verify readiness |
| 1 | Planning | **Approve sprint plan** | Select work, create plan |
| 2 | Kickoff | None | Branch, issues, setup |
| 3 | Execution | None (autonomous) | Build, test, commit |
| 4 | Review & Testing | **Review PR** | Full validation, PR |
| 5 | Retrospective | **Provide feedback** | Reflect, learn, improve |

**User touchpoints**: The user must be present for Phase 1 (approval) and Phase 5 (feedback). Everything between runs autonomously.

---

## How a Sprint Starts

A new sprint is triggered by:
1. **User request**: "Let's plan the next sprint" or "Run /plan-sprint"
2. **Previous sprint complete**: After a retro is done, the natural next step is Phase 0

There is no automatic trigger. The user decides when to start the next sprint.

---

## Phase 0: Prerequisites

**Goal**: Verify the environment is ready for a new sprint.

### Checklist

- [ ] Read `docs/agile/sprint-status.json` to understand current state
- [ ] If a previous sprint exists:
  - [ ] Verify its PR was merged (or intentionally abandoned)
  - [ ] Verify its retrospective exists (`docs/agile/sprints/SPRINT_N_RETRO.md`)
  - [ ] Review retrospective for carry-over items and applied improvements
  - [ ] **Verify previous retro's High-priority improvements were applied to process docs** (check the "Improvements Applied" table in the retro document against the actual files)
- [ ] Check Copilot memory (`/memories/repo/`) for saved sprint context
- [ ] Verify backend builds: `dotnet build`
- [ ] Verify frontend builds: `npm run build`
- [ ] Working directory is clean (`git status`)

### Gate

All prerequisites pass before proceeding to planning. If previous sprint's High-priority retro improvements were NOT applied, apply them now before starting the new sprint.

---

## Phase 1: Planning

**Goal**: Select sprint scope and create a plan document.

**USER ACTION REQUIRED**: User must approve the sprint plan before execution begins.

### Checklist

- [ ] Review `docs/agile/BACKLOG.md` for prioritized candidates
- [ ] Review previous sprint retrospective (if any) for:
  - Carry-over items that were not completed
  - Process improvements that should be tested this sprint
  - Lessons learned that affect scope or approach
- [ ] Select items for the sprint (aim for 1-2 weeks of work)
- [ ] Break each item into tasks with estimates
- [ ] Define acceptance criteria for each item
- [ ] Identify risks and mitigations
- [ ] Create `docs/agile/sprints/SPRINT_N_PLAN.md`
- [ ] Present plan to user for approval

### Gate

**Do not proceed to Phase 2 until the user approves the sprint plan.** Plan approval grants blanket authorization for all implementation decisions through Phase 3. No per-task approval will be requested.

---

## Phase 2: Kickoff

**Goal**: Set up the working environment for the sprint.

### Checklist

- [ ] Create feature branch: `feature/YYYYMMDD-sprint-N`
- [ ] Create GitHub issues for each sprint item (use sprint card template)
- [ ] Update `docs/agile/sprint-status.json`:
  ```json
  {
    "current_sprint": {
      "number": N,
      "status": "in-progress",
      "branch": "feature/YYYYMMDD-sprint-N",
      "plan_approved": true
    }
  }
  ```
- [ ] Verify all builds and tests pass

### Gate

All builds and tests must pass before starting execution.

---

## Phase 3: Execution

**Goal**: Implement all sprint items according to the plan.

### Checklist

For each task:
- [ ] Implement the change
- [ ] Write or update tests
- [ ] Run relevant tests to verify
- [ ] Commit with descriptive message referencing the issue (e.g., `feat: add difficulty slider (#12)`)
- [ ] Push to feature branch regularly

**Commit after each task completes** - do not batch all commits to the end of the sprint. Per-task commits provide intermediate save points, better traceability, and smaller diffs that are easier to review.

Cross-cutting:
- [ ] Update `CHANGELOG.md` with user-facing changes as they are committed
- [ ] Update GitHub issue comments with progress

### Execution Autonomy

Once the sprint plan is approved in Phase 1, proceed through all tasks without stopping to ask for per-task approval. Make best engineering judgment for implementation decisions and document them.

**Only stop for reasons listed in [SPRINT_STOPPING_CRITERIA.md](SPRINT_STOPPING_CRITERIA.md).**

### Test Failure Protocol

All new tests introduced during the sprint MUST pass before the sprint can be marked complete.

If a test fails and the fix is not straightforward:
1. Attempt to fix (15-30 minutes)
2. If still failing, try an alternative approach (15-30 minutes)
3. If still failing, request user approval to defer with a documented reason

Do NOT accept failing new tests. Do NOT mark the sprint complete with failing new tests unless the user explicitly approves deferral. See Criterion 7 in [SPRINT_STOPPING_CRITERIA.md](SPRINT_STOPPING_CRITERIA.md) for details.

### Context Limit Handling

If the conversation is approaching its context limit during execution:
1. Save sprint progress to Copilot memory (`/memories/repo/`) with:
   - Current sprint number and branch
   - Tasks completed so far
   - Tasks remaining
   - Any blockers or notes
2. Update `docs/agile/sprint-status.json` with current state
3. Commit and push all work in progress
4. Notify the user that a new session is needed to continue

---

## Phase 4: Review & Testing

**Goal**: Validate all changes, create PR.

**USER ACTION REQUIRED**: User reviews the PR.

### Checklist

- [ ] Run full test suite (backend + frontend): use `/full-test` skill
- [ ] Run code analysis (lint, type check)
- [ ] Backend builds cleanly
- [ ] Frontend builds cleanly
- [ ] **All new tests pass** (see Test Failure Protocol in Phase 3)
- [ ] Verify acceptance criteria for each sprint item
- [ ] Push final changes to feature branch
- [ ] Create Pull Request to `main` branch
  - Include: what changed, why, testing done, issue references
- [ ] Request user review

### Merge Gate

**The PR cannot be merged until Phase 5 (Retrospective) is complete.** The retro may produce High-priority improvements that must be applied to the feature branch before merging. This ensures the merged code includes both sprint work AND process improvements.

---

## Phase 5: Retrospective (THE LEARNING LOOP)

**Goal**: Reflect on the sprint, gather feedback, apply improvements to the process, and document findings.

**USER ACTION REQUIRED**: User provides feedback on the sprint.

This phase is **mandatory** for every sprint. It is how the system improves.

### Step 5.1: Sprint Review

Summarize what was delivered vs what was planned. Present to the user.

### Step 5.2: Gather User Feedback

Ask the user for input on:
- **Effectiveness**: Did the sprint deliver what was planned?
- **Quality**: Any bugs, regressions, or quality issues?
- **Planning**: Was the scope right?
- **Process**: Did the workflow work well? Any friction?

User rates overall: Poor / Fair / Good / Very Good / Excellent

### Step 5.3: Identify Improvements

Propose specific, actionable improvements categorized by priority:
- **High (Apply Now)**: Caused real problems this sprint. These MUST be applied to the feature branch before the PR is merged. They are not optional.
- **Medium (Next Sprint)**: Would help but did not block work. Add to backlog as `chore` items for the next sprint.
- **Low (Future)**: Nice to have. Add to backlog for future prioritization.

**Enforcement**: High-priority improvements are mandatory. They must be implemented on the feature branch and committed before the PR merge. The next sprint's Phase 0 will verify they were applied.

### Step 5.4: Apply Improvements (CRITICAL)

For each "Apply Now" improvement, update the relevant process document:

| Improvement type | Document to update |
|-----------------|-------------------|
| Workflow steps | `docs/agile/SPRINT_EXECUTION_WORKFLOW.md` |
| Stop/continue rules | `docs/agile/SPRINT_STOPPING_CRITERIA.md` |
| Retro process | `docs/agile/SPRINT_RETROSPECTIVE_TEMPLATE.md` |
| Coding patterns | `.github/copilot-instructions.md` |
| Build/test process | `.github/skills/full-test/SKILL.md` |
| Agent behavior | `.github/agents/*.agent.md` |

Commit changes: `docs: apply sprint N retrospective improvements`

**This is the learning loop.** Each sprint's lessons are baked into the process docs, so the next sprint runs better.

### Step 5.5: Document the Retrospective

Create `docs/agile/sprints/SPRINT_N_RETRO.md` using the template in [SPRINT_RETROSPECTIVE_TEMPLATE.md](SPRINT_RETROSPECTIVE_TEMPLATE.md).

### Step 5.6: Update State

- [ ] Update `docs/agile/sprint-status.json` to reflect completion
- [ ] Update `docs/agile/BACKLOG.md`:
  - Remove completed items
  - Add newly discovered items
  - Add deferred improvements as `chore` items
  - Re-prioritize if needed
- [ ] Save sprint summary to Copilot memory (`/memories/repo/`):
  ```
  Sprint N completed [date]. Goal: [goal]. Result: [result].
  Key lesson: [most important takeaway].
  Process docs updated: [list].
  Next candidates: [top items from backlog].
  ```
- [ ] Close GitHub issues for completed items

---

## Quick Reference: Sprint Documents

| Document | Created In | Required |
|----------|-----------|----------|
| `docs/agile/sprints/SPRINT_N_PLAN.md` | Phase 1 | Yes |
| `docs/agile/sprints/SPRINT_N_RETRO.md` | Phase 5 | Yes |
| `docs/agile/sprint-status.json` (updated) | Phase 2, 5 | Yes |
| `CHANGELOG.md` (updated) | Phase 3+ | Yes |
| `docs/agile/BACKLOG.md` (updated) | Phase 5 | Yes |

## Quick Reference: User Touchpoints

| When | What the user does |
|------|--------------------|
| Sprint start (Phase 1) | Reviews and approves the sprint plan |
| During execution | Nothing required (but can request early review) |
| PR created (Phase 4) | Reviews the pull request |
| Retrospective (Phase 5) | Provides feedback on 4-6 categories, rates the sprint |
| After retro | Approves PR merge |
