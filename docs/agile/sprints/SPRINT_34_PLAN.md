# Sprint 34 Plan — Backlog Drift Prevention

**Date**: 2026-04-10
**Goal**: Eliminate backlog drift by cleaning stale items, fixing outdated copilot-instructions, and adding automated hygiene checks to the sprint workflow.
**Branch**: `feature/20260410-sprint-34`

## Problem Statement

Across 33 sprints, the backlog has accumulated stale sprint assignments, completed items not marked done, and outdated descriptions. The copilot-instructions.md still references the original User model with `List<GameRecord> Games` (removed in Sprint 13), JSON file storage (MySQL since Sprint 11), and missing architecture context for features added in Sprints 15-33 (JWT auth, roles, admin, analytics, simulation). This causes agents to generate incorrect code patterns and wastes context on outdated information.

## Items

| # | Task | Acceptance Criteria |
|---|------|-------------------|
| 1 | Clean BACKLOG.md — mark completed items, remove stale sprint numbers | All items completed in sprints 1-33 are marked DONE or removed. Remaining items have no stale sprint assignments. |
| 2 | Update copilot-instructions.md User Model section | User model reflects current reality (no Games list, includes PasswordHash, IsActive, DeletedAt). |
| 3 | Update copilot-instructions.md Data Access section | References MySQL/EF Core as primary, JSON as legacy. Mentions DbContext, migrations, DataProvider flag. |
| 4 | Add "Backlog Hygiene" step to SPRINT_EXECUTION_WORKFLOW.md Phase 5 | Retro checklist includes: verify completed items marked in backlog, remove stale sprint assignments, add new discoveries. |
| 5 | Update copilot-instructions.md architecture sections | Reflects current state: JWT auth, roles, admin dashboard, analytics, simulation engine, EF Core. |

## Risks
- None significant — all documentation-only changes.
