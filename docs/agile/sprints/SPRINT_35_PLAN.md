# Sprint 35 Plan — Context Exhaustion Prevention

**Date**: 2026-04-10
**Goal**: Reduce context exhaustion during sprints by adding context-awareness to agents, improving session handoff, and creating a context-budget estimation checklist.
**Branch**: `feature/20260410-sprint-35`

## Problem Statement

Sprint 33 retro explicitly cited "context budget exhaustion" as a problem. Multi-sprint pipelines and large refactors consume the entire context window, forcing session breaks mid-execution. Agents have no guidance on minimizing context usage. The sprint-status.json lacks task-level granularity for session resumption.

## Items

| # | Task | Acceptance Criteria |
|---|------|-------------------|
| 1 | Add context-efficiency guidelines to all 4 agents | Each agent.md includes a "Context Efficiency" section with rules: prefer targeted reads over full-file reads, batch related changes, avoid redundant searches. |
| 2 | Enhance sprint-status.json schema for task-level tracking | Schema includes `tasks` array with `{id, title, status}` for each sprint item. Workflow docs reference the new schema. |
| 3 | Add "Context Budget Estimation" section to SPRINT_EXECUTION_WORKFLOW.md Phase 3 | Before each task, estimate context cost (Small/Medium/Large). If cumulative > 85%, save state. |
| 4 | Create session-handoff checklist in SPRINT_STOPPING_CRITERIA.md | Criterion 6 (Context Limit) gets a concrete checklist: what to save, where to save it, how to resume. |
| 5 | Add memory-save instructions to copilot-instructions.md | New section explaining when and how to persist sprint progress to `/memories/repo/`. |

## Risks
- None significant — process documentation changes only.
