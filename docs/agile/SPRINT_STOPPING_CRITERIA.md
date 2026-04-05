# Sprint Stopping Criteria

This document defines when and why to stop work during a sprint. Stopping at the right time prevents wasted effort. Stopping for the wrong reason wastes momentum.

## Valid Reasons to Stop

### 1. All Tasks Complete

All sprint items meet their acceptance criteria. Tests pass. This is normal sprint completion - proceed to Phase 4 (Review & Testing).

### 2. Blocked on External Dependency

Something outside the sprint's control prevents progress:
- Waiting for a third-party API (Datamuse, DuckDuckGo, OpenAI)
- Infrastructure not available (Azure, GitHub Actions)
- Dependency on a decision only the user can make

**Action**: Document the blocker, notify the user, work on unblocked items if any remain.

### 3. User Requests Scope Change

The user wants to add, remove, or reprioritize items mid-sprint.

**Action**: Pause execution, update the sprint plan, get re-approval, then resume.

### 4. Critical Bug Found

A show-stopping defect is discovered that is outside the sprint scope but affects core functionality.

**Action**: Document the bug, assess severity. If P0/P1, create an issue and discuss with user whether to address now or defer.

### 5. User Requests Early Review

The user wants to see progress before the sprint is complete.

**Action**: Present current state, get feedback, then resume or adjust.

### 6. Context Limit Approaching

The conversation is getting very long and context quality may degrade.

**Action**: Summarize progress, document remaining tasks, and suggest continuing in a new session.

---

## Invalid Reasons to Stop (Do NOT Stop For These)

| Situation | Wrong Response | Correct Response |
|-----------|---------------|-----------------|
| Implementation choice ("method A or B?") | Stop and ask | Make best judgment, continue |
| Approach uncertainty | Stop and ask | Implement, test, iterate |
| Single test failure | Stop and report | Fix the test, continue |
| Code style question | Stop and ask | Follow existing patterns, continue |
| Feature seems incomplete | Stop and verify | Complete acceptance criteria, continue |
| Minor refactoring needed | Stop and ask | Do it if it is small, note as tech debt if large |

---

## Key Principle

Sprint plan approval is blanket approval for all implementation decisions needed to meet acceptance criteria. The only valid stops are the 6 criteria above.
