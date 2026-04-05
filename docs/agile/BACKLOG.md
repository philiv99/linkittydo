# LinkittyDo Backlog

Master backlog of all planned work for LinkittyDo. Items are prioritized and grouped by category. This is the single source of truth for what to build next.

**Last Updated**: 2026-04-05
**Source Analysis**: See [DESIGN_CONTENT_ANALYSIS.md](DESIGN_CONTENT_ANALYSIS.md) for the full gap assessment that generated this backlog.

---

## How to Use This Document

- **Sprint planning**: Select items from the top of each priority group
- **After each sprint**: Remove completed items, add new discoveries, re-prioritize
- **Adding items**: Add to the appropriate category with a priority level
- **Sprint mapping**: See [Sprint Roadmap](#sprint-roadmap) below for the recommended execution order

### Priority Levels

| Priority | Meaning |
|----------|---------|
| P1 - Critical | Must be done next; blocks other work or core functionality |
| P2 - High | Important for the product; should be in an upcoming sprint |
| P3 - Medium | Valuable but not urgent; schedule when capacity allows |
| P4 - Low | Nice to have; do when convenient |

---

## Backlog Items

### Foundation & Testing

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 1 | Backend test suite (xUnit) | P1 | 1 | ZERO tests exist. Must add unit tests for GameService, ClueService, UserService, GamePhraseService, and all controllers. Blocks all future sprints. |
| 2 | Frontend test suite (Vitest + Testing Library) | P1 | 1 | Install Vitest, add tests for hooks (useGame, useUser), API service, and key components (GameBoard, WordSlot). |
| 3 | API response standardization | P1 | 1 | Controllers return raw objects; must wrap in `{ data, message }` / `{ error: { code, message } }` per copilot-instructions spec. |

### Core Gameplay Enhancement

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 4 | Difficulty-aware clue selection | P1 | 2 | `PreferredDifficulty` is stored and sent but completely ignored. ClueService must select Xnym type and URL preference based on difficulty (see DESIGN_CONTENT_ANALYSIS.md §3A). |
| 5 | Enhanced scoring model | P1 | 2 | Replace flat 100 pts with `BasePoints / (n_clues × n_guesses)`. Add difficulty-scaled base points, first-guess bonus, speed bonus. See §3B. |
| 6 | Phrase difficulty computation | P2 | 2 | GamePhrase needs a `Difficulty` field computed from word frequency, hidden word ratio, phrase length. LLM generation prompt should request varied difficulty levels. |
| 7 | Scale phrase database | P2 | 2 | Only 26 phrases exist. Batch-generate 100+ phrases across difficulty bands. Add duplicate detection. |

### Frontend Architecture

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 8 | React Router + page structure | P2 | 3 | Add routing: `/` (home), `/play` (game), `/history`, `/leaderboard`, `/profile`. Move GameBoard to `/play` route. |
| 9 | Home page | P2 | 3 | Welcome content, quick-start button, user stats summary for logged-in users. Currently only a splash screen. |
| 10 | Game history UI | P2 | 3 | List past games with scores, phrases, results. Drill into game detail with event timeline replay. Data already exists on User.Games. |
| 11 | Responsive mobile layout | P2 | 4 | Desktop-only today. Design spec defines tablet/mobile breakpoints for PhraseDisplay, CluePanel, and word slot sizing. |

### Clue Quality & Linguistic Engine

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 12 | Xnym taxonomy expansion | P2 | 5 | Add Datamuse `rel_ant` (antonyms), `rel_trg` (triggers), `rel_hom` (homophones). Blend by difficulty level per formula in system doc 02. |
| 13 | Contextual synonym selection | P3 | 5 | Use Datamuse `lc=`/`rc=` to disambiguate polysemous words (e.g., "bank" in financial vs river context). |
| 14 | Clue caching layer | P3 | 5 | Pre-compute and cache synonym → URL mappings with 7-day TTL. Background validation of cached URLs. |

### Infrastructure & DevOps

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 15 | CI/CD pipeline (GitHub Actions) | P2 | 4 | Backend build + test, frontend build + lint + test, PR gate checks. No CI exists today. |
| 16 | Session TTL and cleanup | P2 | 4 | In-memory sessions persist forever until restart. Add configurable TTL (default 24h), background cleanup timer. |
| 17 | Health check improvements | P3 | 4 | Current `/health` returns minimal info. Add dependency checks (Datamuse, DuckDuckGo reachability, data directory). |

### User Experience & Engagement

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 18 | Leaderboard page | P2 | 6 | Ranked list by lifetime points. Backend endpoint for top-N users. Frontend page with table. |
| 19 | Timer and streak mechanics | P3 | 6 | Per-word timer (affects scoring), consecutive-correct streak multiplier, visual streak indicator. |
| 20 | Sound effects polish | P3 | 6 | Audio assets exist but integration is basic. Add difficulty-appropriate audio cues, volume control, mute toggle. |
| 21 | Accessibility improvements | P2 | 6 | ARIA labels for word slots, keyboard shortcuts (C=clue, G=give-up, N=new-game), `prefers-reduced-motion`, high-contrast mode, screen reader announcements. |

### Developer Experience

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 36 | One-click app launcher (bat file) | P1 | 2 | Create a single `.bat` file that starts both the backend API and frontend dev server and opens the app in a browser. Eliminates multi-step manual startup. |

### Security & Production Readiness

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 22 | User authentication (JWT) | P1 | 7 | API is fully open. Add JWT token authentication, login/register flow with password, refresh tokens, protect user-specific endpoints. |
| 23 | Input sanitization audit | P2 | 7 | Review all API inputs for injection vectors. Validate guess text, user names, emails at API boundary. |
| 24 | Rate limiting | P2 | 7 | Prevent brute-force guessing and API abuse. Add rate limits on clue requests (per session) and auth endpoints. |

### Data Layer Evolution

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 25 | SQL database migration | P2 | 8 | Repository pattern is ready. Implement SqlUserRepository + SqlGamePhraseRepository. Add EF Core migrations. Config switch in appsettings. |
| 26 | Session persistence (Redis/DB) | P3 | 8 | Active game sessions lost on restart. Persist to Redis or database for production resilience. |

### Analytics & Intelligence

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 27 | Clue effectiveness tracking | P3 | 9 | Log clue → outcome pairs. Compute P(correct | synonym, URL domain). Build synonym affinity matrix over time. |
| 28 | User behavior analytics | P3 | 9 | Track play patterns, session lengths, difficulty progression, give-up rates. Dashboard for insights. |
| 29 | Phrase difficulty calibration | P3 | 9 | Use gameplay data (solve rate, avg clues, avg time) to auto-calibrate phrase difficulty scores. |

### Advanced Linguistic Features

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 30 | POS tagging integration | P3 | 10 | Replace stop-word list with real POS tagger for phrase decomposition. Consider spaCy API or cloud NLP service. |
| 31 | Reinforcement learning (LinUCB) | P4 | 10 | Contextual bandit for clue selection. Requires analytics data from Sprint 9. See system doc 07 for full spec. |

### Social & Community (Future)

| # | Item | Priority | Sprint | Notes |
|---|------|----------|--------|-------|
| 32 | Share game results | P4 | 11+ | Social sharing cards, copy-to-clipboard results format. |
| 33 | Games Manager UI | P4 | 11+ | Browse/create/evaluate phrases. Admin + community phrase submission. |
| 34 | Social features (groups, comments) | P4 | 11+ | From original wireframes (Society section). Community features. |
| 35 | Admin panel | P4 | 11+ | Content management, site parameters, user management. |

---

## Sprint Roadmap

Sprints are sequenced by dependencies and priority. Each sprint builds on the previous one. Estimated scope: 1-2 weeks each.

| Sprint | Theme | Key Items | Dependencies |
|--------|-------|-----------|--------------|
| **1** | **Testing & API Foundation** | #1, #2, #3 | None — must be first |
| **2** | **Difficulty & Scoring Engine** | #4, #5, #6, #7 | Sprint 1 (tests) |
| **3** | **Frontend Architecture & History** | #8, #9, #10 | Sprint 1 (tests) |
| **4** | **Responsive, CI/CD, Infrastructure** | #11, #15, #16, #17 | Sprint 1 (tests), Sprint 3 (routing) |
| **5** | **Clue Quality & Linguistic Engine** | #12, #13, #14 | Sprint 2 (difficulty system) |
| **6** | **Engagement & Accessibility** | #18, #19, #20, #21 | Sprint 3 (routing), Sprint 2 (scoring) |
| **7** | **Security & Authentication** | #22, #23, #24 | Sprint 1 (tests), Sprint 3 (routing) |
| **8** | **Data Layer Migration** | #25, #26 | Sprint 7 (auth), Sprint 4 (infra) |
| **9** | **Analytics & Intelligence** | #27, #28, #29 | Sprint 8 (database), Sprint 5 (clue tracking) |
| **10** | **Advanced NLP & RL** | #30, #31 | Sprint 9 (analytics data), Sprint 5 (xnym) |
| **11+** | **Social & Community** | #32, #33, #34, #35 | Sprint 7 (auth), Sprint 8 (database) |

### Sprint Dependency Graph

```
Sprint 1 (Tests + API) ────────┬──────────────────────────────────────┐
       │                       │                                      │
       ▼                       ▼                                      ▼
Sprint 2 (Difficulty)    Sprint 3 (Routing)                    Sprint 7 (Auth)
       │                    │       │                              │
       ▼                    ▼       ▼                              ▼
Sprint 5 (Clue Quality) Sprint 4 (Responsive/CI)          Sprint 8 (Database)
       │                    │                                      │
       ▼                    ▼                                      ▼
Sprint 6 (Engagement) ◄────┘                               Sprint 9 (Analytics)
                                                                   │
                                                                   ▼
                                                           Sprint 10 (NLP/RL)
                                                                   │
                                                                   ▼
                                                           Sprint 11+ (Social)
```

---

## Completed Items

_Move items here after sprint completion. Include sprint number._

| Item | Sprint | Date |
|------|--------|------|
| (none yet) | - | - |
