# Sprint 7 Retrospective — Engagement & Accessibility

**Sprint**: 7
**Date**: 2026-04-09
**Branch**: feature/20260409-sprint-7
**Goal**: Add leaderboard, timer/streaks, and accessibility improvements

## Delivered

| Item | Issue | Status |
|------|-------|--------|
| Leaderboard page | #18 | Done |
| Timer & streaks | #19 | Done |
| Accessibility improvements | #21 | Done |

**Note**: Item #20 (Sound effects polish) deferred to future sprint.

## Metrics

- Backend tests: 130 → 136 (+6)
- Frontend tests: 49 → 55 (+6)
- Total: 191

## What Went Well

- Leaderboard implementation was straightforward — service layer already provided GetAllUsersAsync
- ARIA labels and prefers-reduced-motion were low-effort high-impact accessibility wins
- Keyboard shortcuts needed careful input-vs-global scoping to avoid interfering with guess typing
- Timer and streak are purely frontend state — no backend changes needed

## Process Notes

- Sound effects (#20) deferred because it needs audio asset evaluation, which is separate from core engagement
- Dual header pattern (mobile outer + desktop inner) requires duplicating stats bar — consider refactoring later
