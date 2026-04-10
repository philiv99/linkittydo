# Sprint Stopping Criteria

This document defines when and why to stop work during a sprint. Stopping at the right time prevents wasted effort. Stopping for the wrong reason wastes momentum.

## Valid Reasons to Stop

### 1. All Tasks Complete

All sprint items meet their acceptance criteria. All tests pass (including every new test introduced during the sprint). This is normal sprint completion - proceed to Phase 4 (Review & Testing).

**A sprint is NOT complete if any new tests are failing.** See Criterion 7 (Test Failure Escalation) for how to handle persistent test failures.

### 2. Blocked on External Dependency

Something outside the sprint's control prevents progress:
- Waiting for a third-party API (Datamuse, DuckDuckGo, OpenAI)
- Infrastructure not available (Azure, GitHub Actions)
- Dependency on a decision only the user can make

**Action**: Document the blocker using the template below, notify the user, work on unblocked items if any remain.

**Blocker Documentation Template**:
```markdown
[STOP] BLOCKED: [Brief description]

Root Cause: [Why this is blocking progress]

Attempted Solutions:
- [Approach 1] - [Why it did not work]
- [Approach 2] - [Why it did not work]

What's Required to Unblock:
- [Input or decision needed]
- [Alternative approach if any]
```

### 3. User Requests Scope Change

The user wants to add, remove, or reprioritize items mid-sprint.

**Action**: Pause execution, update the sprint plan, get re-approval, then resume.

### 4. Bug Discovery

A defect is discovered that is outside the sprint scope. Triage by severity:

| Severity | Description | Action |
|----------|-------------|--------|
| **Critical** | Blocks sprint progress or core functionality broken | Fix immediately as part of sprint work. Add regression test. |
| **High** | Affects work quality but does not block | Fix if time allows this sprint. Otherwise create issue with `bug` label. |
| **Medium** | Visible but not blocking | Create issue with `bug` label. Do NOT fix during sprint (avoid context switch). |
| **Low** | Edge case, no user impact | Create issue with `bug` label. Add to backlog for future sprint. |

Only **Critical** bugs are a valid reason to pause sprint work. High/Medium/Low bugs are documented and deferred.

### 5. User Requests Early Review

The user wants to see progress before the sprint is complete.

**Action**: Complete current task to a stable state, commit in-progress work, present current state, get feedback, then resume or adjust.

### 6. Context Limit Approaching

The conversation context is running low and quality may degrade.

| Context Level | Action |
|---------------|--------|
| < 70% | Continue normally |
| 70-84% | Estimate remaining work; if next task may exceed 95%, save state first |
| 85-94% | Save sprint progress to memory and `sprint-status.json` immediately before continuing |
| 95%+ | STOP - commit all work, save to memory, notify user a new session is needed |

**Context cost estimation** - before starting a task, estimate its context cost:

| Task Size | Examples | Estimated Cost |
|-----------|----------|----------------|
| Small | Docs update, single-file edit, config change | ~5-10% |
| Medium | Multi-file feature, writing tests, refactoring | ~15-25% |
| Large | Research, architecture work, complex debugging | ~25-40% |

If `current_usage% + estimated_task_cost% > 95%`, save state before starting the task.

**Action at threshold**:
1. Save sprint progress to Copilot memory (`/memories/repo/`) with current sprint number, branch, tasks completed, tasks remaining, and any blockers
2. Update `docs/agile/sprint-status.json` with current state
3. Commit and push all work in progress
4. Notify the user that a new session is needed to continue

### Session Handoff Checklist (added Sprint 35)

When a session break is needed (context at 85%+ or session ending), follow this exact checklist:

**Before ending the session:**
- [ ] Update `sprint-status.json` → set `tasks` array with status for each item (done/in-progress/not-started)
- [ ] Commit all work in progress with message: `wip: Sprint N - session break after task M`
- [ ] Push to feature branch
- [ ] Save to Copilot memory (`/memories/repo/sprint-N-progress.md`) with:
  - Current branch name
  - List of completed tasks with one-line summary of what was done
  - List of remaining tasks
  - Any blockers, open questions, or partial work state
  - Files currently being modified

**When resuming in a new session:**
- [ ] Read `sprint-status.json` for current state
- [ ] Read `/memories/repo/sprint-N-progress.md` for saved context
- [ ] `git checkout {branch}` and `git pull`
- [ ] Verify builds pass: `dotnet build` and `npm run build`
- [ ] Continue from the first non-done task in `sprint-status.json`

### 7. Test Failure Escalation

**Rule**: All new tests introduced during a sprint MUST pass before the sprint can be marked complete. A sprint with failing new tests is NOT done.

When a test fails and a straightforward fix is not apparent:

```
Test Failure Detected
    |
    v
Attempt fix (15-30 minutes)
    |--- Fixed --> Continue sprint
    |
    v (NOT fixed)
Try alternative approach (15-30 minutes)
    |--- Fixed --> Continue sprint
    |
    v (NOT fixed)
Request user approval to defer with documented reason
    |--- Approved --> Create issue, continue sprint
    |--- Not approved --> Keep working on fix
```

**Key rules**:
- Do NOT accept test failures as "pre-existing" without investigation
- Do NOT mark a sprint complete with failing new tests unless the user explicitly approves deferral
- Document what was attempted and why it failed before requesting deferral

### 8. Fundamental Design Failure

A design issue is discovered that requires rethinking the approach - not just fixing a bug.

**Indicators**:
- Test failures suggest a design flaw, not an implementation bug
- Attempted solutions create fragile or overly complex code
- Architecture does not support the requirements

**Action**:
1. Document the design issue
2. Propose 2-3 alternative approaches with trade-offs
3. Discuss with user before proceeding
4. Restart implementation with the chosen approach

### 9. Time Limit Reached

The sprint has reached its planned duration limit.

**Action**: Complete current task to a stable state, commit all work, proceed to Phase 4 (Review & Testing). Remaining tasks move to the next sprint as carry-over items.

---

## Invalid Reasons to Stop (Do NOT Stop For These)

| Situation | Wrong Response | Correct Response |
|-----------|---------------|-----------------|
| Implementation choice ("method A or B?") | Stop and ask | Make best judgment, document decision, continue |
| Approach uncertainty | Stop and ask | Implement, test, iterate |
| Single test failure | Stop and report | Fix the test, continue |
| Code style question | Stop and ask | Follow existing patterns, continue |
| Feature seems incomplete | Stop and verify | Complete acceptance criteria, continue |
| Minor refactoring needed | Stop and ask | Do it if it is small, note as tech debt if large |
| Method signature change needed | Stop and ask | Change if needed for acceptance criteria, continue |
| Refactor vs extend decision | Stop and ask | Choose based on code quality, document, continue |

---

## Key Principle

Sprint plan approval is blanket approval for all implementation decisions needed to meet acceptance criteria. The only valid stops are the 9 criteria above.
