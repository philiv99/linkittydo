# Sprint 37 Plan — Growing Complexity Management

**Date**: 2026-04-10
**Goal**: Update all agent architecture context to match the current codebase (33 sprints of evolution), add a codebase map for quick orientation, and create complexity management guidelines.
**Branch**: `feature/20260410-sprint-37`

## Problem Statement

The codebase has grown from a simple JSON-file game to a full-stack application with MySQL, JWT auth, roles, admin dashboard, analytics, simulation engine, and 294 backend + 61 frontend tests. However, agent descriptions still reference the original simple architecture (JSON files, no auth, basic service layer). This causes agents to give outdated advice and miss important patterns.

## Items

| # | Task | Acceptance Criteria |
|---|------|-------------------|
| 1 | Update code-architect agent with current architecture | Reflects: EF Core + MySQL, JWT auth with refresh tokens, role-based authorization, admin dashboard, analytics pipeline, simulation engine. Lists all current services and controllers. |
| 2 | Update code-simplifier agent with complexity awareness | Includes guidance on: when to extract services, max file length guidelines, detecting God objects, spotting unnecessary abstractions. |
| 3 | Add "Codebase Map" section to copilot-instructions.md | Quick-reference section listing all services, controllers, DbContext tables, and key frontend components with their file paths. |
| 4 | Add complexity budget to SPRINT_EXECUTION_WORKFLOW.md | Guideline: if a sprint adds >3 new services or >2 new controllers, require an architecture review step. |
| 5 | Update copilot-instructions.md Backend Patterns section | Reflects current patterns: EF Core DbContext, Unit of Work, feature flags, scoped DI, soft-delete, audit trail. |

## Risks
- None significant — documentation and agent instruction updates only.
