# Sprint 7 Plan — Engagement & Accessibility

**Sprint**: 7
**Date**: 2026-04-09
**Branch**: feature/20260409-sprint-7
**Goal**: Add leaderboard, keyboard shortcuts, accessibility, and timer/streak mechanics

## Items

| # | Item | Issue | Priority |
|---|------|-------|----------|
| 18 | Leaderboard page | #18 | P2 |
| 19 | Timer and streak mechanics | #19 | P3 |
| 21 | Accessibility improvements | #21 | P2 |

**Note**: Item #20 (Sound effects polish) deferred — depends on audio assets and is lower priority than accessibility.

## Acceptance Criteria

### Leaderboard (#18)
- Backend: GET `/api/user/leaderboard?top=10` returns ranked users by lifetimePoints
- Frontend: LeaderboardPage at `/leaderboard` route shows ranked table
- NavHeader links to leaderboard

### Timer & Streaks (#19)
- Per-word timer displayed during gameplay
- Consecutive correct guesses show streak indicator
- Streak multiplier affects score display

### Accessibility (#21)
- ARIA labels on word slots, clue panels, score display
- Keyboard shortcuts: C=request clue, G=give up, N=new game
- prefers-reduced-motion support
- Focus management for modal dialogs
