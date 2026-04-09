# Sprint 4 Plan: Frontend Architecture & History

**Sprint Number**: 4
**Date**: 2026-04-09
**Branch**: `feature/20260409-sprint-4`
**Theme**: React Router, Home Page, Game History

---

## Goals

Establish proper frontend page structure with React Router, create a welcoming home page, and add game history UI for registered users.

## Items

### Item 1: React Router + Page Structure (Backlog #8)
- Install react-router-dom
- Set up BrowserRouter with routes: `/` (home), `/play` (game), `/history`
- Add navigation header component
- Move GameBoard to `/play` route
- Update App.tsx as router container

### Item 2: Home Page (Backlog #9)
- Welcome content with game description
- Quick-start "Play Now" button
- User stats summary for logged-in users (lifetime points, games played)
- Link to game history

### Item 3: Game History UI (Backlog #10)
- List past games with scores, phrases, results
- Drill into game detail with event timeline
- Uses `getUserGames()` API (already exists)
- Only available for registered users

## Acceptance Criteria
- React Router installed and configured
- Three routes working: /, /play, /history
- Navigation between pages
- Home page shows user stats when logged in
- Game history lists past games
- All existing tests pass
- New tests for HomePage and GameHistory components
