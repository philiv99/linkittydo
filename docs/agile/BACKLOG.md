# LinkittyDo Backlog

Master backlog of all planned work for LinkittyDo. Items are prioritized and grouped by category. This is the single source of truth for what to build next.

**Last Updated**: 2026-04-05

---

## How to Use This Document

- **Sprint planning**: Select items from the top of each priority group
- **After each sprint**: Remove completed items, add new discoveries, re-prioritize
- **Adding items**: Add to the appropriate category with a priority level

### Priority Levels

| Priority | Meaning |
|----------|---------|
| P1 - Critical | Must be done next; blocks other work or core functionality |
| P2 - High | Important for the product; should be in an upcoming sprint |
| P3 - Medium | Valuable but not urgent; schedule when capacity allows |
| P4 - Low | Nice to have; do when convenient |

---

## Backlog Items

### Core Gameplay

| # | Item | Priority | Notes |
|---|------|----------|-------|
| 1 | Scale phrase database (LLM batch generation) | P1 | Currently small set; need diverse phrases at multiple difficulties |
| 2 | Adaptive difficulty for clue selection | P2 | Infrastructure exists; clue selection not yet difficulty-aware |
| 3 | Phrase decomposition via syntax parsing | P3 | Currently uses stop-word lists; NLP parsing would improve quality |

### User Experience

| # | Item | Priority | Notes |
|---|------|----------|-------|
| 4 | User authentication (JWT) | P1 | API is currently open; needed before any public deployment |
| 5 | Responsive mobile layout | P2 | Desktop works; mobile needs optimization |
| 6 | Game timer and streak mechanics | P3 | Engagement features for returning players |
| 7 | Sound effects and animations | P4 | Polish; audio assets exist in public/audio/ |

### Infrastructure

| # | Item | Priority | Notes |
|---|------|----------|-------|
| 8 | SQL database migration | P2 | JSON files work for dev; SQL needed for production scale |
| 9 | Automated test suite (backend + frontend) | P1 | Essential for sprint confidence; minimal tests exist now |
| 10 | CI/CD pipeline hardening | P3 | GitHub Pages deploy exists; need backend deploy pipeline |

### Analytics & Learning

| # | Item | Priority | Notes |
|---|------|----------|-------|
| 11 | Clue effectiveness tracking | P3 | Foundation for RL system; log which clues lead to correct guesses |
| 12 | User behavior analytics | P4 | Track play patterns, difficulty preferences, session lengths |

### Social Features

| # | Item | Priority | Notes |
|---|------|----------|-------|
| 13 | Leaderboard | P3 | Lifetime points infrastructure exists |
| 14 | Share game results | P4 | Social sharing for viral growth |

---

## Completed Items

_Move items here after sprint completion. Include sprint number._

| Item | Sprint | Date |
|------|--------|------|
| (none yet) | - | - |
